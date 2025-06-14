using Impostor.Api.Extension.Plugins;
using Impostor.Api.Plugins;
using Scalar.AspNetCore;

namespace HttpDebugPlugin;

[ImpostorPlugin("SelfHttpMatchmaker.Impostor.Next")]
public class HttpDebugPlugin : IPlugin, IHttpPluginStartup
{
    public bool AssemblyPart
    {
        get => true;
    }

    public void ConfigureHost(IWebHostBuilder host)
    {
        host.ConfigureServices(services =>
        {
            services.AddOpenApi();
        });
    }

    public void ConfigureWebApplication(IApplicationBuilder app)
    {
        app.UseEndpoints(endpoint =>
        {
            endpoint.MapOpenApi();
            endpoint.MapScalarApiReference();
        });
    }
}
