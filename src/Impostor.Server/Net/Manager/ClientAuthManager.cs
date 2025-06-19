using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Impostor.Api.Events;
using Impostor.Api.Events.Managers;
using Impostor.Api.Extension.Events;
using Impostor.Api.Extension.Net;
using Impostor.Api.Innersloth;
using Microsoft.Extensions.Logging;

namespace Impostor.Server.Net.Manager;

public class ClientAuthManager(ILogger<ClientAuthManager> logger, IEventManager eventManager)
{
    private List<ClientAuthInfo> AuthInfos { get; } = [];

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
        var randomId = (uint)Random.Shared.NextInt64(1, uint.MaxValue);
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

    public async Task<uint> CreateAuthInfoAsync(GameVersion version, Platforms platform, string matchmakerToken,
        string friendCode, IPAddress targetIp)
    {
        if (TryGetAuthInfo(n => n.MatchmakerToken == matchmakerToken || n.FriendCode == friendCode, out var authInfo))
        {
            authInfo.Version = version;
            authInfo.Platform = platform;
            authInfo.MatchmakerToken = matchmakerToken;
            authInfo.FriendCode = friendCode;
            authInfo.TargetIp = targetIp;
            return authInfo.LastId;
        }

        var id = GetNextId();
        if (id == 0)
        {
            return 0;
        }

        var info = new ClientAuthInfo(id, version, platform, matchmakerToken, friendCode, targetIp);
        var @event = new ClientAuthCreateEvent(info);
        await eventManager.CallAsync(@event);
        if (@event.Status.Type is EventResultType.Cancelled or EventResultType.Error)
        {
            if (@event.Status.Type is EventResultType.Error)
            {
                logger.LogError("Failed to create authInfo:{id} {version}, {platform}, {token}, {code}, {reason}", id,
                    version, platform,
                    matchmakerToken, friendCode, @event.Status.Message);
            }
            else
            {
                logger.LogInformation("Cancelled to create authInfo:{id} {version}, {platform}, {token}, {code}", id,
                    version, platform,
                    matchmakerToken, friendCode);
            }

            return 0;
        }

        AuthInfos.Add(info);
        logger.LogInformation("Create authInfo:{id} {version}, {platform}, {token}, {code}", id, version, platform,
            matchmakerToken, friendCode);
        return id;
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
}
