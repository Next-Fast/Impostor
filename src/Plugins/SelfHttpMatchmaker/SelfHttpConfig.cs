namespace SelfHttpMatchmaker;

public class SelfHttpConfig
{
    public const string Section = "SelfHttpMatchmaker";
    public string RegionName { get; set; } = "Impostor";

    public string Token { get; set; } = "TianMeng_Impostor_Server_Token";
    public int TokenExpiresMinutes { get; set; } = 30;
    public bool EnableMatchmakerTokenAuth { get; set; } = false;

    public bool PutTokenAuth { get; set; } = false;

    public bool EnableClientTokenAuth { get; set; } = false;
}
