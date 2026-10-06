using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SaveBite.Backend.Authorization;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Controllers;

[ApiController]
[Route("api/profile")]
[AccountAccess]
public sealed class UserProfileController : ControllerBase
{
    private readonly IUserAddressService _userAddressService;
    private readonly IUserProfileService _userProfileService;

    public UserProfileController(IUserAddressService userAddressService, IUserProfileService userProfileService)
    {
        _userAddressService = userAddressService;
        _userProfileService = userProfileService;
    }
    
    // UPDATE PROFILE
    [HttpPut("")]
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> UpdateProfile([FromBody] UpdateUserProfileRequest request, CancellationToken cancellationToken)
    {
        var profile = await _userProfileService.UpdateProfileAsync(GetAuthenticatedUserId(), request, cancellationToken);
        return Ok(ApiResponse<CurrentUserResponse>.Ok(profile, "Profile updated successfully."));
    }

    // UPLOAD AVATAR
    [HttpPost("avatar")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<CurrentUserResponse>>> UploadAvatar([FromForm] IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
            throw AppException.BadRequest("Avatar file is required.");
        
        var profile = await _userProfileService.UploadAvatarAsync(GetAuthenticatedUserId(), file, cancellationToken);
        return Ok(ApiResponse<CurrentUserResponse>.Ok(profile, "Avatar updated successfully."));
    }
    
    // GET ALL ADDRESSES
    [HttpGet("addresses")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserAddressResponse>>>> GetMyAddresses(CancellationToken cancellationToken)
    {
        var addresses= await _userAddressService.GetMyAddressesAsync(GetAuthenticatedUserId(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UserAddressResponse>>.Ok(addresses, "Get addresses successfully."));
    }
    
    // ADD NEW ADDRESS
    [HttpPost("addresses")]
    public async Task<ActionResult<ApiResponse<UserAddressResponse>>> CreateAddress([FromBody] UserAddressRequests.CreateAddressRequest request, CancellationToken cancellationToken)
    {
        var address = await _userAddressService.CreateAddressAsync(GetAuthenticatedUserId(), request, cancellationToken);
        return Ok(ApiResponse<UserAddressResponse>.Ok(address, "Address created successfully."));
    }
    
    // SET ADDRESS AS DEFAULT
    [HttpPut("addresses/{addressId:guid}/default")]
    public async Task<ActionResult<ApiResponse<UserAddressResponse>>> SetDefaultAddress(Guid addressId, CancellationToken cancellationToken)
    {
        var address = await _userAddressService.SetDefaultAddressAsync(GetAuthenticatedUserId(), addressId, cancellationToken);
        return Ok(ApiResponse<UserAddressResponse>.Ok(address, "Address set as default successfully."));
    }
    
    // UPDATE ADDRESS
    [HttpPut("addresses/{addressId:guid}")]
    public async Task<ActionResult<ApiResponse<UserAddressResponse>>> UpdateAddress(Guid addressId, [FromBody] UserAddressRequests.UpdateAddressRequest request, CancellationToken cancellationToken)
    {
        var address = await _userAddressService.UpdateAddressAsync(GetAuthenticatedUserId(), addressId, request, cancellationToken);
        return Ok(ApiResponse<UserAddressResponse>.Ok(address, "Address updated successfully."));
    }
    
    // DELETE ADDRESS
    [HttpDelete("addresses/{addressId:guid}")]
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