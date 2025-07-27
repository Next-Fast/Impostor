using System.Text.Json.Serialization;

namespace NextMatchPlugin.Types;

public class ConnectionRequest : IConnectionEvent
{

    [JsonPropertyName("action")]
    public ConnectionAction Action { get; set; } 

    [JsonPropertyName("id")]
    public string Id { get; set; } 
    
    [JsonPropertyName("type")]
    public ConnectionType Type { get; set; }
    
    [JsonPropertyName("params")]
    public object? Params { get; set; }
}

public class ConnectionRequest<T>: ConnectionRequest where T : class
{
    public T? Content
    {
        get
        {
            return Params as T;
        }
        set
        {
            Params = value;
        }
    }
    
    public static implicit operator T?(ConnectionRequest<T> request) => request.Content;
}
