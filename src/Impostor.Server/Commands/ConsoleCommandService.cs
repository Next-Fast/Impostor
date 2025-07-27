using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Impostor.Api.Config;
using Impostor.Api.Extension.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Impostor.Server.Commands;

public class ConsoleCommandService(
    IServiceProvider serviceProvider,
    ILogger<ConsoleCommandService> logger,
    IOptions<ServerConfig> config,
    ICommandManager commandManager) : BackgroundService
{
    private readonly ServerConfig _config = config.Value;

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!config.Value.EnableCommands)
        {
            logger.LogInformation("Commands are disabled in the config");
            return;
        }


        Console.OutputEncoding = Console.InputEncoding = Encoding.UTF8;
        logger.LogInformation("Starting ConsoleCommandService");
        await commandManager.HandleStringAsync();
        foreach (var command in serviceProvider.GetServices<ICommand>())
        {
            commandManager.RegisterCommand(command);
        }
        
        await base.StartAsync(cancellationToken);
    }

    private static bool IsDocker()
    {
        return File.Exists("/.dockerenv");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (IsDocker() && Console.IsInputRedirected)
            {
                break;
            }
            
            var task = Task.Run(Console.ReadLine, stoppingToken);
            await Task.WhenAny(task, Task.Delay(Timeout.Infinite, stoppingToken));

            var line = task.IsCompleted ? task.GetAwaiter().GetResult() : null;
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }

            logger.LogDebug("Received input: {line}", line);

            var prefix = _config.CommandPrefix;
            var trimLine = line.Trim();
            if (!trimLine.StartsWith(prefix))
            {
                continue;
            }

            var command = trimLine.Remove(0, prefix.Length);
            await commandManager.HandleCommandAsync(command);
        }
    }
}
