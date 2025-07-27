using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Impostor.Api.Extension.Utils;

public static class JsonUtils
{
    private static readonly IpAddressConverter IpAddressConverter = new();
    private static readonly TimeConverter TimeConverter = new();
    private static JsonSerializerOptions? _options;
    
    public static JsonSerializerOptions GetConverterOptions()
    {
        if (_options != null)
        {
            return _options;
        }
        
        _options = new JsonSerializerOptions
        {
            AllowTrailingCommas = true,
            WriteIndented = true,
            Converters = { IpAddressConverter,TimeConverter },
        };
        return _options;
    }

    public static string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize(obj, GetConverterOptions());
    }

    public static T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, GetConverterOptions());
    }
}

internal class IpAddressConverter : JsonConverter<IPAddress>
{
    public override IPAddress Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return IPAddress.Parse(reader.GetString() ?? string.Empty);
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, IPAddress value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

internal class TimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return DateTime.ParseExact(reader.GetString() ?? string.Empty, "yyyy:MM:dd:HH:mm", null);
        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy:MM:dd:HH:mm"));
    }
}
