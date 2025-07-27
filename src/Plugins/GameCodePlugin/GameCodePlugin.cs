using Impostor.Api.Extension.Commands;
using Impostor.Api.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameCodePlugin;

[ImpostorPlugin("GameCodePlugin.Impostor.Next")]
public sealed class GameCodePlugin(
    GameCodeStateManager stateManager,
    IHostEnvironment env,
    ILogger<GameCodePlugin> logger,
    IOptions<GameCodeConfig> config,
    IServiceProvider provider
    ) : IPlugin
{
    public async ValueTask EnableAsync()
    {
        logger.LogInformation("GameCodePlugin enabled!");
        var root = env.ContentRootPath;
        
        var dir = config.Value.GameCodeDir.Replace("{Root}", root);
        try
        {
            await stateManager.LoadCodeAsync(new DirectoryInfo(dir));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to load game code");
        }
        
        var commandManager = provider.GetService<ICommandManager>();
        commandManager?.ConfigSets.Add(config.Value);
    }
}
