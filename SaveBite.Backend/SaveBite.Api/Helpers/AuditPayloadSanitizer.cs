using System.Text.Json;
using System.Text.Json.Nodes;

namespace SaveBite.Backend.Helpers;

/// <summary>
/// BR-163: audit entries must never expose secrets. Redaction is recursive (objects and arrays at any depth) and is
/// applied when an entry is read back for the Audit Log (UC-147).
/// </summary>
public static class AuditPayloadSanitizer
{
    public const string Redacted = "[REDACTED]";

    private static readonly string[] SensitiveFragments =
    {
        "password", "passwd", "secret", "token", "apikey", "api_key", "checksum", "hash",
        "privatekey", "private_key", "credential", "authorization", "payos",
        "clientid", "client_id", "accountnumber", "account_number", "bankaccount", "bank_account"
    };

    // Short names are matched exactly to avoid false positives such as "footprint" or "shipping".
    private static readonly HashSet<string> SensitiveExactKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "otp", "cvv", "pin"
    };

    public static bool IsSensitiveKey(string key)
    {
        if (SensitiveExactKeys.Contains(key)) return true;
        var lower = key.ToLowerInvariant();
        return SensitiveFragments.Any(fragment => lower.Contains(fragment));
    }

    /// <summary>Returns the JSON with every sensitive property replaced by <see cref="Redacted"/>.</summary>
    public static string? SanitizeJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;

        try
        {
            var node = JsonNode.Parse(json);
            RedactNode(node);
            return node?.ToJsonString();
        }
        catch (JsonException)
        {
            // An unparseable payload cannot be proven free of secrets, so it is never displayed.
            return JsonSerializer.Serialize(new Dictionary<string, string> { ["value"] = Redacted });
        }
    }

    /// <summary>Sanitizes then converts a stored JSON payload to a plain dictionary for API responses.</summary>
    public static Dictionary<string, object?>? ToSafeDictionary(string? json)
    {
        var sanitized = SanitizeJson(json);
        if (string.IsNullOrWhiteSpace(sanitized)) return null;

        try
        {
            using var doc = JsonDocument.Parse(sanitized);
            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                return doc.RootElement.EnumerateObject()
                    .ToDictionary(p => p.Name, p => ConvertElement(p.Value));
            }

            return new Dictionary<string, object?> { ["value"] = ConvertElement(doc.RootElement) };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void RedactNode(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(pair => pair.Key).ToList())
                {
                    if (IsSensitiveKey(key))
                        obj[key] = Redacted;
                    else
                        RedactNode(obj[key]);
                }
                break;

            case JsonArray array:
                foreach (var item in array)
                    RedactNode(item);
                break;
        }
    }

    private static object? ConvertElement(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ConvertElement(p.Value)),
        JsonValueKind.Array => element.EnumerateArray().Select(ConvertElement).ToList(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var l) ? l
            : element.TryGetDecimal(out var d) ? d
            : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => null
    };
}
