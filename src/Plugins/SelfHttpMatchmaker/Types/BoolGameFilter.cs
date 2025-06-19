using System.Text.Json.Serialization;
using Impostor.Api.Innersloth.GameFilters;

namespace SelfHttpMatchmaker.Types;

[Serializable]
public class BoolGameFilter : ISubFilter
{
    [JsonPropertyName("FilterType")]
    public string FilterType { get; } = "bool";

    [JsonPropertyName("AcceptedValues")]
    public required List<bool> AcceptedValues { get; set; }

    [JsonPropertyName("OptionEnum")]
    public required BoolOptionNames OptionEnum { get; set; }
}
