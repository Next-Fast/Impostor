using Impostor.Api.Config;
using Impostor.Api.Utils;

namespace SelfHttpMatchmaker;

public class SelfHttpConfig : IConfigSet
{
    public const string Section = "SelfHttpMatchmaker";

    public string Token { get; set; } = "TianMeng_Impostor_Server_Token";
    public int TokenExpiresMinutes { get; set; } = 30;
    public bool EnableMatchmakerTokenAuth { get; set; }

    public bool PutTokenAuth { get; set; }

    public bool EnableClientTokenAuth { get; set; }
    public string SectionName => Section;

    public bool FriendCodeAuth
    {
        get;
        set;
    }
    
    public void Set(string key, IArgUtils value)
    {
        switch (key)
        {
            case "RegionName":
                RegionName = value.GetArg(0, RegionName);
                break;
            case "Token":
                TokenUtils.Token = value.GetArg(0, Token);
                break;
            case "TokenExpiresMinutes":
                TokenUtils.TokenExpiresTime = TimeSpan.FromMinutes(value.GetArg(0, TokenExpiresMinutes));
                break;
            case "EnableMatchmakerTokenAuth":
                EnableMatchmakerTokenAuth = value.GetArg(0, EnableMatchmakerTokenAuth);
                break;
            case "PutTokenAuth":
                PutTokenAuth = value.GetArg(0, PutTokenAuth);
                break;
            case "EnableClientTokenAuth":
                EnableClientTokenAuth = value.GetArg(0, EnableClientTokenAuth);
                break;
            case "FriendCodeAuth":
                FriendCodeAuth = value.GetArg(0, FriendCodeAuth);
                break;
        }
    }
}
