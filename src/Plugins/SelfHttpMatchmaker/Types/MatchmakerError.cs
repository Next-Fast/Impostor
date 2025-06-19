using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Impostor.Api.Innersloth;

namespace SelfHttpMatchmaker.Types;

[method: SetsRequiredMembers]
public class MatchmakerError(DisconnectReason reason, string message = "")
{
    [JsonPropertyName("Reason")] public required DisconnectReason Reason { get; init; } = reason;

    [JsonPropertyName("Message")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public required string Message { get; init; } = message;
}
