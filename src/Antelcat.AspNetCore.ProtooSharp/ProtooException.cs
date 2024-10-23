namespace Antelcat.AspNetCore.ProtooSharp;

public class ProtooException : Exception
{
    public int ErrorCode { get; set; }

    public required string ErrorReason { get; set; }
}