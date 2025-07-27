using System.Security.Cryptography;
using System.Text;

namespace NextMatchPlugin.Types;

public static class AuthHelper
{
    public static string GeneratePasswordHash(string password)
    {
        // 将密码和标识符连接起来
        var data = password + ".Next";

        // 计算SHA256哈希值
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(data));

        // 将哈希值转换为16进制字符串
        var builder = new StringBuilder();
        foreach (var t in bytes)
        {
            builder.Append(t.ToString("x2"));
        }
        return builder.ToString();
    }
}
