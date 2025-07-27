namespace NextMatchPlugin;

public class NextMatchConfig
{
    public const string Section = "NextMatch";
    
    public string? WebsocketUrl { get; set; }
    public string? Token { get; set; }
}
