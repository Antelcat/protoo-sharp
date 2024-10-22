using System.Net.WebSockets;
using System.Text.Json;
using Antelcat.AspNetCore.ProtooSharp.Internals;
using Microsoft.Extensions.Logging;

namespace Antelcat.AspNetCore.ProtooSharp.Transports;

internal class WebSocketTransport(WebSocket webSocket)
{
    /*private readonly WebSocketConnection connection;
    private readonly ILogger             logger;
    private          string?             toString;

    public WebSocketTransport(WebSocketConnection connection, ILogger logger) : base(logger)
    {
        logger.LogDebug("constructor()");
        this.connection = connection;
        this.logger     = logger;
        HandleConnection();
    }

    public bool Closed { get; private set; }

    public void Close()
    {
        if (Closed) return;
        logger.LogDebug("close() | [{Connection}]", this);
        Closed = true;
        SafeEmit("close");
        connection.Close();
    }

    public async Task Send(object message)
    {
        ObjectDisposedException.ThrowIf(Closed, this);
        try
        {
            await connection.SendAsync(JsonSerializer.Serialize(message));
        }
        catch (Exception ex)
        {
            logger.LogWarning("send() failed:{Ex}", ex);
            throw;
        }
    }
    
    private void HandleConnection()
    {
        connection.Closed += async ex =>
        {
            if (Closed) return;
            Closed = true;
            logger.LogDebug("connection 'close' event [{@Connection}, {Ex}]", connection, ex);
            SafeEmit("close");
        };

        connection.Message += async raw =>
        {
            Dictionary<string, object> message;
            try
            {
                message = Message.Parse(raw);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
                return;
            }

            if (ListenerCount("message") == 0)
            {
                logger.LogError(
                    "no listeners for 'message' event, ignoring received message");
                return;
            }

            SafeEmit("message", message);
        };
    }


    public override string ToString() =>
        toString ??=
            $"{(connection.HttpContext.Request.IsHttps ? "WSS" : "WS")}:[{connection.HttpContext.Connection.RemoteIpAddress}]:{connection.HttpContext.Connection.RemotePort}";*/
}