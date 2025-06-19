using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Impostor.Server.Utils;

public static class DotnetUtils
{
    [field: AllowNull, MaybeNull]
    public static string Version
    {
        get
        {
            if (field != null)
            {
                return field;
            }

            var attribute = typeof(DotnetUtils).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            field = attribute != null ? attribute.InformationalVersion : "UNKNOWN";

            return field;
        }
    }

#pragma warning disable CS0162
    public static bool IsDev
    {
        get
        {
#if DEBUG
            return true;
#endif
            return false;
        }
    }
#pragma warning restore CS0162

    public static string Environment
    {
        get => IsDev ? Environments.Development : Environments.Production;
    }

    public static IActionResult OkJson<T>(this T content)
    {
        return new OkObjectResult(JsonSerializer.Serialize(content));
    }
    
    public class NextCounter<T>(string name, T start) where T : class
    {
        public T StartIndex { get; } = start;
        public T? CurrentIndex { get; private set; }
        public string Name { get; } = name;
        public bool HasNext { get; private set; }

        public Func<NextCounter<T>, T>? NextAction { get; set; }
        public Action<NextCounter<T>> OnNext { get; set; } = counter => { };
        
        public ILogger? Logger { get; set; }
        
        public Task Start()
        {
            CurrentIndex = StartIndex;
            while (HasNext)
            {
                HasNext = false;
                OnNext(this);
                if (NextAction != null)
                {
                    CurrentIndex = NextAction(this);
                }
                else
                {
                    CurrentIndex = null;
                    Logger?.LogWarning("NextCounter:{0} CurrentIndex Is Null", Name);
                }
                Logger?.LogDebug("NextCounter:{0} {1} {2}", Name, CurrentIndex, HasNext);
            }
            
            
            return Task.CompletedTask;
        }

        public NextCounter<T> Next()
        {
            HasNext = true;
            return this;
        }
    }
}
