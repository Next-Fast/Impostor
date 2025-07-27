using Impostor.Api.Events;

namespace Impostor.Api.Extension.Commands;

public interface ICommand
{
    public string GetDescription()
    {
        return string.Empty;
    }
}

public interface ISystemCommand : ICommand
{
    public ValueTask<bool> InvokeAsync(string command, string[] argsArray);
}

public interface ISingleCommand : ICommand
{
    public string Command { get; }
    public Task<EventTypeResult> InvokeAsync(CommandEventArgs args);
}
