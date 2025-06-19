using System.Text.Json.Serialization;
using Impostor.Api.Innersloth;

namespace SelfHttpMatchmaker.Types;

[Serializable]
[method: JsonConstructor]
public class GameFilterSet(GameModes gameMode, List<GameFilter> filters)
{
    [JsonPropertyName("GameMode")]
    public required GameModes GameMode { get; set; } = gameMode;

    [JsonPropertyName("Filters")]
    public required List<GameFilter> Filters { get; set; } = filters;
}
