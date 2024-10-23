namespace Antelcat.AspNetCore.ProtooSharp.Internals;

public class NotificationPayload
{
    public required string Method { get; set; }
}

public class NotificationPayload<T> : NotificationPayload
{
    public T? Data { get; set; }
}
