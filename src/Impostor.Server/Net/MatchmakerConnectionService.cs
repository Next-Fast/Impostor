using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Impostor.Server.Net;

public class MatchmakerConnectionService : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
