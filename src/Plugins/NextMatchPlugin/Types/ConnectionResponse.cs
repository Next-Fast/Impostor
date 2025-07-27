using System.Text.Json.Serialization;

namespace NextMatchPlugin.Types;

public class ConnectionResponse : IConnectionEvent
{


    [JsonPropertyName("action")] 
    public ConnectionAction Action { get; set; } 

    [JsonPropertyName("type")]
    public ConnectionType Type { get; set; }
    
    [JsonPropertyName("id")]
    public string Id { get; set; }
    
    [JsonPropertyName("status")]
    public ConnectionStatus Status { get; set; }
    
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

public class ConnectionResponse<T> : ConnectionResponse where T : class
{
    public T? Content
    {
        get
        {
            return Data as T;
        }
        set
        {
            Data = value;
        }
    }
}
