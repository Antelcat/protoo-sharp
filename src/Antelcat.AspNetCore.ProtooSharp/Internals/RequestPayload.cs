namespace Antelcat.AspNetCore.ProtooSharp.Internals;

public record RequestPayload
{
    public required int Id { get; set; }
    
    public required string Method { get; set; }
}

public record RequestPayload<T> : RequestPayload
{
    public T? Data { get; set; }
}