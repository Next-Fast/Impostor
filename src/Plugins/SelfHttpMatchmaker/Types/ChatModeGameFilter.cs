using System.Text.Json.Serialization;

namespace SelfHttpMatchmaker.Types;

[Serializable]
public class ChatModeGameFilter : ISubFilter
{
    [JsonPropertyName("AcceptedValues")] public required byte AcceptedValues { get; set; }

    [JsonPropertyName("FilterType")] public string FilterType { get; } = "chat";
}
