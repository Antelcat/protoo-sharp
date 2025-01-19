namespace Antelcat.AspNetCore.ProtooSharp;

public static class Utils
{
    static Utils()
    {
        var random = new Random();
        RandomNumberGenerator = () => random.Next(0, 10_000_000);
    }

    internal static int GenerateRandomNumber() => RandomNumberGenerator();
    public static Func<int> RandomNumberGenerator { get; set; }
}