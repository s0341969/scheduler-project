using System.Security.Cryptography;

namespace Rag.Api.Services;

public static class FileHasher
{
    public static async Task<string> Sha256Async(string filePath, CancellationToken ct)
    {
        await using var stream = File.OpenRead(filePath);
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, ct);
        return Convert.ToHexString(hash);
    }
}
