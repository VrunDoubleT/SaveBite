using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class UserAddressService : IUserAddressService
{
    private readonly IUserAddressRepository _userAddressRepository;

    public UserAddressService(IUserAddressRepository userAddressRepository)
    {
        _userAddressRepository = userAddressRepository;
    }

    // Retrieve all addresses for the current user.
    public async Task<IReadOnlyList<UserAddressResponse>> GetMyAddressesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var addresses = await _userAddressRepository.GetByUserIdAsync(userId, cancellationToken);
        return addresses.Select(ToResponse).ToList();
    }

    // Add a new address.
    public async Task<UserAddressResponse> CreateAddressAsync(Guid userId, UserAddressRequests.CreateAddressRequest request, CancellationToken cancellationToken = default)
    {
        var label = string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim();
        var addressLine = request.AddressLine?.Trim();
        var ward = string.IsNullOrWhiteSpace(request.Ward) ? null : request.Ward.Trim();
        var district = string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim();
        var city = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();

        var now = DateTime.UtcNow;
        var address = new UserAddress
        {
            UserId = userId,
            Label = label,
            AddressLine = addressLine,
            Ward = ward,
            District = district,
            City = city,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            CreatedAt = now,
            UpdatedAt = now
        };

        var created = await _userAddressRepository.CreateAsync(
            address,
            request.IsDefault,
            cancellationToken);

        return ToResponse(created);
    }

    // Set the address as the default.
    public async Task<UserAddressResponse> SetDefaultAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default)
    {
        var address = await _userAddressRepository.GetByIdAsync(userId, addressId, cancellationToken);
        if (address is null)
            throw AppException.NotFound("Address not found.");

        if (address.IsDefault)
            throw AppException.Conflict("The default address is already the default address.");

        await _userAddressRepository.SetDefaultAsync(userId, addressId, cancellationToken);
        address.IsDefault = true;
        address.UpdatedAt = DateTime.UtcNow;

        return ToResponse(address);
    }

    // Update an address.
    public async Task<UserAddressResponse> UpdateAddressAsync(Guid userId, Guid addressId, UserAddressRequests.UpdateAddressRequest request, CancellationToken cancellationToken = default)
    {
        var address = await _userAddressRepository.GetByIdAsync(userId, addressId, cancellationToken);

        if (address is null)
            throw AppException.NotFound("Address not found.");

        var label = string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim();
        var addressLine = request.AddressLine?.Trim();
        var ward = string.IsNullOrWhiteSpace(request.Ward) ? null : request.Ward.Trim();
        var district = string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim();
        var city = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();

        address.Label = label;
        address.AddressLine = addressLine;
        address.Ward = ward;
        address.District = district;
        address.City = city;
        address.Latitude = request.Latitude;
        address.Longitude = request.Longitude;
        address.UpdatedAt = DateTime.UtcNow;

        await _userAddressRepository.UpdateAsync(address, request.IsDefault, cancellationToken);

        return ToResponse(address);
    }

    // Delete an address.
    public async Task DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default)
    {
        var address = await _userAddressRepository.GetByIdAsync(userId, addressId, cancellationToken);
        if (address is null)
            throw AppException.NotFound("Address not found.");

        if (address.IsDefault)
            throw AppException.Conflict("The default address cannot be deleted.");

        await _userAddressRepository.DeleteAsync(address, cancellationToken);
    }

    // Map the entity to its response model.
    private static UserAddressResponse ToResponse(UserAddress address)
        => new(address.Id,
            address.Label,
            address.AddressLine,
            address.Ward,
            address.District,
            address.City,
            address.Latitude,
            address.Longitude,
            address.IsDefault,
            address.CreatedAt,
            address.UpdatedAt);
}
