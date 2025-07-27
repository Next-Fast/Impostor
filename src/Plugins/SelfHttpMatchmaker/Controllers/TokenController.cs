using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Impostor.Api.Extension.Commands;
using Impostor.Api.Innersloth;
using Impostor.Api.Net.Manager;
using Impostor.Api.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SelfHttpMatchmaker.Types;

namespace SelfHttpMatchmaker.Controllers;

/// <summary>
///     This controller has a method to get an auth token.
/// </summary>
[Route("/api/user")]
[ApiController]
public sealed class TokenController(ICompatibilityManager manager, CacheAuthFriendCodeContent content, IOptions<SelfHttpConfig> config, ILogger<TokenController> logger, BanIpContent banIpContent) : ControllerBase
{
    /// <summary>
    ///     Get an authentication token.
    /// </summary>
    /// <param name="request">Token parameters that need to be put into the token.</param>
    /// <param name="authorization"></param>
    /// <returns>A bare minimum authentication token that the client will accept.</returns>
    [HttpPost]
    public async Task<IActionResult> GetTokenAsync([FromBody] TokenRequest request, [FromHeader] AuthenticationHeaderValue authorization)
    {
        if (banIpContent.CheckIsBan(Request.HttpContext.Connection.RemoteIpAddress))
        {
            return BadRequest(new MatchmakerError(DisconnectReason.Error, "You are banned"));
        }
        
        if (authorization.Scheme != "Bearer")
        {
            return BadRequest(new MatchmakerError(DisconnectReason.Error, "Invalid Authorization Header"));
        }

        var token = authorization.Parameter;
        if (token == null)
        {
            return BadRequest(new MatchmakerError(DisconnectReason.Error, "Invalid Authorization Header"));
        }

        var version = new GameVersion(request.ClientVersion);
        if (version < manager.MinSupportedVersion)
        {
            return BadRequest(new MatchmakerError(DisconnectReason.Error, "Unsupported Game Version"));
        }

        var matchmakerToken = request.GenerateTokenResponse();
        if (config.Value.FriendCodeAuth && !await GetOrUpdateFriendCodeAsync(request.ProductUserId, token, matchmakerToken))
        {
            return BadRequest(new MatchmakerError(DisconnectReason.Error, "Invalid Friend Code"));
        }
        
        return Ok(matchmakerToken);
    }

    private readonly HttpClient _client = new()
    {
        Timeout = TimeSpan.FromMinutes(1),
    };

    // From Niko's code
    private async ValueTask<string> GetFriendCodeAsync(string token)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://backend.innersloth.com/api/user/username");
            request.Headers.Add("Content-Type", "application/vnd.api+json");
            request.Headers.Add("User-Agent", "UnityPlayer/2022.3.44f1 (UnityWebRequest/1.0, libcurl/7.84.0-DEV)");
            request.Headers.Add("X-Unity-Version", "2022.3.44f1");
            request.Headers.Add("Authorization", "Bearer " + token);
        
            var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }
        
            var jsonContent = await response.Content.ReadAsStringAsync();
            var responseFriendCode = JsonSerializer.Deserialize<ResponseFriendCode>(jsonContent);
            if (responseFriendCode == null)
            {
                return string.Empty;
            }
            var friendCode = responseFriendCode.Username + "#" + responseFriendCode.Discriminator;
            return string.Empty;
        }
        catch (Exception e)
        {
            logger.LogTrace("Exception while getting friend code: {e}", e);
        }

        return string.Empty;
    }
    
    private async ValueTask<bool> GetOrUpdateFriendCodeAsync(string puid, string token, string matchmakerToken)
    {
        if (content._friendCodeAuths.TryGet(i => i.UserToken == token, out var getInfo))
        {
            if (getInfo.LastAuthTime < DateTime.Now && getInfo.MatchmakerToken == matchmakerToken)
            {
                return true;
            }
        }
        
        var friendCode = await GetFriendCodeAsync(token);
        if (string.IsNullOrEmpty(friendCode))
        {
            return false;
        }

        var time = DateTime.Now.AddHours(4);
        if (getInfo != null)
        {
            getInfo.FriendCode = friendCode;
            getInfo.MatchmakerToken = matchmakerToken;
            getInfo.LastAuthTime = DateTime.Now.AddHours(4);
        }
        else
        {
            var info = new CacheAuthFriendCodeContent.FriendCodeAuth(token, puid, friendCode, matchmakerToken, time);
            content._friendCodeAuths.Add(info);
        }
        
        return true;
        
    }
    
    public class ResponseFriendCode
    {
        [JsonPropertyName("username")]
        public required string Username { get; set; }
        
        [JsonPropertyName("discriminator")]
        public required string Discriminator { get; set; }
    }
}
