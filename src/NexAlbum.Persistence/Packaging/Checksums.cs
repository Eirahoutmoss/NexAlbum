using System.Security.Cryptography;

namespace NexAlbum.Persistence.Packaging;

internal static class Checksums
{
    /// <summary>Akışın SHA-256 özeti (küçük harf onaltılık).</summary>
    public static string Sha256(Stream stream)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexStringLower(sha.ComputeHash(stream));
    }

    public static string Sha256(byte[] data) => Convert.ToHexStringLower(SHA256.HashData(data));
}
