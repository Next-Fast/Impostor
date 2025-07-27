using Impostor.Api.Extension.Plugins;
using Impostor.Api.Extension.Utils;
using Impostor.Api.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NextMatchPlugin.Types;

namespace NextMatchPlugin;

[ImpostorPlugin("NextMatchPlugin")]
public class NextMatchPlugin : IPlugin
{
    
}

public class NextMatchPluginStartup : IHttpPluginStartup
{
    public void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services
            .ConfigureSection<NextMatchConfig>(context.Configuration, NextMatchConfig.Section)
            .AddHostedService<MatchmakerService>()
            .AddSingleton<ConnectionActioner>();
    }
}
