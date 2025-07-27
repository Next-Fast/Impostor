using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Impostor.Api.Config;
using Impostor.Api.Events;
using Impostor.Api.Extension.Commands;
using Impostor.Api.Games;
using Impostor.Api.Games.Managers;
using Impostor.Api.Net.Manager;
using Impostor.Api.Utils;
using Impostor.Server.Net.Manager;
using Impostor.Server.Net.State;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Impostor.Server.Commands;

public sealed class CommandManager(
    IServiceProvider provider,
    ILogger<CommandManager> logger,
    IOptions<AntiCheatConfig> config1,
    IOptions<CompatibilityConfig> config2,
    IOptions<TimeoutConfig> config3
) : ICommandManager
{
    private readonly List<ICommand> _allCommands = [];

    private readonly Dictionary<string, ISingleCommand> _singleCommands = [];
    private readonly List<ISystemCommand> _systemCommands = [];

    public IReadOnlyList<ICommand> Commands
    {
        get => _allCommands.AsReadOnly();
    }

    public ICommandManager RegisterCommand(ICommand command)
    {
        switch (command)
        {
            case ISingleCommand singleCommand:
                _singleCommands[singleCommand.Command] = singleCommand;
                break;
            case ISystemCommand systemCommand:
                _systemCommands.Add(systemCommand);
                break;
        }

        _allCommands.Add(command);
        return this;
    }

    public ICommandManager RegisterCommand<T>() where T : ICommand
    {
        return RegisterCommand(ActivatorUtilities.CreateInstance<T>(ServiceProvider));
    }

    public IServiceProvider ServiceProvider { get; } = provider;

    public async Task HandleCommandAsync(string commandString)
    {
        var args = commandString.Split(" ");
        var command = args[0];

        var argArray = args.Skip(1).ToArray();
        logger.LogDebug("HandleCommandAsync: {Command} {Args}", command, string.Join(" ", argArray));
        if (await HandleDefaultCommandAsync(command, argArray))
        {
            return;
        }
        
        foreach (var sc in _systemCommands)
        {
            if (await sc.InvokeAsync(command, argArray))
            {
                return;
            }
        }

        var eventArg = new CommandEventArgs(this, argArray);
        if (_singleCommands.TryGetValue(command, out var singleCommand))
        {
            var result = await singleCommand.InvokeAsync(eventArg);
            if (result.GetError(out var error))
            {
                logger.LogError("Command {Command} failed: {Error}", command, error);
            }
            
            return;
        }
        
        logger.LogError("Command {Command} not found", command);
        // BuildHelpMessage();
    }
    
    
    public Task HandleStringAsync()
    {
        _defaultCommands = ActivatorUtilities.CreateInstance<DefaultCommands>(ServiceProvider, ConfigSets);
        foreach (var method in typeof(DefaultCommands).GetMethods())
        {
            // 当方法继承自Object跳过
            if (method.DeclaringType == typeof(object))
                continue;
            
            logger.LogTrace("method Name : {name}", method.Name);
            var attrs = method.GetCustomAttributes<CommandRegister>().ToList();
            if (attrs.Count == 0)
            {
                logger.LogTrace("method has no CommandRegister attribute");
                continue;
            }

            if (method.ReturnType != typeof(Task<EventTypeResult>))
            {
                logger.LogTrace("method has wrong return type");
                continue;
            }

            var parameter = method.GetParameters();
            if (parameter.Length != 1 || parameter[0].ParameterType != typeof(CommandEventArgs))
            {
                logger.LogTrace("method has wrong Parameters");
                logger.LogTrace("Parameters Length:{length} {types}", parameter.Length, string.Join(" ", parameter.Select(n => n.ParameterType.Name)));
                continue;
            }

            Func<CommandEventArgs, Task<EventTypeResult>> action = args => (Task<EventTypeResult>)method.Invoke(method.IsStatic ? null : _defaultCommands, [args])!;
            foreach (var attr in attrs)
            {
                attr.Handler = action;
                _defaultCommands.Commands?.Add(attr);
            }
            
            logger.LogTrace("Register command {Command}", method.Name);
        }

        return Task.CompletedTask;
    }

    private DefaultCommands? _defaultCommands;

    private async ValueTask<bool> HandleDefaultCommandAsync(string command, string[] args)
    {
        if (_defaultCommands == null || _defaultCommands.Commands.Count == 0)
        {
            return false;
        }
        
        var eventArg = new CommandEventArgs(this, args);
        foreach (var defaultCommand in _defaultCommands.Commands.Where(defaultCommand => defaultCommand.Command == command))
        {
            if (defaultCommand.SubCommands.Length != 0)
            {
                var sub = eventArg.GetArg(0, null);
                if (string.IsNullOrEmpty(sub))
                {
                    continue;
                }

                if (!defaultCommand.SubCommands.Contains(sub))
                {
                    continue;
                }
                
                eventArg.SubCommand = sub;
            }
            else
            {
                eventArg.SubCommand = string.Empty;
            }

            var result = await defaultCommand.InvokeAsync(eventArg);
            if (result.Type == EventResultType.Cancelled)
            {
                continue;
            }
            
            if (result.GetError(out var error))
            {
                logger.LogError("Command {Command}{sub}failed: {Error}", command, error, !string.IsNullOrEmpty(eventArg.SubCommand) ? $" SubCommand:{eventArg.SubCommand} " : " ");
            }
            else
            {
                return true;
            }
        }

        return false;
    }
    
    public List<IConfigSet> ConfigSets { get; } =
    [
        config1.Value,
        config2.Value,
        config3.Value,
    ];
}

internal class DefaultCommands(
    ILogger<DefaultCommands> logger,
    List<IConfigSet> configSets,
    ClientAuthManager clientAuthManager,
    IClientManager clientManager,
    IGameManager gameManager,
    BanIpContent banIpContent
    )
{
    internal readonly List<CommandRegister> Commands = [];
    private readonly ClientManager _clientManager = (ClientManager)clientManager;
    
    private static string BuildConsoleList<T>(string title, IEnumerable<T> list, Func<T, string> func)
    {
        var all = list.ToList();
        var builder = new StringBuilder();
        
        builder.AppendLine($"[{title}]");

        if (all.Count == 0)
        {
            builder.AppendLine("No Data");
        }
        else
        {
            foreach (var item in all)
            {
                builder.AppendLine(func(item));
            }
        }
        builder.Append($"[{title} End]");
        return builder.ToString();
    }

    [CommandRegister("set")]
    public Task<EventTypeResult> HandleSetConfigAsync(CommandEventArgs args)
    {
        var name = args.GetArg(0, null);
        var config = configSets.FirstOrDefault(config => config.SectionName == name);
        if (config == null)
        {
            return Task.FromResult(EventTypeResult.CreateCancelled());
        }

        config.Set(args.GetArg(1, null), args.To(2));
        return Task.FromResult(EventTypeResult.CreateSuccess());
    }

    [CommandRegister("list", ["auth", "client", "ban", "game", "games"])]
    public Task<EventTypeResult> HandleListAsync(CommandEventArgs args)
    {
        if (args.SubCommand == "auth")
        {
            var authList = BuildConsoleList(
                "Auth List", 
                clientAuthManager.AllAuthInfo
                , 
                info => $"Name:{info.CacheName ?? "No Client"} Version:{info.Version} Id:{info.LastId} FriendCode:{info.FriendCode} Ban:{info.IsBanned}"
                );
            
            Console.WriteLine(authList);
            return Task.FromResult(EventTypeResult.CreateSuccess());
        }

        if (args.SubCommand == "client")
        {
            var clientList = BuildConsoleList(
                "Client List", 
                clientManager.Clients, 
                info => $"Name:{info.Name} Version:{info.GameVersion} Id:{info.Id} Language:{info.Language} Game:{info.Player?.Game.Code ?? "No Game"}"
                );
            Console.WriteLine(clientList);
            return Task.FromResult(EventTypeResult.CreateSuccess());
        }

        if (args.SubCommand == "ban")
        {
            var banList = BuildConsoleList(
                "Auth Ban List", 
                clientAuthManager.AllAuthInfo.Where(n => n.IsBanned), 
                info => $"Name:{info.CacheName ?? "No Client"} Version:{info.Version} Id:{info.LastId} FriendCode:{info.FriendCode}"
                );
            var banIpList = BuildConsoleList
            (
                "Ip Ban List",
                banIpContent._banIps,
                info => $"Ip:{info.IP} StartTime:{info.StartTime} EndTime:{info.EndTime}"
            );
            Console.WriteLine(banList);
            Console.WriteLine(banIpList);
            return Task.FromResult(EventTypeResult.CreateSuccess());
        }

        if (args.SubCommand == "games")
        {
            var gameList = BuildConsoleList(
                    "Game List",
                    gameManager.Games,
                    game => $"" +
                            $"Name:{game.Code}" +
                            $"Host:{game.Host?.Client.Name ?? "Unknown"}" +
                            $"Players:{game.PlayerCount}" +
                            $"Max:{game.Options.MaxPlayers}" +
                            $"State:{game.GameState}" +
                            $"IsPublic:{game.IsPublic}" + 
                            $"IsHostAuthoritative:{game.IsHostAuthoritive}"
                            );
            Console.WriteLine(gameList);
            return Task.FromResult(EventTypeResult.CreateSuccess());
        }

        if (args.SubCommand == "games")
        {
            var code = args.GetArg(1, null);
            if (string.IsNullOrEmpty(code))
            {
                return Task.FromResult(EventTypeResult.CreateError("NoGetGameId"));
            }

            if (!gameManager.Games.TryGet(g => g.Code.Code == GameCode.From(code), out var result))
            {
                return Task.FromResult(EventTypeResult.CreateError("GameNotFound"));
            }

            var list = BuildConsoleList(
                "Game Player List",
                result.Players,
                p => $"Name:{p.Client.Name} Id:{p.Client.Id} IsHost:{p.IsHost}");
            Console.WriteLine(list);
            return Task.FromResult(EventTypeResult.CreateSuccess());
        }
        
        return Task.FromResult(EventTypeResult.CreateCancelled());
    }

    [CommandRegister("kick", ["player"])]
    [CommandRegister("remove", ["game", "player"])]
    public async Task<EventTypeResult> HandleRemoveAsync(CommandEventArgs args)
    {
        var code = args.GetArg(1, null);
        if (string.IsNullOrEmpty(code))
        {
            return EventTypeResult.CreateError("NoGetGameId");
        }
        
        if (!gameManager.Games.TryGet(g => g.Code.Code == GameCode.From(code), out var result))
        {
            return EventTypeResult.CreateError("GameNotFound");
        }

        var game = (Game)result;
        if (args.SubCommand == "game")
        {
            await game.DestroyGameAsync();
            return EventTypeResult.CreateSuccess();
        }


        if (args.SubCommand == "player")
        {
            var clientId = args.GetArg(2, -1);
            if (clientId == -1)
            {
                return EventTypeResult.CreateError("NoGetClientId");
            }

            if (!game.TryGetPlayer(clientId, out var player))
            {
                return EventTypeResult.CreateError("PlayerNotFound");
            }

            await player.KickAsync();
        }
        
        return EventTypeResult.CreateCancelled();
    }

    [CommandRegister("ban", ["auth", "client", "Ip"])]
    public async Task<EventTypeResult> HandleBanAsync(CommandEventArgs args)
    {
        if (args.SubCommand == "auth")
        {
            var id = args.GetArg(1, -1);
            if (id == -1)
            {
                return EventTypeResult.CreateError("NoGetBanTargetId");
            }
            
            if (!clientAuthManager.TryGetAuthInfo((uint)id, out var info))
            {
                return EventTypeResult.CreateError("AuthInfoNotFound");
            }

            info.IsBanned = true;
            return EventTypeResult.CreateSuccess();
        }

        if (args.SubCommand == "client")
        {
            var id = args.GetArg(1, -1);
            if (id == -1)
            {
                return EventTypeResult.CreateError("NoGetBanTargetId");
            }
            
            var client = clientManager.Clients.FirstOrDefault(n => n.Id == id);
            if (client == null)
            {
                return EventTypeResult.CreateError("ClientNotFound");
            }

            await _clientManager.BanAsync(client);
            return EventTypeResult.CreateSuccess();
        }

        if (args.SubCommand == "Ip")
        {
            var ipString = args.GetArg(1, null);
            if (ipString == string.Empty)
            {
                return EventTypeResult.CreateError("NoGetBanTargetIp");
            }
            var ip = IPAddress.Parse(ipString);
            var time = args.GetArg<long>(2, -1);
            TimeSpan? timeSpan;
            if (time == -1)
            {
                timeSpan = null;
            }
            else
            {
                timeSpan = TimeSpan.FromMinutes(time);
            }
            
            await _clientManager.BanAsync(ip, timeSpan ?? TimeSpan.FromMinutes(10));
            return EventTypeResult.CreateSuccess();
        }
        
        return EventTypeResult.CreateCancelled();
    }
    
    [CommandRegister("unban", ["auth", "ip"])]
    public Task<EventTypeResult> HandleUnBanAsync(CommandEventArgs args)
    {
        if (args.SubCommand == "auth")
        {
            var id = args.GetArg(1, -1);
            if (id == -1)
            {
                return Task.FromResult(EventTypeResult.CreateError("NoGetBanTargetId"));
            }
            
            if (!clientAuthManager.TryGetAuthInfo((uint)id, out var info))
            {
                return Task.FromResult(EventTypeResult.CreateError("AuthInfoNotFound"));
            }

            info.IsBanned = false;
            return Task.FromResult(EventTypeResult.CreateSuccess());
        }

        if (args.SubCommand == "ip")
        {
            var ipString = args.GetArg(1, null);
            if (ipString == string.Empty)
            {
                logger.LogWarning("NoGetBanTargetIp");
                return Task.FromResult(EventTypeResult.CreateError("NoGetBanTargetIp"));
            }
            
            banIpContent.UnBan(ipString);
            return Task.FromResult(EventTypeResult.CreateSuccess());
        }

        return Task.FromResult(EventTypeResult.CreateCancelled());
    }
}

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
// ReSharper disable once ClassNeverInstantiated.Global
public class CommandRegister(string command) : Attribute, ISingleCommand
{
    public string Command { get; } = command;
    
    public Func<CommandEventArgs, Task<EventTypeResult>>? Handler { get; set; }
    
    public string[] SubCommands { get; set; } = [];

    public CommandRegister(string command, string[] subCommands) : this(command)
    {
        SubCommands = subCommands;
    }

    private static readonly EventTypeResult NotInvoke = EventTypeResult.CreateError("CommandNotInvoke");
    public Task<EventTypeResult> InvokeAsync(CommandEventArgs args)
    {
        return Handler?.Invoke(args) ?? Task.FromResult(NotInvoke);
    }
}
