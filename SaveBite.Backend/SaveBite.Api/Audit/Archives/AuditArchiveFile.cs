using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Audit.Archives;

/// <summary>Streaming ZIP/JSONL serialization, including read support for legacy GZIP files.</summary>
public static class AuditArchiveFile
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<long> WriteAsync(string path, int year, int month,
        IAsyncEnumerable<AuditLog> rows, CancellationToken ct)
    {
        await using var file = File.Create(path);
        using var zip = new ZipArchive(file, ZipArchiveMode.Create);
        await using var entry = zip.CreateEntry($"audit-logs-{year:D4}-{month:D2}/audit-logs.jsonl", CompressionLevel.Optimal).Open();
        await using var writer = new StreamWriter(entry);
        long count = 0;
        await foreach (var row in rows.WithCancellation(ct))
        {
            row.OldValuesJson = AuditPayloadSanitizer.SanitizeJson(row.OldValuesJson);
            row.NewValuesJson = AuditPayloadSanitizer.SanitizeJson(row.NewValuesJson);
            await writer.WriteLineAsync(JsonSerializer.Serialize(row, Json).AsMemory(), ct);
            count++;
        }
        return count;
    }

    public static async Task<string> CreateDownloadZipAsync(string archivePath, string entryName, CancellationToken ct)
    {
        var zipPath = Path.GetTempFileName();
        try
        {
            if (IsZip(archivePath))
            {
                File.Copy(archivePath, zipPath, overwrite: true);
                return zipPath;
            }
            await using var output = File.Create(zipPath);
            using var zip = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true);
            var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal);
            await using var entryStream = entry.Open();
            await using var source = File.OpenRead(archivePath);
            await using var gzip = new GZipStream(source, CompressionMode.Decompress);
            await gzip.CopyToAsync(entryStream, ct);
            return zipPath;
        }
        catch { File.Delete(zipPath); throw; }
    }

    public static async IAsyncEnumerable<AuditLog> RowsAsync(string path, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        await using var file = File.OpenRead(path);
        using var zip = IsZip(path) ? new ZipArchive(file, ZipArchiveMode.Read, leaveOpen: true) : null;
        await using var content = zip != null
            ? zip.Entries.Single(x => x.FullName.EndsWith(".jsonl", StringComparison.OrdinalIgnoreCase)).Open()
            : new GZipStream(file, CompressionMode.Decompress);
        using var reader = new StreamReader(content);
        while (await reader.ReadLineAsync(ct) is { } line)
            yield return JsonSerializer.Deserialize<AuditLog>(line, Json) ?? throw new InvalidDataException("Invalid archive row.");
    }
    private static bool IsZip(string path)
    {
        using var file = File.OpenRead(path);
        return file.ReadByte() == 'P' && file.ReadByte() == 'K';
    }
    public static async Task<string> HashAsync(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
    }
}
