using System.Text.Json.Serialization;

namespace SelfHttpMatchmaker.Types;

[Serializable]
public class ChatModeGameFilter : ISubFilter
{
    [JsonPropertyName("FilterType")]
    public string FilterType { get; } = "chat";

    [JsonPropertyName("AcceptedValues")]
    public required byte AcceptedValues { get; set; }
}
