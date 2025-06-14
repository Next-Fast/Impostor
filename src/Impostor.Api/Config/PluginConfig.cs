namespace Impostor.Api.Config;

public class PluginConfig
{
    public const string Section = "Plugin";

    public string[] PluginPaths { get; set; } = ["plugins"];

    public string[] LibraryPaths { get; set; } = ["libraries"];

    public string HttpIp { get; set; } = "127.0.0.1";
    public int HttpPort { get; set; } = 25000;
}
