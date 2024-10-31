using System.Diagnostics.CodeAnalysis;
using Antelcat.AspNetCore.ProtooSharp.Extensions;
using Accept = System.Func<System.Threading.Tasks.Task<Antelcat.AspNetCore.ProtooSharp.WebSocketTransport?>>;
using Reject = System.Func<int, string, System.Threading.Tasks.Task>;

// ReSharper disable once CheckNamespace
namespace Antelcat.AspNetCore.ProtooSharp;

[Serializable]
public class WebSocketServer
{
    private          bool                   stopped;
    private          HashSet<HttpContext>   connections = [];
    private readonly WebSocketAcceptContext webSocketAcceptContext;
    private readonly ILoggerFactory         loggerFactory;
    private readonly ILogger                logger;

    public WebSocketServer(WebApplication webApplication,
                           [StringSyntax("route")] string pattern = "/",
                           WebSocketAcceptContext? webSocketAcceptContext = null,
                           Action<IEndpointConventionBuilder>? endpointBuilder = null)
        : this(webApplication.Services.GetRequiredService<ILoggerFactory>(), webSocketAcceptContext)
    {
        var builder = webApplication.Map("/", OnRequest);
        endpointBuilder?.Invoke(builder);
    }

    public WebSocketServer(ILoggerFactory loggerFactory, WebSocketAcceptContext? webSocketAcceptContext = null)
    {
        this.loggerFactory = loggerFactory;
        logger             = loggerFactory.CreateLogger<WebSocketServer>();
        this.webSocketAcceptContext = webSocketAcceptContext ?? new WebSocketAcceptContext
        {
            SubProtocol       = "protoo",
            KeepAliveInterval = TimeSpan.FromSeconds(60),
        };
        this.webSocketAcceptContext.SubProtocol = "protoo";
    }


    public delegate Task ConnectionHandler((HttpRequest Request, string Origin, WebSocketManager Socket) request,
                                           Accept accept,
                                           Reject reject);
    
    public event ConnectionHandler? ConnectionRequest;
    
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
    
    public async Task OnRequest(HttpContext request)
    {
        if (stopped)
        {
            request.Abort();
            return;
        }

        if (!request.WebSockets.IsWebSocketRequest)
        {
            logger.LogWarning($"{nameof(OnRequest)}() | not WebSocket request");
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
            var lifeTime = new TaskCompletionSource(); //keep connection for ASP.NET lifetime
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
                    var subLogger     = loggerFactory.CreateLogger<WebSocketTransport>();
                    var connection = await request.WebSockets.AcceptWebSocketAsync(webSocketAcceptContext);

                    // Create a new Protoo WebSocket transport.
                    var transport = new WebSocketTransport(connection,
                        request,
                        subLogger);
                    transport.Close += async () => lifeTime.SetResult();

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
                    
                    lifeTime.SetResult();
                }
            );
            await lifeTime.Task;
        }
        catch (Exception ex)
        {
            await request.Reject(500, ex.ToString());
        }
    }
}

