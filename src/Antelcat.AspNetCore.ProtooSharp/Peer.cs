using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Antelcat.AspNetCore.ProtooSharp;

public class Peer : EnhancedEventEmitter
{
    private readonly ILogger<Peer> logger;
    private          string        Id { get; }
    public           bool          Closed { get; private set; }

    private readonly Dictionary<int, object>    sents = [];
    public           Dictionary<string, object> Data { get; } = [];

    private readonly HttpContext transport;
    
    public Peer(string peerId, HttpContext transport, ILoggerFactory loggerFactory) : base(
        loggerFactory.CreateLogger<EnhancedEventEmitter>())
    {
        Id             = peerId;
        this.transport = transport;
        logger         = loggerFactory.CreateLogger<Peer>();
        HandleTransport();
    }

    private void HandleTransport()
    {
        if (transport.RequestAborted.IsCancellationRequested)
        {
            Closed = true;
            Task.Run(() => { SafeEmit("close"); });
            return;
        }

        transport.RequestAborted.Register(() =>
        {
            if (Closed) return;
            Closed = true;
            SafeEmit("close");
        });
    }

    public void Close()
    {
        if (Closed)
        {
            return;
        }
            
        logger.LogDebug("close");
        
       
    }
}