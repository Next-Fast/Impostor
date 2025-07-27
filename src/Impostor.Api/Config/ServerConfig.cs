using System.Text.Json.Serialization;
using Serilog.Events;

namespace Impostor.Api.Config;

public class ServerConfig 
{
    public const string Section = "Server";

    public ListenerConfig[] Listeners { get; set; } = [];

    public bool EnableCommands { get; set; }
    public string CommandPrefix { get; set; } = "/";
    
    public bool EnableContent { get; set; } = false;

    public bool WriteConsole { get; set; } = true;
    public bool WriteFile { get; set; } = true;
    public string LogFileDir { get; set; } = "{Root}/Logs";
    public string LogFileName { get; set; } = "LogOut_{TimeStamp}.log";
    
    public string ContentPath { get; set; } = "{Root}/Content";
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogEventLevel LogLevel { get; set; } = LogEventLevel.Information;
    
    public bool SaveAuthInfo { get; set; } = false;

    public string? RootPath { get; set; }
    public string? Env { get; set; }
}
