using System.Security.Cryptography;
using System.Text;

namespace SelfHttpMatchmaker;

public static class TokenUtils
{
    public const string Token = "TianMengToken";
    private static SHA256 _hash = SHA256.Create();

    public static string GetTokenHashString()
    {
        var hash = _hash.ComputeHash(Encoding.UTF8.GetBytes(Token));
        return Convert.ToBase64String(hash);
    }

    public static bool CompareTokenHash(this string tokenString)
    {
        var hash = _hash.ComputeHash(Encoding.UTF8.GetBytes(tokenString));
        return Convert.ToBase64String(hash) == GetTokenHashString();
    }
}
