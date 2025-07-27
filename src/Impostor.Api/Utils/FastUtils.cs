using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Impostor.Api.Utils;

public static class FastUtils
{
    public static bool TryGet<T>(this IEnumerable<T> list, Predicate<T> predicate,[MaybeNullWhen(false)]out T result) where T : class
    {
        foreach (var item in list)
        {
            if (!predicate(item))
            {
                continue;
            }

            result = item;
            return true;
        }

        result = null;
        return false;
    }

    public static string GetTimeStamp()
    {
        return DateTime.Now.ToString("yyyy_MM_dd@HH:mm:ss");
    }

    public static string Replace(this string text, params List<(string, string)> values)
    {
        return values.Aggregate(text, (current, value) => current.Replace(value.Item1, value.Item2));
    }
}
