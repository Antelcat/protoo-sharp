using Antelcat.AspNetCore.ProtooSharp.Extensions;

using Accept = System.Func<System.Threading.Tasks.Task<Antelcat.AspNetCore.ProtooSharp.Transports.WebSocketTransport?>>;
using Reject = System.Func<int, string, System.Threading.Tasks.Task>;

namespace Antelcat.AspNetCore.ProtooSharp.Transports;

[Serializable]
public class WebSocketServer(ILoggerFactory loggerFactory)
{
    private readonly ILogger              logger = loggerFactory.CreateLogger<WebSocketServer>();
    private          bool                 stopped;
    private          HashSet<HttpContext> connections = [];

    internal event Func<(HttpRequest Request, string Origin, WebSocketManager Socket), Accept, Reject, Task>? ConnectionRequest;
    
    public void Stop()
    {
        if(stopped) return;
        stopped = true;
        lock (connections)
        {
            foreach (var connection in connections)
            {
                connection.Abort();
            }

            connections.Clear();
        }
    }
    
    internal async Task OnRequest(HttpContext request)
    {
        if (stopped)
        {
            request.Abort();
            return;
        }

        if (request.Request.Headers.SecWebSocketProtocol != "protoo")
        {
            logger.LogWarning($"{nameof(OnRequest)}() | invalid/missing Sec-WebSocket-Protocol");

            await request.Reject(403, "invalid/missing Sec-WebSocket-Protocol");
            
            return;
        }

        if (ConnectionRequest == null)
        {
            logger.LogError($"{nameof(OnRequest)}() | no listeners for 'ConnectionRequest' event, rejecting connection request");

            await request.Reject(500, $"no listeners for '{nameof(ConnectionRequest)}' event");

            return;
        }
        
        request.RequestAborted.Register(() =>
        {
            lock (connections)
            {
                connections.Remove(request);
            }
        });

        var replied = false;
        try
        {
            await ConnectionRequest(
                // Connection data.
                (request.Request, request.Origin() ?? string.Empty, request.WebSockets),
                // accept() function.
                async () =>
                {
                    if (replied)
                    {
                        logger.LogWarning(
                            $"{nameof(OnRequest)}() | cannot call accept(), connection request already replied");

                        return null;
                    }

                    replied = true;

                    // Get the WebSocketConnection instance.
                    var connection = await request.WebSockets.AcceptWebSocketAsync();

                    // Create a new Protoo WebSocket transport.
                    var transport = new WebSocketTransport(connection,
                        request,
                        loggerFactory.CreateLogger<WebSocketTransport>());

                    logger.LogDebug($"{nameof(OnRequest)}() | accept() called");

                    // Return the transport.
                    return transport;
                },
                async (code, reason) =>
                {
                    if (replied)
                    {
                        logger.LogWarning(
                            $"{nameof(OnRequest)}() | cannot call accept(), connection request already replied");

                        return;
                    }

                    replied = true;

                    logger.LogDebug(
                        $"{nameof(OnRequest)}() | reject() called [{{Code}} | {{Reason}}]", code, reason);

                    await request.Reject(code, reason);
                }
            );
        }
        catch (Exception ex)
        {
            await request.Reject(500, ex.ToString());
        }
    }
}

