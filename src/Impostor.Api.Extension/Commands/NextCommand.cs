using Impostor.Api.Events;

namespace Impostor.Api.Extension.Commands;

public class NextCommand(string command, Func<CommandEventArgs, Task<EventTypeResult>> onInvoke) : ISingleCommand
{
    private Func<CommandEventArgs, Task<EventTypeResult>> OnInvoke { get; } = onInvoke;
    public string Command { get; } = command;

    public Task<EventTypeResult> InvokeAsync(CommandEventArgs args)
    {
        return OnInvoke(args);
    }
}
