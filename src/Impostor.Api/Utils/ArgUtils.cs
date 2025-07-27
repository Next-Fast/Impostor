using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Impostor.Api.Config;

namespace Impostor.Api.Utils;

public interface IArgUtils
{
    public string[] Args { get; }
}

public class DefaultArgUtils(string[] arg, int startIndex = 0, int endIndex = -1) : IArgUtils
{
    public string[] Args { get; } = arg.Skip(startIndex).Take(endIndex == -1 ? arg.Length - startIndex : endIndex).ToArray();
}

public static class ArgUtils
{
    public static DefaultArgUtils To(this IArgUtils utils, int startIndex = 0, int endIndex = -1)
    {
        return new DefaultArgUtils(utils.Args, startIndex, endIndex);
    }
    
    public static string GetArg(this IArgUtils arg,int index, string? defaultValue)
    {
        var args = arg.Args;
        if (args.Length > index)
        {
            return args[index];
        }
        return defaultValue ?? string.Empty;
    }

    public static T? GetArg<T>(this IArgUtils arg,int index, T? defaultValue, Func<string, T?> converter)
    {
        var value = arg.GetArg(index, null);
        if (string.IsNullOrEmpty(value))
        {
            return defaultValue;
        }
        
        return converter(value) ?? defaultValue;
    }

    public static T? GetArg<T>(this IArgUtils arg,int index, T? defaultValue)
    {
        return arg.GetArg(index, defaultValue, DefaultConverter<T>);
    }
    
    public static T GetEnumArg<T>(this IArgUtils arg,int index, T defaultValue) where T : struct
    {
        var text = arg.GetArg(index, null);
        if (string.IsNullOrEmpty(text))
        {
            return defaultValue;
        }
        
        return Enum.TryParse<T>(text, true, out var value) ? value : defaultValue;
    }
    
    public static Dictionary<Type, Func<string, object>> Converters { get; } = new()
    {
        {typeof(int), value => int.Parse(value)},
        {typeof(bool), value => bool.Parse(value)},
        {typeof(string), value => value},
        {typeof(float), value => float.Parse(value)},
        {typeof(double), value => double.Parse(value)},
        {typeof(long), value => long.Parse(value)},
        {typeof(CheatingHostMode), value => Enum.Parse<CheatingHostMode>(value)},
    };
    
    public static T? DefaultConverter<T>(this string value) 
    {
        if (Converters.TryGetValue(typeof(T), out var converter))
        {
            return (T)converter(value);
        }

        if (value.StartsWith('{') && value.EndsWith('}'))
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        
        if (value.StartsWith('[') && value.EndsWith(']') && typeof(T).IsArray)
        {
            return JsonSerializer.Deserialize<T>(value);
        }
        
        return default;
    }
}
