using System.Diagnostics.CodeAnalysis;

namespace Antelcat.AspNetCore.ProtooSharp.Internals;

public record ResponsePayload
{
    public required int Id { get; set; }

    [MemberNotNullWhen(false, nameof(ErrorCode), nameof(ErrorReason))]
    public bool Ok { get; set; }

    public int?    ErrorCode   { get; set; }
    public string? ErrorReason { get; set; }
}

public record ResponsePayload<T> : ResponsePayload
{
    public T? Data { get; set; }
}
