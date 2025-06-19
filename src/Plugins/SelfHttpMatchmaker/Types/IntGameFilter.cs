using System.Text.Json.Serialization;
using Impostor.Api.Innersloth.GameFilters;

namespace SelfHttpMatchmaker.Types;

[Serializable]
public class IntGameFilter : ISubFilter
{
    [JsonPropertyName("AcceptedValues")] public required List<int> AcceptedValues { get; set; }

    [JsonPropertyName("OptionEnum")] public required Int32OptionNames OptionEnum { get; set; }

    [JsonPropertyName("FilterType")] public string FilterType { get; } = "int";
}
