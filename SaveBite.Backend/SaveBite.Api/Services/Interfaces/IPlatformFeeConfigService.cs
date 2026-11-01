using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IPlatformFeeConfigService
{
    Task<PlatformFeeConfigResponse> GetActiveConfigAsync(CancellationToken ct = default);
    Task<IReadOnlyList<PlatformFeeConfigResponse>> GetAllConfigsAsync(CancellationToken ct = default);
    Task<PlatformFeeConfigResponse> UpdateConfigAsync(Guid adminId, UpdatePlatformFeeConfigRequest request, CancellationToken ct = default);
    Task CancelScheduledConfigAsync(Guid adminId, Guid configId, CancellationToken ct = default);
}

