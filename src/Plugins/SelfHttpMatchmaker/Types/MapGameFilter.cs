using System.Text.Json.Serialization;

namespace SelfHttpMatchmaker.Types;

[Serializable]
public class MapGameFilter : ISubFilter
{
    [JsonPropertyName("AcceptedValues")] public required byte AcceptedValues { get; set; }

    [JsonPropertyName("FilterType")] public string FilterType { get; } = "map";
}
