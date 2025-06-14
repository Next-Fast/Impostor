using Impostor.Api.Extension.Plugins;
using Impostor.Api.Plugins;
using Microsoft.Extensions.DependencyInjection;
using SelfHttpMatchmaker.Types;

namespace SelfHttpMatchmaker;

[ImpostorPlugin("SelfHttpMatchmaker.Impostor.Next")]
public class SelfHttpMatchmakerPlugin : IPlugin, IHttpPluginStartup
{
    public bool AssemblyPart => true;
    

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<ListingManager>();
        services.AddSingleton<IHostServer, HostServerGet>();
    }
}
