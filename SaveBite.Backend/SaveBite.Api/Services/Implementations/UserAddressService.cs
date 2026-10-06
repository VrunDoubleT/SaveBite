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

    public UserAddressService(
        IUserAddressRepository userAddressRepository)
    {
        _userAddressRepository = userAddressRepository;
    }

    // GET ALL ADDRESSES OF CURRENT USER
    public async Task<IReadOnlyList<UserAddressResponse>> GetMyAddressesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var addresses = await _userAddressRepository.GetByUserIdAsync(userId, cancellationToken);
        return addresses.Select(ToResponse).ToList();
    }

    // ADD NEW ADDRESS
    public async Task<UserAddressResponse> CreateAddressAsync(Guid userId, UserAddressRequests.CreateAddressRequest request, CancellationToken cancellationToken = default)
    {
        var label = string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim();
        
        var addressLine = request.AddressLine?.Trim();
        if (string.IsNullOrWhiteSpace(addressLine))
            throw AppException.BadRequest("Address line is required.");

        var ward = string.IsNullOrWhiteSpace(request.Ward) ? null : request.Ward.Trim();
        var district = string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim();
        var city = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();

        if (request.Latitude < -90 || request.Latitude > 90)
            throw AppException.BadRequest("Latitude must be between -90 and 90.");

        if (request.Longitude < -180 || request.Longitude > 180)
            throw AppException.BadRequest("Longitude must be between -180 and 180.");

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

    // SET ADDRESS AS DEFAULT
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
    
    // UPDATE ADDRESS
    public async Task<UserAddressResponse> UpdateAddressAsync(Guid userId, Guid addressId, UserAddressRequests.UpdateAddressRequest request, CancellationToken cancellationToken = default)
    {
        var address = await _userAddressRepository.GetByIdAsync(userId, addressId, cancellationToken);

        if (address is null)
            throw AppException.NotFound("Address not found.");
        
        var label = string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim();
        
        var addressLine = request.AddressLine?.Trim();
        if (string.IsNullOrWhiteSpace(addressLine))
            throw AppException.BadRequest("Address line is required.");

        var ward = string.IsNullOrWhiteSpace(request.Ward) ? null : request.Ward.Trim();
        var district = string.IsNullOrWhiteSpace(request.District) ? null : request.District.Trim();
        var city = string.IsNullOrWhiteSpace(request.City) ? null : request.City.Trim();

        if (request.Latitude < -90 || request.Latitude > 90)
            throw AppException.BadRequest("Latitude must be between -90 and 90.");

        if (request.Longitude < -180 || request.Longitude > 180)
            throw AppException.BadRequest("Longitude must be between -180 and 180.");

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

    // DELETE ADDRESS
    public async Task DeleteAddressAsync(Guid userId, Guid addressId, CancellationToken cancellationToken = default)
    {
        var address = await _userAddressRepository.GetByIdAsync(userId, addressId, cancellationToken);
        if (address is null)
            throw AppException.NotFound("Address not found.");

        if (address.IsDefault)
            throw AppException.Conflict("The default address cannot be deleted.");

        await _userAddressRepository.DeleteAsync(address, cancellationToken);
    }

    // MAP ENTITY TO RESPONSE
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