using System.Text.Json.Serialization;

namespace SelfHttpMatchmaker.Types;

public interface IHostServer
{
    long Ip { get; }

    ushort Port { get; }
}

public class HostServer : IHostServer
{
    [JsonPropertyName("Ip")] public required long Ip { get; init; }

    [JsonPropertyName("Port")] public required ushort Port { get; init; }
}
