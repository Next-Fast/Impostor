using Impostor.Api.Utils;

namespace Impostor.Api.Config;

public class CompatibilityConfig : IConfigSet
{
    public const string Section = "Compatibility";

    public bool AllowFutureGameVersions { get; set; } = false;

    public bool AllowHostAuthority { get; set; } = true;

    public bool AllowVersionMixing { get; set; } = false;
    public string SectionName => Section;
    public void Set(string key, IArgUtils value)
    {
        switch (key)
        {
            case "AllowFutureGameVersions":
                AllowFutureGameVersions = value.GetArg(0, false);
                break;
            case "AllowHostAuthority":
                AllowHostAuthority = value.GetArg(0, true);
                break;
            case "AllowVersionMixing":
                AllowVersionMixing = value.GetArg(0, false);
                break;
        }
    }
}
