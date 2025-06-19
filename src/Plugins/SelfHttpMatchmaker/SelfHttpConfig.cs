using Impostor.Api.Config;

namespace SelfHttpMatchmaker;

public class SelfHttpConfig
{
    public const string Section = "SelfHttpMatchmaker";
    public string RegionName { get; set; } = "Impostor";
    
    public bool EnableMatchmakerTokenAuth { get; set; } = false;

    public bool PutTokenAuth { get; set; } = false;
    
    public bool EnableClientTokenAuth { get; set; } = false;
}
