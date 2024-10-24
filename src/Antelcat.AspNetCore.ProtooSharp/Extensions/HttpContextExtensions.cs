namespace Antelcat.AspNetCore.ProtooSharp.Extensions;

internal static class HttpContextExtensions
{
    public static async Task Reject(this HttpContext context, int statusCode, string text)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(text);
        await context.Response.CompleteAsync();
    }

    public static string? Origin(this HttpContext context) => context.Request.Headers.Origin;
}