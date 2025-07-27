using System.Text.Json;
using Impostor.Api.Data;
using Impostor.Api.Extension.Utils;
using Microsoft.Extensions.Options;

namespace SelfHttpMatchmaker;

public class CacheAuthFriendCodeContent(IOptions<SelfHttpConfig> config) : IContent
{
    public string Name => "FriendCodeAuth";
    public bool Enable => config.Value.FriendCodeAuth;

    internal class FriendCodeAuth(string userToken, string puid, string friendCode, string matchmakerToken, DateTime lastAuthTime)
    {
        public string Puid { get; set; } = puid;
        public string UserToken { get; set; } = userToken;
        public string FriendCode { get; set; } = friendCode;
        public string MatchmakerToken { get; set; } = matchmakerToken;
        public DateTime LastAuthTime { get; set; } = lastAuthTime;
    }
    
    internal List<FriendCodeAuth> _friendCodeAuths = [];
    
    public Task<string> SerialiseAsync()
    {
        var json = JsonUtils.Serialize(_friendCodeAuths);
        return Task.FromResult(json);
    }

    public Task DeserializeAsync(string data)
    {
        var json = JsonUtils.Deserialize<List<FriendCodeAuth>>(data);
        if (json != null)
        {
            _friendCodeAuths = json;
        }
        return Task.CompletedTask;
    }
}
