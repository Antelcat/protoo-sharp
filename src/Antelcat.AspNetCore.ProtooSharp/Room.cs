using System.Collections.Concurrent;
using System.Data;

namespace Antelcat.AspNetCore.ProtooSharp;

public class Room(ILoggerFactory loggerFactory)
{
    private readonly ILogger<Room>                      logger = loggerFactory.CreateLogger<Room>();
    private readonly ConcurrentDictionary<string, Peer> peers  = [];

    public event Func<Task>? Close;

    public bool Closed { get; private set; }

    public ICollection<Peer> Peers => peers.Values;

    public async Task CloseAsync()
    {
        if (Closed) return;
        logger.LogDebug($"{nameof(CloseAsync)}()");
        Closed = true;
        foreach (var (_, peer) in peers)
        {
            await peer.CloseAsync();
        }

        if (Close is not null)
        {
            await Close.Invoke();
        }
    }

    public Peer CreatePeer(string peerId, WebSocketTransport? transport)
    {
        logger.LogDebug("CreatePeer() [{PeerId}, {@Transport}]", peerId, transport);
        ArgumentNullException.ThrowIfNull(transport);
        
        if (peers.ContainsKey(peerId))
        {
            _ = transport.CloseAsync();
            throw new DuplicateNameException($"there is already a Peer with same peerId [peerId:${peerId}]");
        }

        var peer = new Peer(peerId, transport, loggerFactory.CreateLogger<Peer>());
        peers.TryAdd(peerId, peer);

        peer.Close += async () => peers.TryRemove(peerId, out _);
        return peer;
    }

    public bool HasPeer(string peerId) => peers.ContainsKey(peerId);

    public Peer? GetPeer(string peerId) => peers.GetValueOrDefault(peerId);
}