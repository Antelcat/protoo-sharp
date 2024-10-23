namespace Antelcat.AspNetCore.ProtooSharp.Extensions;

internal static class TaskExtensions
{
    public static Task Catch(this Task task, Action<Exception?> action) =>
        task.ContinueWith(t => action(t.Exception), TaskContinuationOptions.OnlyOnFaulted);

    public static Task Catch<T>(this Task<T> task, Action<Exception?> action) =>
        task.ContinueWith(t => action(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
}