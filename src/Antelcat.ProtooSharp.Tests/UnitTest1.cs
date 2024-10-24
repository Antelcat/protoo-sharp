using System.Net;
using Antelcat.AspNetCore.ProtooSharp;

namespace Antelcat.ProtooSharp.Tests;

public class Tests
{
    private          WebApplication             app;
    private          Room                       room;
    private readonly TaskCompletionSource       complete     = new();
    private readonly TaskCompletionSource<Peer> onServerPeer = new();

    [SetUp]
    public void Setup()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.WebHost.UseKestrel(o => { o.Listen(IPAddress.Any, 9999); });

        builder.Services.AddSingleton<WebSocketAcceptContext>();
        builder.Services.AddProtooServer<WebSocketServer>();

        app = builder.Build();

        app.UseHttpsRedirection();
        app.UseWebSockets();
        app.MapProtooServer<WebSocketServer>();
        
        room = app.Services.GetRequiredService<Room>();

        app.Services.GetRequiredService<WebSocketServer>().ConnectionRequest += async (request, accept, reject) =>
        {
            string? peerId = request.Request.Query["peerId"];

            switch (peerId)
            {
                case null or "reject":
                {
                    await reject(403, "Sorry!");
                    break;
                }
                default:
                {
                    var transport = await accept();
                    onServerPeer.SetResult(room.CreatePeer(peerId, transport));
                    break;
                }
            }
        };
        app.RunAsync();
    }

    [TearDown]
    public async Task Clean()
    {
        await app.DisposeAsync();
    }

    [Test(Description = "client sends request to server")]
    public async Task TestClientRequest()
    {
        var serverPeer = await onServerPeer.Task;
        serverPeer.Request += async handler =>
        {
            var method = handler.Request.Request.Method;
            Console.WriteLine(method);
            var data = handler.Request.WithData<Dictionary<string, string>>();
            Console.WriteLine(data);
            await handler.AcceptAsync(new Dictionary<string, string> { { "text", "hi!" } });
            Pass();
        };
        await complete.Task;
        Assert.Pass();
    }

    [Test(Description = "client sends request to server and server rejects it")]
    public async Task TestServerReject()
    {
        var                             serverPeer = await onServerPeer.Task;
        Func<Peer.RequestHandler, Task> handler    = null!;
        handler = async request =>
        {
            serverPeer.Request -= handler;
            await request.RejectAsync(503, "WHO KNOWS!");
            Pass();
        };
        serverPeer.Request += handler;
        await complete.Task;
        Assert.Pass();
    }

    [Test(Description = "client sends notification to server")]
    public async Task TestClientNotify()
    {
        var serverPeer = await onServerPeer.Task;

        serverPeer.Notification += async notification =>
        {
            //Assert.Equals(notification.Notification.Method, "hello");
            if (notification.Notification.Method == "hello")
            {
                Pass();
            }
            else
            {
                Fail("method not match");
            }
        };

        await complete.Task;
        Assert.Pass();
    }

    [Test(Description = "server sends request to client")]
    public async Task TestServerRequest()
    {
        var serverPeer = await onServerPeer.Task;
        var message    = await serverPeer.RequestAsync("hello", new { foo = "bar" });
        if (message.WithData<Dictionary<string, string>>()!.Data!.TryGetValue("text", out var text) && text is "hi!")
        {
            Pass();
        }
        else
        {
            Fail($"{text} not match required hi!");
        }

        await complete.Task;
        Assert.Pass();
    }

    [Test(Description = "server sends request to client and client rejects it")]
    public async Task TestClientReject()
    {
        var serverPeer = await onServerPeer.Task;
        try
        {
            var response = await serverPeer.RequestAsync("hello", new { foo = "bar" });
            Assert.Fail();
        }
        catch (ProtooException ex)
        {
            Assert.That(ex.ErrorCode, Is.EqualTo(503));
            Assert.That(ex.ErrorReason, Is.EqualTo("WHO KNOWS!"));
            Assert.Pass();
        }
    }

    [Test(Description = "server sends notification to client")]
    public async Task TestServerNotify()
    {
        var serverPeer = await onServerPeer.Task;
        await serverPeer.NotifyAsync("hello", new { foo = "bar" });
        Assert.Pass();
    }

    [Test(Description = "room.close() closes clientPeer and serverPeer")]
    public async Task TestRoomClose()
    {
        var serverPeer = await onServerPeer.Task;
        serverPeer.Close += async () => { Pass(); };
        await room.CloseAsync();
        await complete.Task;
        Assert.Pass();
    }

    private void Pass()               => complete.SetResult();
    private void Fail(string message) => complete.SetException(new Exception(message));
}