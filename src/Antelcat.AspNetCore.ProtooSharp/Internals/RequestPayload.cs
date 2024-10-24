namespace Antelcat.AspNetCore.ProtooSharp.Internals;

public record RequestPayload
{
    public          bool Request => true;
    public required int  Id      { get; set; }

    public required string Method { get; set; }
}

public record RequestPayload<T> : RequestPayload
{
    public T? Data { get; set; }

    public override string ToString() => Serialization.GlobalSerialization.Serialize(this);
}