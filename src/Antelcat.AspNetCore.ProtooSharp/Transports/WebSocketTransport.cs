using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Text;
using Antelcat.AspNetCore.ProtooSharp.Extensions;
using Antelcat.AspNetCore.WebSocket;
using Antelcat.AspNetCore.WebSocket.Extensions;
using WebSocketOptions = Antelcat.AspNetCore.WebSocket.WebSocketOptions;

// ReSharper disable once CheckNamespace
namespace Antelcat.AspNetCore.ProtooSharp;

public class WebSocketTransport
{
    private readonly System.Net.WebSockets.WebSocket socket;
    private readonly HttpContext                     context;
    private readonly ILogger                         logger;
    private          string?                         toString;

    public event Func<Task>? Close;

    public event Func<Message, Task>? Message;

    public WebSocketTransport(System.Net.WebSockets.WebSocket socket,
                              HttpContext context,
                              ILogger logger)
    {
        logger.LogDebug("constructor()");
        this.socket  = socket;
        this.context = context;
        this.logger  = logger;
        HandleConnection();
    }

    public bool Closed { get; private set; }
    
    public override string ToString() =>
        toString ??=
            $"{(context.Request.IsHttps ? "WSS" : "WS")}:[{context.Connection.RemoteIpAddress}]:{context.Connection.RemotePort}";

    public async Task CloseAsync()
    {
        if (Closed) return;
        logger.LogDebug("close() | [{Connection}]", this);
        Closed = true;
        Close?.Invoke();
        try
        {
            await socket.CloseAsync((WebSocketCloseStatus)4000, "closed by protoo-server", default);
        }
        catch (Exception ex)
        {
            logger.LogError($"{nameof(CloseAsync)}() | error closing the connection: {{Ex}}", ex);
        }
    }

    public async Task SendAsync<T>(T message)
    {
        ObjectDisposedException.ThrowIf(Closed, this);
        try
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes(message.Serialize()),
                WebSocketMessageType.Text, WebSocketMessageFlags.EndOfMessage, default);
        }
        catch (Exception ex)
        {
            logger.LogWarning($"{nameof(SendAsync)}() failed:{{Ex}}", ex);
            throw;
        }
    }

    private void HandleConnection()
    {
        var handler = new AsyncWebSocketHandler();
        handler.Text   += OnMessage;
        handler.Closed += OnClose;
        Task.Run(async () =>
        {
            await handler.Handle(socket, new WebSocketOptions());
        });
    }

    private async Task OnClose(Exception? exception)
    {
        if (Closed) return;
        Closed = true;
        logger.LogDebug("connection 'close' event [{@Connection}, {Ex}]", socket, exception?.Message);
        if (Close != null) await Close();
    }

    private async Task OnMessage(string raw, CancellationToken token)
    {
        Console.WriteLine(raw);
        Message message;
        try
        {
            message = Antelcat.AspNetCore.ProtooSharp.Message.Parse(raw);
        }
        catch (Exception ex)
        {
            logger.LogError("{Ex}", ex);
            return;
        }

        if (Message == null)
        {
            logger.LogError(
                "no listeners for 'message' event, ignoring received message");
            return;
        }

        await Message(message);
    }
}