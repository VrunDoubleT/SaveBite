using System.Security.Cryptography;
using System.Text;

namespace SaveBite.Backend.Helpers;

// Provides general-purpose one-way hashing helpers for non-secret identifiers.
public static class HashingHelper
{
    public static string ComputeSha256Hex(string value)
        => Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(value)))
            .ToLowerInvariant();
}
