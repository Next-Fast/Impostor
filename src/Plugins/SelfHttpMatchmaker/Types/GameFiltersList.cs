using System.Text.Json.Serialization;

namespace SelfHttpMatchmaker.Types;

[Serializable]
public class GameFiltersList
{
    [JsonPropertyName("FilterSets")] public required List<GameFilterSet> FilterSets { get; set; }
}
