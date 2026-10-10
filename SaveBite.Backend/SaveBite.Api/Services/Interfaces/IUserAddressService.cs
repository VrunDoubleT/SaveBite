using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Services.Interfaces;

public interface IUserAddressService
{
    Task<IReadOnlyList<UserAddressResponse>> GetMyAddressesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserAddressResponse> CreateAddressAsync(Guid userId, UserAddressRequests.CreateAddressRequest request, CancellationToken cancellationToken = default);
    Task<UserAddressResponse> SetDefaultAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);
    Task<UserAddressResponse> UpdateAddressAsync(Guid userId, Guid addressId, UserAddressRequests.UpdateAddressRequest request, CancellationToken cancellationToken = default);
    Task DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default);
}