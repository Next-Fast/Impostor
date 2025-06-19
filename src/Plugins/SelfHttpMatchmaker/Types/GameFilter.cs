using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SelfHttpMatchmaker.Types;

[Serializable]
public class GameFilter
{
    [JsonConstructor]
    public GameFilter(string optionType, string key, string subFilterString)
    {
        OptionType = optionType;
        Key = key;
        SubFilterString = subFilterString;
        SubFilter = ResolveSubFilter(OptionType, SubFilterString);
    }

    [JsonIgnore] public ISubFilter SubFilter { get; set; }

    [JsonPropertyName("OptionType")] public required string OptionType { get; set; }

    [JsonPropertyName("Key")] public required string Key { get; set; }

    [JsonPropertyName("SubFilterString")] public required string SubFilterString { get; set; }

    public void ModifySubFilter(ISubFilter subFilter)
    {
        SubFilter = subFilter;
    }

    [OnSerializing]
    internal void OnSerializing(StreamingContext context)
    {
        SubFilterString = JsonSerializer.Serialize(SubFilter);
    }

    [OnDeserialized]
    internal void OnDeserialized(StreamingContext context)
    {
        SubFilter = ResolveSubFilter(OptionType, SubFilterString);
    }

    private ISubFilter ResolveSubFilter(string? type, string filterString)
    {
        if (type != null)
        {
            switch (type.ToLower())
            {
                case "int":
                    return JsonSerializer.Deserialize<IntGameFilter>(filterString)
                           ?? throw new InvalidOperationException("Deserialization returned null for IntGameFilter");
                case "platform":
                    return JsonSerializer.Deserialize<PlatformGameFilter>(filterString)
                           ?? throw new InvalidOperationException(
                               "Deserialization returned null for PlatformGameFilter");
                case "cat":
                    return JsonSerializer.Deserialize<CategorizedGameFilter>(filterString)
                           ?? throw new InvalidOperationException(
                               "Deserialization returned null for CategorizedGameFilter");
                case "languages":
                    return JsonSerializer.Deserialize<LanguageFilter>(filterString)
                           ?? throw new InvalidOperationException("Deserialization returned null for LanguageFilter");
                case "chat":
                    return JsonSerializer.Deserialize<ChatModeGameFilter>(filterString)
                           ?? throw new InvalidOperationException(
                               "Deserialization returned null for ChatModeGameFilter");
                case "map":
                    return JsonSerializer.Deserialize<MapGameFilter>(filterString)
                           ?? throw new InvalidOperationException("Deserialization returned null for MapGameFilter");
                case "bool":
                    return JsonSerializer.Deserialize<BoolGameFilter>(filterString)
                           ?? throw new InvalidOperationException("Deserialization returned null for BoolGameFilter");
                default:
                    throw new InvalidOperationException("No type matches subfilter");
            }
        }

        throw new InvalidOperationException("Must provide type for sub filter");
    }
}
