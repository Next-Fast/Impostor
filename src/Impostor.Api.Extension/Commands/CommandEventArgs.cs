using Impostor.Api.Extension.Utils;
using Impostor.Api.Utils;

namespace Impostor.Api.Extension.Commands;

public class CommandEventArgs(ICommandManager manager, string[] args) : EventArgs, IArgUtils
{
    public ICommandManager Sender { get; set; } = manager;
    public string? SubCommand { get; set; }
    public string[] Args { get; set; } = args;
}
