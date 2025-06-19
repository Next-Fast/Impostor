using System.Text.Json.Serialization;
using Impostor.Api.Innersloth.GameFilters;

namespace SelfHttpMatchmaker.Types;

[Serializable]
public class CategorizedGameFilter : ISubFilter
{
    [JsonPropertyName("FilterType")]
    public string FilterType { get; } = "cat";

    [JsonPropertyName("AcceptedValues")]
    public required List<int> AcceptedValues { get; set; }

    [JsonPropertyName("OptionEnum")]
    public required CategorizedOptionNames OptionEnum { get; set; }
}
