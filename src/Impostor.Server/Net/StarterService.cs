using System.Threading;
using System.Threading.Tasks;
using Impostor.Api.Config;
using Impostor.Server.Net.Manager;
using Impostor.Server.Utils;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Impostor.Server.Net;

internal class StarterService(NetListenerManager listenerManager, MatchmakerManager matchmakerManager, IOptions<ServerConfig> serverConfigOption)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var serverConfig = serverConfigOption.Value;
        
        await listenerManager.CreateAll(serverConfig.Listeners)
                             .StartAllAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await listenerManager.StopAllAsync();
    }
}
