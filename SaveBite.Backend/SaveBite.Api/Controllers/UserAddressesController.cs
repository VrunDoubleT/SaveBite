using SaveBite.Backend.Models.Common;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

// Manage the current user's addresses.
[ApiController]
[Route("api/user-addresses")]
public sealed class UserAddressesController(IUserAddressService userAddressService) : ControllerBase
{
    private readonly IUserAddressService _userAddressService = userAddressService;

    [HttpGet]
    // Retrieve all addresses for the current user.
    [CustomerAnyStatusAccess]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserAddressResponse>>>> GetMyAddresses(CancellationToken cancellationToken)
    {
        var addresses= await _userAddressService.GetMyAddressesAsync(GetAuthenticatedUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UserAddressResponse>>.Ok(addresses, "Get addresses successfully."));
    }

    [HttpPost]
    // Add a new address.
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<UserAddressResponse>>> CreateAddress([FromBody] UserAddressRequests.CreateAddressRequest request, CancellationToken cancellationToken)
    {
        var address = await _userAddressService.CreateAddressAsync(GetAuthenticatedUserId(), request, cancellationToken);
        return Ok(ApiResponse<UserAddressResponse>.Ok(address, "Address created successfully."));
    }

    [HttpPut("{addressId:guid}/default")]
    // Set the address as the default.
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<UserAddressResponse>>> SetDefaultAddress(Guid addressId, CancellationToken cancellationToken)
    {
        var address = await _userAddressService.SetDefaultAddressAsync(GetAuthenticatedUserId(), addressId, cancellationToken);
        return Ok(ApiResponse<UserAddressResponse>.Ok(address, "Address set as default successfully."));
    }

    [HttpPut("{addressId:guid}")]
    // Update an address.
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse<UserAddressResponse>>> UpdateAddress(Guid addressId, [FromBody] UserAddressRequests.UpdateAddressRequest request, CancellationToken cancellationToken)
    {
        var address = await _userAddressService.UpdateAddressAsync(GetAuthenticatedUserId(), addressId, request, cancellationToken);
        return Ok(ApiResponse<UserAddressResponse>.Ok(address, "Address updated successfully."));
    }

    [HttpDelete("{addressId:guid}")]
    // Delete an address.
    [CustomerAccess]
    public async Task<ActionResult<ApiResponse>> DeleteAddress(Guid addressId, CancellationToken cancellationToken)
    {
        await _userAddressService.DeleteAddressAsync(GetAuthenticatedUserId(), addressId, cancellationToken);
        return Ok(ApiResponse.Ok("Address deleted successfully."));
    }

    private Guid GetAuthenticatedUserId()
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(userIdValue, out var userId) ? userId : throw AppException.Unauthorized();
    }
}
