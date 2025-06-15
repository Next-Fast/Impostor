using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Impostor.Api.Plugins;

public interface IPluginStartup
{
    void ConfigureHost(IHostBuilder host) { }

    void ConfigureServices(HostBuilderContext context, IServiceCollection services) => ConfigureServices(services);
    void ConfigureServices(IServiceCollection services) { }

    void ConfigureConfiguration(IConfigurationBuilder config) { }
}
