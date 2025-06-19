using System.Text.Json.Serialization;

namespace SelfHttpMatchmaker.Types;

[Serializable]
public class LanguageFilter : ISubFilter
{
    [JsonPropertyName("AcceptedValues")] public required uint AcceptedValues { get; set; }

    [JsonPropertyName("FilterType")] public string FilterType { get; } = "languages";
}
