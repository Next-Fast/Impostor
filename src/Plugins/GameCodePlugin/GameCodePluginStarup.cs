using Impostor.Api.Events;
using Impostor.Api.Extension.Utils;
using Impostor.Api.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace GameCodePlugin;

public class GameCodePluginStartup : IPluginStartup
{
    
    public void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services
            .ConfigureSection<GameCodeConfig>(context.Configuration, GameCodeConfig.Section)
            .AddSingleton<IEventListener, GameCodeEventListener>()
            .AddSingleton<GameCodeStateManager>();
    }
}
