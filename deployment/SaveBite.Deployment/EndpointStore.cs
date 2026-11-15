using System.Text.Json;

namespace SaveBite.Deployment;

public sealed class EndpointStore(string path, TimeProvider clock)
{
    public async Task WriteAsync(string url, bool ready, string session, CancellationToken ct = default)
    {
        var state = new EndpointSnapshot(url, ready, session, clock.GetUtcNow().ToUnixTimeSeconds());
        await AtomicFile.WriteAsync(path, JsonSerializer.Serialize(state) + "\n", privateFile: false, ct);
    }

    public async Task<EndpointSnapshot?> ReadAsync(CancellationToken ct = default)
    {
        try
        {
            return JsonSerializer.Deserialize<EndpointSnapshot>(await File.ReadAllTextAsync(path, ct));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            return null;
        }
    }
}
