using Antelcat.NodeSharp.Events;

namespace Antelcat.AspNetCore.ProtooSharp;

public class EnhancedEventEmitter : EventEmitter
{
    private readonly ILogger logger;

    protected EnhancedEventEmitter(ILogger logger)
    {
        this.logger  =  logger;
        MaxListeners =  int.MaxValue;
        EmitError    += (eventName, exception) =>
        {
            logger.LogError("{SafeEmit}() | event listener threw an error {EventName}:{Exception}", nameof(SafeEmit), eventName, exception);
        };
    }

    public void SafeEmit(string eventName, params object?[] args) => Emit(eventName, args);

    public Task SafeEmitAsTask(string eventName, params object?[] args)
    {
        var source = new TaskCompletionSource<object?[]>();
        SafeEmit(eventName, args, (object?[] argv) => source.SetResult(argv),
            (Exception ex) => source.SetException(ex));
        return source.Task;
    }
}