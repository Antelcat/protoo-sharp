using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Antelcat.AspNetCore.WebSocket;

namespace Antelcat.AspNetCore.ProtooSharp.Transports;

internal class WebSocketTransport
{
    private readonly System.Net.WebSockets.WebSocket socket;
    private readonly HttpContext                     context;
    private readonly ILogger                         logger;
    private          string?                         toString;

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

    public event Func<Task>? Close;

    public event Func<Dictionary<string, object>, Task>? Message;

    public async Task CloseAsync()
    {
        if (Closed) return;
        logger.LogDebug("close() | [{Connection}]", this);
        Closed = true;
        Close?.Invoke();
        try
        {
            await socket.CloseAsync((WebSocketCloseStatus)4000, "closed by protoo-server", CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogError($"{nameof(CloseAsync)}() | error closing the connection: {{Ex}}", ex);
        }
    }

    public async Task SendAsync(object message)
    {
        ObjectDisposedException.ThrowIf(Closed, this);
        try
        {
            await socket.SendAsync(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)),
                WebSocketMessageType.Text, true, CancellationToken.None);
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
        handler.Closed += async ex =>
        {
            if (Closed) return;
            Closed = true;
            logger.LogDebug("connection 'close' event [{@Connection}, {Ex}]", socket, ex);
            if (Close != null) await Close();
        };

        handler.Text += async (raw, _) =>
        {
            Dictionary<string, object> message;
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
        };
    }


    public override string ToString() =>
        toString ??=
            $"{(context.Request.IsHttps ? "WSS" : "WS")}:[{context.Connection.RemoteIpAddress}]:{context.Connection.RemotePort}";
}