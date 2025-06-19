using System.Net;
using Impostor.Api.Innersloth;

namespace Impostor.Api.Extension.Net;

public class ClientAuthInfo(
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
