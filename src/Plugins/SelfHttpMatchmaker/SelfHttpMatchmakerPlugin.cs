using Impostor.Api.Data;
using Impostor.Api.Events;
using Impostor.Api.Extension.Commands;
using Impostor.Api.Extension.Events;
using Impostor.Api.Extension.Plugins;
using Impostor.Api.Extension.Utils;
using Impostor.Api.Plugins;
using Impostor.Api.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SelfHttpMatchmaker.Types;

namespace SelfHttpMatchmaker;

[ImpostorPlugin("SelfHttpMatchmaker.Impostor.Next")]
public class SelfHttpMatchmakerPlugin(IOptions<SelfHttpConfig> config, IServiceProvider provider) : IPlugin
{
    public ValueTask EnableAsync()
    {
        TokenUtils.TokenAuthEnabled = config.Value.EnableMatchmakerTokenAuth;
        TokenUtils.Token = config.Value.Token;
        TokenUtils.TokenExpiresTime = TimeSpan.FromMinutes(config.Value.TokenExpiresMinutes);

        var commandManager = provider.GetService<ICommandManager>();
        commandManager?.ConfigSets.Add(config.Value);
        return ValueTask.CompletedTask;
    }
}

public class SelfHttpListener(IOptions<SelfHttpConfig> config, CacheAuthFriendCodeContent content) : IEventListener
{
    [EventListener]
    public void OnCreateAuth(ClientAuthCreateEvent @event)
    {
        if (!config.Value.EnableClientTokenAuth)
        {
            return;
        }
        
        var token = @event.DefaultAuthInfo.MatchmakerToken;
        if (!token.VerifyToken(out _))
        {
            @event.Status = new EventTypeResult(EventResultType.Cancelled);
        }

        if (!config.Value.FriendCodeAuth)
        {
            return;
        }
        
        var friendCode = @event.DefaultAuthInfo.FriendCode;
        if (content._friendCodeAuths.TryGet(i => i.MatchmakerToken == @event.DefaultAuthInfo.MatchmakerToken, out var info) && info.FriendCode != friendCode)
        {
            @event.Status = new EventTypeResult(EventResultType.Cancelled);
        }
    }
}

public class SelfHttpMatchmakerStartup : IHttpPluginStartup
{
    public bool AssemblyPart
    {
        get => true;
    }

    public void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        services.ConfigureSection<SelfHttpConfig>(context.Configuration, SelfHttpConfig.Section);
        services.AddSingleton<ListingManager>();
        services.AddSingleton<IEventListener, SelfHttpListener>();
        services.AddSingleton<IHostServer, HostServerGet>();
        services.AddRequiredSingleton<IContent, CacheAuthFriendCodeContent>();
    }
}
