using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IPlatformPayOsConfigService
{
    Task<PlatformPayOsConfigResponse> GetAdminConfigAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PlatformPayOsConfigHistoryResponse>> GetHistoryAsync(CancellationToken cancellationToken = default);
    Task<PlatformPayOsConfigResponse> UpdateAsync(Guid adminId, UpdatePlatformPayOsConfigRequest request, CancellationToken cancellationToken = default);
    Task<PlatformPayOsCredentials> GetCredentialsAsync(CancellationToken cancellationToken = default);
}
