using Impostor.Api.Config;

namespace Impostor.Api.Extension.Commands;

public interface ICommandManager
{
    public IServiceProvider ServiceProvider { get; }

    public IReadOnlyList<ICommand> Commands { get; }

    public ICommandManager RegisterCommand(ICommand command);

    public ICommandManager RegisterCommand<T>() where T : ICommand;

    public Task HandleCommandAsync(string commandString);
    
    public Task HandleStringAsync();
    
    public List<IConfigSet> ConfigSets { get; }
}
