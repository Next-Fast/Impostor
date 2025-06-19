using System.Text.Json.Serialization;

namespace SelfHttpMatchmaker.Types;

[Serializable]
public class MapGameFilter : ISubFilter
{
    [JsonPropertyName("FilterType")]
    public string FilterType { get; } = "map";

    [JsonPropertyName("AcceptedValues")]
    public required byte AcceptedValues { get; set; }
}