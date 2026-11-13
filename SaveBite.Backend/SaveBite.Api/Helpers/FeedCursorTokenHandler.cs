using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SaveBite.Backend.Helpers;

public sealed class FeedCursorData
{
    public DateTime? LastFeedPublishedAt { get; set; }
    public Guid? LastDealId { get; set; }
    public DateTime SnapshotAt { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LocationMode { get; set; }
    public string? FilterHash { get; set; }
}

public static class FeedCursorTokenHandler
{
    // Static secret key for HMAC signature of cursor tokens (internal consistency)
    private static readonly byte[] HmacKey = Encoding.UTF8.GetBytes("SaveBite_Feed_Cursor_Secret_Token_Key_2026!#");

    public static string Encode(FeedCursorData data)
    {
        var json = JsonSerializer.Serialize(data);
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        using var hmac = new HMACSHA256(HmacKey);
        var signature = hmac.ComputeHash(jsonBytes);

        var payloadWithSig = new byte[jsonBytes.Length + signature.Length];
        Buffer.BlockCopy(jsonBytes, 0, payloadWithSig, 0, jsonBytes.Length);
        Buffer.BlockCopy(signature, 0, payloadWithSig, jsonBytes.Length, signature.Length);

        return Convert.ToBase64String(payloadWithSig)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public static FeedCursorData? Decode(string token)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;

        try
        {
            var incoming = token.Replace('-', '+').Replace('_', '/');
            switch (incoming.Length % 4)
            {
                case 2: incoming += "=="; break;
                case 3: incoming += "="; break;
            }

            var bytes = Convert.FromBase64String(incoming);
            const int sigLen = 32; // SHA256 length
            if (bytes.Length <= sigLen) return null;

            var jsonLen = bytes.Length - sigLen;
            var jsonBytes = new byte[jsonLen];
            var expectedSig = new byte[sigLen];

            Buffer.BlockCopy(bytes, 0, jsonBytes, 0, jsonLen);
            Buffer.BlockCopy(bytes, jsonLen, expectedSig, 0, sigLen);

            using var hmac = new HMACSHA256(HmacKey);
            var computedSig = hmac.ComputeHash(jsonBytes);

            if (!CryptographicOperations.FixedTimeEquals(expectedSig, computedSig))
            {
                return null;
            }

            var json = Encoding.UTF8.GetString(jsonBytes);
            return JsonSerializer.Deserialize<FeedCursorData>(json);
        }
        catch
        {
            return null;
        }
    }
}
