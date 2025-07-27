using Impostor.Api.Utils;

namespace Impostor.Api.Config;

public class TimeoutConfig : IConfigSet
{
    public const string Section = "Timeout";

    public int SpawnTimeout { get; set; } = 2500;

    public int ConnectionTimeout { get; set; } = 2500;
    public string SectionName => Section;
    public void Set(string key, IArgUtils value)
    {
        switch (key)
        {
            case "SpawnTimeout":
                SpawnTimeout = value.GetArg(0, SpawnTimeout);
                break;
            case "ConnectionTimeout":
                ConnectionTimeout = value.GetArg(0, ConnectionTimeout);
                break;
        }
    }
}
