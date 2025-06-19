using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SelfHttpMatchmaker.Types;

namespace SelfHttpMatchmaker;

public static class TokenUtils
{
    public static string? Token { get; internal set; }
    private static readonly SHA256 Hash = SHA256.Create();
    internal static bool TokenAuthEnabled = false;

    public static TimeSpan? TokenExpiresTime { get; internal set; } = TimeSpan.FromMinutes(30);
    
    private static bool TokenAuth => TokenAuthEnabled && Token != null && TokenExpiresTime != null;

    public static string GetTokenHashString()
    {
        var hash = Hash.ComputeHash(Encoding.UTF8.GetBytes(Token ?? "Impostor"));
        return Convert.ToBase64String(hash);
    }

    public static string GenerateTokenResponse(this TokenRequest request)
    {
        var token = new Token
        {
            Content = new TokenPayload
            {
                ExpiresAt = GetNextTime() ?? TokenPayload.DefaultExpiryDate,
                ProductUserId = request.ProductUserId,
                ClientVersion = request.ClientVersion,
            },
            Hash = GetTokenHashString(),
        };

        // Wrap into a Base64 sandwich
        var serialized = JsonSerializer.SerializeToUtf8Bytes(token);
        return Convert.ToBase64String(serialized);
    }

    public static bool CompareTokenHash(this string tokenString)
    {
        var hash = Hash.ComputeHash(Encoding.UTF8.GetBytes(tokenString));
        return Convert.ToBase64String(hash) == GetTokenHashString();
    }

    public static bool VerifyToken(this string tokenString, [MaybeNullWhen(false)] out Token token)
    {
        var bytes = Convert.FromBase64String(tokenString);
        var jsonToken = JsonSerializer.Deserialize<Token>(bytes);
        if (!TokenAuth)
        {
            token = jsonToken!;
            return true;
        }

        if (jsonToken == null)
        {
            token = null;
            return false;
        }

        token = jsonToken;
        return token.Hash == GetTokenHashString() && token.Content.ExpiresAt > DateTime.Now;
    }

    public static bool TryVerifyTokenFormHeader(this AuthenticationHeaderValue header,
        [MaybeNullWhen(true)] out string result, [MaybeNullWhen(false)] out Token token)
    {
        result = null;
        token = null;


        if (header.Scheme != "Bearer" || header.Parameter == null)
        {
            result = "No Has AuthenticationHeader";
            return false;
        }

        try
        {
            var tokenString = header.Parameter;
            if (!tokenString.VerifyToken(out token))
            {
                result = "Token Invalid";
                return false;
            }
        }
        catch
        {
            result = "Token Invalid";
            return false;
        }

        return true;
    }

    public static DateTime? GetNextTime()
    {
        if (TokenExpiresTime != null)
        {
            return DateTime.Now.Add(TokenExpiresTime.Value);
        }
        return null;
    }
}
