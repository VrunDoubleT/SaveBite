using System.Text.Json;
using System.Text.Json.Serialization;
using SaveBite.Backend.Helpers;

namespace SaveBite.Backend.Audit;

internal static class AuditJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        WriteIndented = false
    };

    public static string Serialize(object value) => JsonSerializer.Serialize(value, Options);

    public static string? SerializeSafe(object? value) => value == null
        ? null
        : AuditPayloadSanitizer.SanitizeJson(Serialize(value));
}
