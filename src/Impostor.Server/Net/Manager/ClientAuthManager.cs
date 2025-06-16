using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using Impostor.Api.Innersloth;
using Microsoft.Extensions.Logging;

namespace Impostor.Server.Net.Manager;

public class ClientAuthManager(ILogger<ClientAuthManager> logger)
{
    private List<AuthInfo> AuthInfos { get; } = [];

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
        var randomId = (uint)Random.Shared.NextInt64(uint.MinValue, uint.MaxValue);
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

    public uint CreateAuthInfo(GameVersion version, Platforms platform, string matchmakerToken, string friendCode, IPAddress targetIp)
    {
        var id = GetNextId();
        if (id == 0)
        {
            return 0;
        }
        
        if (TryGetAuthInfo(n => n.MatchmakerToken == matchmakerToken || n.FriendCode == friendCode, out var authInfo))
        {
            authInfo.LastId = id;
            authInfo.Version = version;
            authInfo.Platform = platform;
            authInfo.MatchmakerToken = matchmakerToken;
            authInfo.FriendCode = friendCode;
            authInfo.TargetIp = targetIp;
            return id;
        }

        var info = new AuthInfo(id, version, platform, matchmakerToken, friendCode, targetIp);
        AuthInfos.Add(info);
        logger.LogInformation("Create authInfo:{id} {version}, {platform}, {token}, {code}", id, version, platform,
            matchmakerToken, friendCode);
        return id;
    }

    private bool TryGetAuthInfo(Func<AuthInfo, bool> predicate, [MaybeNullWhen(false)] out AuthInfo info)
    {
        var find = AuthInfos.FirstOrDefault(predicate);
        info = find;
        return find != null;
    }

    public bool TryGetAuthInfo(uint id, [MaybeNullWhen(false)] out AuthInfo info)
    {
        return TryGetAuthInfo(n => n.LastId == id, out info);
    }

    public class AuthInfo(
        uint lastId,
        GameVersion version,
        Platforms platform,
        string matchmakerToken,
        string friendCode,
        IPAddress targetIp
        )
    {
        public IPAddress TargetIp { get; set; } = targetIp;
        public uint LastId { get; set; } = lastId;
        public GameVersion Version { get; set; } = version;
        public Platforms Platform { get; set; } = platform;
        public string MatchmakerToken { get; set; } = matchmakerToken;
        public string FriendCode { get; set; } = friendCode;
    }
}
