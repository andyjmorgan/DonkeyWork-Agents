using System.Security.Cryptography;
using System.Text;

namespace DonkeyWork.Agents.Credentials.Core.Utilities;

/// <summary>
/// Utility class for PKCE (Proof Key for Code Exchange) operations.
/// </summary>
public static class PkceUtility
{
    /// <summary>
    /// Generates a cryptographically random code verifier.
    /// </summary>
    /// <returns>A base64url-encoded code verifier.</returns>
    public static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Generates a code challenge from a code verifier using SHA256.
    /// </summary>
    /// <param name="codeVerifier">The code verifier.</param>
    /// <returns>A base64url-encoded code challenge.</returns>
    public static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Encodes bytes to base64url format (RFC 4648).
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A base64url-encoded string.</returns>
    public static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
