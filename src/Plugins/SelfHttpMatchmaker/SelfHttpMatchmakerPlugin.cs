using Impostor.Api.Extension.Plugins;
using Impostor.Api.Extension.Utils;
using Impostor.Api.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SelfHttpMatchmaker.Types;

namespace SelfHttpMatchmaker;

[ImpostorPlugin("SelfHttpMatchmaker.Impostor.Next")]
public class SelfHttpMatchmakerPlugin : IPlugin, IHttpPluginStartup
{
    public bool AssemblyPart
    {
        get => true;
    }

    public void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.ConfigureSection<Config>(context.Configuration, Config.Section);
        services.AddSingleton<ListingManager>();
        services.AddSingleton<IHostServer, HostServerGet>();
    }
}
