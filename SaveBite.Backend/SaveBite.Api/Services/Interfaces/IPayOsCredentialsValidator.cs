using SaveBite.Backend.Models.Responses;
namespace SaveBite.Backend.Services.Interfaces;
public interface IPayOsCredentialsValidator
{
    Task ValidateAsync(PlatformPayOsCredentials credentials, CancellationToken ct = default);
    Task ValidateShopAsync(PlatformPayOsCredentials credentials, CancellationToken ct = default);
    Task ValidatePlatformAsync(PlatformPayOsCredentials credentials, CancellationToken ct = default);
}
