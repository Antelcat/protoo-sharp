using Antelcat.AspNetCore.ProtooSharp.Extensions;

namespace Antelcat.AspNetCore.ProtooSharp;

public class Peer
{
    public string                     Id     { get; }
    public bool                       Closed { get; private set; }
    public Dictionary<string, object> Data   { get; } = [];
    
    private readonly ILogger               logger;
    private readonly Dictionary<int, Sent> sents = [];
    private readonly WebSocketTransport    transport;

    public event Func<Task>?                      Close;
    public event Func<RequestHandler, Task>?      Request;
    public event Func<NotificationMessage, Task>? Notification;

    public Peer(string peerId, WebSocketTransport transport, ILogger<Peer> logger)
    {
        Id             = peerId;
        this.transport = transport;
        this.logger    = logger;
        HandleTransport();
    }

    public async Task CloseAsync()
    {
        if (Closed) return;

        logger.LogDebug("CloseAsync()");
        Closed = true;
        await transport.CloseAsync();
        foreach (var (key, value) in sents)
        {
            value.Close();
        }

        if (Close != null)
        {
            await Close();
        }
    }

    public async Task<ResponseMessage> RequestAsync<T>(string method, T? data = default)
    {
        var request = Message.CreateRequest(method, data);
        logger.LogDebug($"{nameof(RequestAsync)}() [{{Method}}, {{Id}}]", method, request.Id);
        await transport.SendAsync(request);

        var ret     = new TaskCompletionSource<ResponseMessage>();
        var cancel  = new CancellationTokenSource();
        var timeout = 2000 * (15 + 0.1 * sents.Count);
        var sent = new Sent
        {
            Id     = request.Id,
            Method = request.Method,
            Resolve = response =>
            {
                if (!sents.Remove(request.Id)) return;
                cancel.Cancel();
                ret.SetResult(response);
            },
            Reject = exception =>
            {
                if (!sents.Remove(request.Id)) return;
                cancel.Cancel();
                ret.SetException(exception);
            },
            Timer = Task.Delay((int)timeout, cancel.Token).ContinueWith(t =>
            {
                if (!sents.Remove(request.Id)) return;
                ret.SetException(new TimeoutException("request timeout"));
            }, cancel.Token),
            Close = () =>
            {
                cancel.Cancel();
                ret.SetException(new ObjectDisposedException("peer closed"));
            }
        };
        sents.Add(request.Id, sent);
        return await ret.Task;
    }

    public async Task NotifyAsync<T>(string method, T? data = default)
    {
        var notification = Message.CreateNotification(method, data);
        logger.LogDebug("Notify() [{Method}]", method);
        await transport.SendAsync(notification);
    }

    private void HandleTransport()
    {
        if (transport.Closed)
        {
            Closed = true;
            Task.Run(async () =>
            {
                if (Close != null) await Close.Invoke();
            });
            return;
        }

        transport.Close += async () =>
        {
            if (Closed) return;
            Closed = true;
            if (Close != null)
            {
                await Close();
            }
        };

        transport.Message += message => 
            message switch
        {
            RequestMessage request           => HandleRequest(request),
            ResponseMessage response         => HandleResponse(response),
            NotificationMessage notification => HandleNotification(notification),
            _                                => Task.CompletedTask
        };
    }

    private async Task HandleRequest(RequestMessage request)
    {
        try
        {
            if (Request is not null) await Request.Invoke(new RequestHandler(request, transport));
        }
        catch (Exception ex)
        {
            var response = Message.CreateErrorResponse(request.Request, 500, ex.ToString());
            _ = transport.SendAsync(response).Catch(_ => { });
        }
    }

    private async Task HandleResponse(ResponseMessage response)
    {
        var res = response.Response;

        var sent = sents.GetValueOrDefault(res.Id);

        if (sent is null)
        {
            logger.LogError("received response does not match any sent request [{Id}]", res.Id);
            return;
        }

        if (res.Ok)
        {
            sent.Resolve(response);
        }
        else
        {
            sent.Reject(new ProtooException
            {
                ErrorCode   = res.ErrorCode.Value,
                ErrorReason = res.ErrorReason
            });
        }
    }

    private Task HandleNotification(NotificationMessage notification)
    {
        return Notification?.Invoke(notification) ?? Task.CompletedTask;
    }

    public class RequestHandler(RequestMessage request, WebSocketTransport transport)
    {
        public RequestMessage Request => request;

        public Task AcceptAsync<T>(T data)
        {
            var response = Message.CreateSuccessResponse(request.Request, data);

            return transport.SendAsync(response);
        }

        public void Accept<T>(T data) => AcceptAsync(data).Catch(_ => { });

        public async Task RejectAsync(int errorCode, string errorReason)
        {
            var response = Message.CreateErrorResponse(request.Request, errorCode, errorReason);

            await transport.SendAsync(response);
        }

        public void Reject(int errorCode, string errorReason) => RejectAsync(errorCode, errorReason).Catch(_ => { });
    }

    private class Sent
    {
        public          int                     Id      { get; set; }
        public required string                  Method  { get; set; }
        public required Action<ResponseMessage> Resolve { get; init; }
        public required Action<ProtooException> Reject  { get; init; }
        public required Task                    Timer   { get; init; }
        public required Action                  Close   { get; init; }
    }
}