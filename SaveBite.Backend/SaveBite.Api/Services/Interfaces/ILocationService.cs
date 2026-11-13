using SaveBite.Backend.Models.DTOs;

namespace SaveBite.Backend.Services.Interfaces;

public interface ILocationService
{
    Task<long> UpdateGpsAsync(Guid userId, double latitude, double longitude, double? accuracy, CancellationToken ct = default);
    Task<UserGpsMetadata?> GetGpsMetadataAsync(Guid userId, CancellationToken ct = default);
    Task RemoveGpsAsync(Guid userId, CancellationToken ct = default);
    Task SyncDefaultAddressAsync(Guid userId, double latitude, double longitude, long addressVersion, CancellationToken ct = default);
    Task RemoveDefaultAddressAsync(Guid userId, CancellationToken ct = default);
    Task<long?> GetSyncedDefaultAddressVersionAsync(Guid userId, CancellationToken ct = default);
}
