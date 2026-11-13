using SaveBite.Backend.Models.Responses;
namespace SaveBite.Backend.Services.Interfaces;
public interface IPayOsApiClient
{
    Task<System.Text.Json.JsonElement> SendAsync(HttpMethod method, string path,
        PlatformPayOsCredentials credentials, object? body, CancellationToken ct);
}
