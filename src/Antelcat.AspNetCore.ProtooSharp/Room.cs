using System.Collections.Concurrent;
using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Antelcat.AspNetCore.ProtooSharp;

public class Room(ILoggerFactory loggerFactory)
    : EnhancedEventEmitter(loggerFactory.CreateLogger<EnhancedEventEmitter>())
{
    private readonly ILogger<Room> logger = loggerFactory.CreateLogger<Room>();
    
    private readonly ConcurrentDictionary<string, Peer> peers = [];

    public bool Closed { get; private set; }

    public ICollection<Peer> Peers => peers.Values;

    public void Close()
    {
        if (Closed) return;
        logger.LogDebug($"{nameof(Close)}()");
        Closed = true;
        foreach (var (id, peer) in peers)
        {
            peer.Close();
        }

        SafeEmit("close");
    }

    public Peer CreatePeer(string peerId, HttpContext transport)
    {
        logger.LogDebug("CreatePeer() [{PeerId}, {@Transport}]", peerId, transport);
        if (peers.ContainsKey(peerId))
        {
            transport.Abort();
            throw new DuplicateNameException($"there is already a Peer with same peerId [peerId:${peerId}]");
        }

        var peer = new Peer(peerId, transport, loggerFactory);
        peers.TryAdd(peerId, peer);
        peer.On("close", async _ => peers.TryRemove(peerId, out var _));
        return peer;
    }

    public bool HasPeer(string peerId) => peers.ContainsKey(peerId);

    public Peer? GetPeer(string peerId) => peers.GetValueOrDefault(peerId);
}