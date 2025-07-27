using System.Text.Json.Serialization;

namespace NextMatchPlugin.Types;

// ReSharper disable InconsistentNaming
public enum ConnectionStatus : int
{
    Success = 0,
    Failed = 1,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ConnectionType
{
    heartbeat,
}

public enum ConnectionAction
{
    Request,
    Response,
    WaitRequest,
    WaitResponse,
}

public interface IConnectionEvent
{
    
    [JsonPropertyName("action")]
    public ConnectionAction Action { get; set; }
    
    [JsonPropertyName("type")]
    public ConnectionType Type { get; set; }

    [JsonPropertyName("id")] 
    public string Id { get; set; }
}

public static class ConnectionEventExtensions
{
    
    public static ConnectionRequest ToRequest(this IConnectionEvent @event) => (ConnectionRequest)@event;
    
    public static ConnectionResponse ToResponse(this IConnectionEvent @event) => (ConnectionResponse)@event;

    public static ConnectionResponse ToResponse(this ConnectionRequest request, ConnectionStatus status,
        object? content)
    {
        var action = request.Action switch
        {
            ConnectionAction.Request => ConnectionAction.Response,
            ConnectionAction.WaitRequest => ConnectionAction.WaitResponse,
            _ => throw new ArgumentOutOfRangeException(nameof(request), request.Action, null),
        };

        return new ConnectionResponse
        {
            Action = action,
            Type = request.Type,
            Id = request.Id,
            Status = status,
            Data = content,
        };
    }
}
