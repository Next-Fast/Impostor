using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Impostor.Api.Extension.Utils;

public static class HttpUtils
{
    public static bool TryGetSingleOrDefault(this HttpRequest request, string key,
        [MaybeNullWhen(false)] out string value)
    {
        if (!request.Query.TryGetValue(key, out var values))
        {
            value = null;
            return false;
        }

        value = values.FirstOrDefault();
        return value != null;
    }
}
