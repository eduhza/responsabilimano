using System.Security.Cryptography;

namespace ResponsabiliMano.Infrastructure.Identity;

/// <summary>
/// Generates cryptographically secure, URL-safe random tokens.
/// </summary>
public static class SecureTokenGenerator
{
    /// <summary>
    /// Creates a URL-safe base64 token derived from <paramref name="byteLength"/> random bytes.
    /// </summary>
    public static string Generate(int byteLength = 32)
    {
        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
