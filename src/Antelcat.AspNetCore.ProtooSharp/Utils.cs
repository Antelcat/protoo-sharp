namespace Antelcat.AspNetCore.ProtooSharp;

internal static class Utils
{
    public static int GenerateRandomNumber() => Random.Next(0, 10000000);

    private static readonly Random Random = new();
}