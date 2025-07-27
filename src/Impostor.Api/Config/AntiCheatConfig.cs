using Impostor.Api.Utils;

namespace Impostor.Api.Config;

public class AntiCheatConfig : IConfigSet
{
    public const string Section = "AntiCheat";

    public bool Enabled { get; set; } = true;

    public bool BanIpFromGame { get; set; } = true;

    public CheatingHostMode AllowCheatingHosts { get; set; } = CheatingHostMode.Never;

    public bool EnableGameFlowChecks { get; set; } = true;

    public bool EnableMustBeHostChecks { get; set; } = true;

    public bool EnableColorLimitChecks { get; set; } = true;

    public bool EnableNameLimitChecks { get; set; } = true;

    public bool EnableOwnershipChecks { get; set; } = true;

    public bool EnableRoleChecks { get; set; } = true;

    public bool EnableTargetChecks { get; set; } = true;

    public bool EnableInvalidObjectChecks { get; set; } = true;

    public bool ForbidProtocolExtensions { get; set; } = true;
    public string SectionName => Section;
    public void Set(string key, IArgUtils value)
    {
        switch (key)
        {
            case "Enabled":
                Enabled = value.GetArg(0, Enabled);
                break;
            case "BanIpFromGame":
                BanIpFromGame = value.GetArg(0, BanIpFromGame);
                break;
            case "AllowCheatingHosts":
                AllowCheatingHosts = value.GetArg(0, AllowCheatingHosts);
                break;
            case "EnableGameFlowChecks":
                EnableGameFlowChecks = value.GetArg(0, EnableGameFlowChecks);
                break;
            case "EnableMustBeHostChecks":
                EnableMustBeHostChecks = value.GetArg(0, EnableMustBeHostChecks);
                break;
            case "EnableColorLimitChecks":
                EnableColorLimitChecks = value.GetArg(0, EnableColorLimitChecks);
                break;
            case "EnableNameLimitChecks":
                EnableNameLimitChecks = value.GetArg(0, EnableNameLimitChecks);
                break;
            case "EnableOwnershipChecks":
                EnableOwnershipChecks = value.GetArg(0, EnableOwnershipChecks);
                break;
            case "EnableRoleChecks":
                EnableRoleChecks = value.GetArg(0, EnableRoleChecks);
                break;
            case "EnableTargetChecks":
                EnableTargetChecks = value.GetArg(0, EnableTargetChecks);
                break;
            case "EnableInvalidObjectChecks":
                EnableInvalidObjectChecks = value.GetArg(0, EnableInvalidObjectChecks);
                break;
            case "ForbidProtocolExtensions":
                ForbidProtocolExtensions = value.GetArg(0, ForbidProtocolExtensions);
                break;
        }
    }
}
