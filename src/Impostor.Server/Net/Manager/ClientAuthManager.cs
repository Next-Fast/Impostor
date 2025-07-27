using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Impostor.Api.Config;
using Impostor.Api.Data;
using Impostor.Api.Events;
using Impostor.Api.Events.Managers;
using Impostor.Api.Extension.Events;
using Impostor.Api.Extension.Net;
using Impostor.Api.Extension.Utils;
using Impostor.Api.Innersloth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Impostor.Server.Net.Manager;

public class ClientAuthManager(ILogger<ClientAuthManager> logger, IEventManager eventManager, IServiceProvider provider)
{
    internal List<ClientAuthInfo> AuthInfos { get; set; } = [];
    
    public IReadOnlyList<ClientAuthInfo> AllAuthInfo => AuthInfos.AsReadOnly();

    private readonly ServiceUtils.ServiceCacheGet<ContentDBService> _db = new(provider);

    public uint GetNextId()
    {
        var count = 0;
        while (true)
        {
            if (count > 5)
            {
                break;
            }

            if (TryGetNextId(out var id))
            {
                return id;
            }

            count++;
        }

        logger.LogError("Failed to get next id");
        return 0;
    }

    private bool TryGetNextId(out uint id)
    {
        var randomId = (uint)Random.Shared.NextInt64(1, 10000);
        if (AuthInfos.Any(n => n.LastId == randomId))
        {
            id = 0;
            return false;
        }

        id = randomId;
        return true;
    }

    public void RemoveAuthInfo(uint id)
    {
        AuthInfos.RemoveAll(n => n.LastId == id);
        logger.LogInformation("Remove authInfo:{id}", id);
    }
    
    public async Task<ClientAuthInfo?> CreateAuthInfoAsync(GameVersion version, Platforms platform, string matchmakerToken,
        string friendCode, IPAddress targetIp)
    {
        ClientAuthInfo? info;
        if (TryGetAuthInfo(n => n.MatchmakerToken == matchmakerToken || n.FriendCode == friendCode, out var authInfo))
        {
            authInfo.Version = version;
            authInfo.Platform = platform;
            authInfo.MatchmakerToken = matchmakerToken;
            authInfo.FriendCode = friendCode;
            authInfo.TargetIp = targetIp;
            info = authInfo;
        }
        else
        {
            var id = GetNextId();
            if (id == 0)
            {
                return null;
            }

            info = new ClientAuthInfo(id, version, platform, matchmakerToken, friendCode, targetIp);
        }
        
        var eventResult = await RunCreateEvent(info);
        if (!eventResult.Item1)
        {
            return null;
        }

        AuthInfos.Add(eventResult.Item2);
        logger.LogInformation("Create authInfo:{id} {version}, {platform}, {token}, {code}", info.LastId, version, platform,
            matchmakerToken, friendCode);
        return eventResult.Item2;

        async Task<(bool,ClientAuthInfo)> RunCreateEvent(ClientAuthInfo create)
        {
            var @event = new ClientAuthCreateEvent(create);
            await eventManager.CallAsync(@event);
            if (@event.Status.IsSuccess())
            {
                return (true, @event.Result ?? create);
            }

            switch (@event.Status.Type)
            {
                case EventResultType.Error:
                    logger.LogError("Failed to create authInfo:{version}, {platform}, {token}, {code}, {reason}",
                        version, platform,
                        matchmakerToken, friendCode, @event.Status.Message);
                    break;
                case EventResultType.Cancelled:
                    logger.LogInformation("Cancelled to create authInfo:{version}, {platform}, {token}, {code}",
                        version, platform,
                        matchmakerToken, friendCode);
                    break;
            }

            return (false, create);
        }
    }

    private bool TryGetAuthInfo(Func<ClientAuthInfo, bool> predicate, [MaybeNullWhen(false)] out ClientAuthInfo info)
    {
        var find = AuthInfos.FirstOrDefault(predicate);
        info = find;
        return find != null;
    }

    public bool TryGetAuthInfo(uint id, [MaybeNullWhen(false)] out ClientAuthInfo info)
    {
        return TryGetAuthInfo(n => n.LastId == id, out info);
    }
    
    private const string ContentName = "AuthInfo";
    public class AuthInfoContent(ClientAuthManager clientAuthManager, ILogger<AuthInfoContent> logger, IOptions<ServerConfig> config) : IContent
    {
        public bool Enable => config.Value.SaveAuthInfo;
        public string Name => ContentName;
        public Task<string> SerialiseAsync()
        {
            var content = clientAuthManager.AllAuthInfo;
            var text = JsonUtils.Serialize(content);
            return Task.FromResult(text);
        }

        public Task DeserializeAsync(string data)
        {
            var allAuth = JsonUtils.Deserialize<List<ClientAuthInfo>>(data);
            if (allAuth == null)
            {
                logger.LogError("Failed to deserialize authInfo");
                return Task.CompletedTask;
            }
            
            clientAuthManager.AuthInfos = allAuth;
            logger.LogInformation("AuthInfo Load Completed {count}", allAuth.Count);
            return Task.CompletedTask;
        }
    }
}
