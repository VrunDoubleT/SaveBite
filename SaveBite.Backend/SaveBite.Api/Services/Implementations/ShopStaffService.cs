using SaveBite.Backend.Constants;
using SaveBite.Backend.Models.Common;
using System.Net;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public class ShopStaffService(
    IShopStaffRepository staffRepository
) : IShopStaffService
{
    // Retrieve staff members for the owned shop.

    public async Task<PagedResult<ShopStaffResponse>>
        GetShopStaffsAsync(
            Guid currentUserId,
            string? keyword,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        NormalizePagination(
            ref page,
            ref pageSize
        );

        var shop = await GetOwnerShopEntityAsync(
            currentUserId,
            cancellationToken
        );

        keyword = string.IsNullOrWhiteSpace(keyword)
            ? null
            : keyword.Trim();

        var result =
            await staffRepository.GetStaffsByShopIdPagedAsync(
                shop.Id,
                keyword,
                page,
                pageSize,
                cancellationToken
            );

        var items = result.Items
            .Select(MapToStaffResponse)
            .ToList();

        return PagedResult<ShopStaffResponse>.Create(
            items,
            page,
            pageSize,
            result.TotalItems
        );
    }

    // Update staff members for the owned shop.

   public async Task UpdateStaffInfoAsync(
       Guid currentUserId,
       Guid staffId,
       UpdateStaffInfoRequest request,
       CancellationToken cancellationToken = default)
   {
       var shop = await GetOwnerShopEntityAsync(
           currentUserId,
           cancellationToken
       );

       var staff =
           await staffRepository.GetStaffByIdAsync(
               staffId,
               cancellationToken
           );

       if (staff == null ||
           staff.ShopId != shop.Id ||
           staff.Status == ShopStaffStatus.Removed)
       {
           throw new AppException(
               "Staff member not found",
               ErrorCodes.StaffNotFound,
               HttpStatusCode.NotFound
           );
       }

       if (request.Status == ShopStaffStatus.Removed)
       {
           throw AppException.BadRequest(
               "Use the remove endpoint to remove a staff member",
               ErrorCodes.InvalidStaffStatus
           );
       }

       if (string.IsNullOrWhiteSpace(request.DisplayName))
       {
           throw AppException.BadRequest(
               "Display name is required",
               ErrorCodes.InvalidStaffStatus
           );
       }

       staff.DisplayName = request.DisplayName.Trim();

       staff.Nickname =
           string.IsNullOrWhiteSpace(request.StaffNickname)
               ? null
               : request.StaffNickname.Trim();

       staff.Status = request.Status;

       await staffRepository.UpdateStaffAsync(
           staff,
           cancellationToken
       );

       await staffRepository.SaveChangesAsync(
           cancellationToken
       );
   }

    // Remove staff members from the owned shop.

    public async Task RemoveStaffAsync(
        Guid currentUserId,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var shop = await GetOwnerShopEntityAsync(
            currentUserId,
            cancellationToken
        );

        var staff =
            await staffRepository.GetStaffByIdAsync(
                staffId,
                cancellationToken
            );

        if (staff == null ||
            staff.ShopId != shop.Id ||
            staff.Status == ShopStaffStatus.Removed)
        {
            throw new AppException(
                "Staff member not found",
                ErrorCodes.StaffNotFound,
                HttpStatusCode.NotFound
            );
        }

        // Soft delete.
        staff.Status =
            ShopStaffStatus.Removed;

        await staffRepository.UpdateStaffAsync(
            staff,
            cancellationToken
        );

        await staffRepository.SaveChangesAsync(
            cancellationToken
        );
    }

    // Retrieve staff activity logs for the owned shop.

    public async Task<PagedResult<StaffActivityLogResponse>>
        GetStaffActivityLogsAsync(
            Guid currentUserId,
            Guid staffId,
            int page,
            int pageSize,
            CancellationToken cancellationToken = default)
    {
        NormalizePagination(
            ref page,
            ref pageSize
        );

        var shop = await GetOwnerShopEntityAsync(
            currentUserId,
            cancellationToken
        );

        var staff =
            await staffRepository.GetStaffByIdAsync(
                staffId,
                cancellationToken
            );

        if (staff == null ||
            staff.ShopId != shop.Id)
        {
            throw new AppException(
                "Staff member not found",
                ErrorCodes.StaffNotFound,
                HttpStatusCode.NotFound
            );
        }

        var result =
            await staffRepository.GetStaffActivityLogsPagedAsync(
                shop.Id,
                staff.UserId,
                page,
                pageSize,
                cancellationToken
            );

        var items = result.Items
            .Select(log =>
                new StaffActivityLogResponse(
                    log.Id,
                    log.ActionBy,
                    log.Action.ToString(),
                    log.Note ?? string.Empty,
                    log.CreatedAt
                ))
            .ToList();

        return PagedResult<StaffActivityLogResponse>.Create(
            items,
            page,
            pageSize,
            result.TotalItems
        );
    }

    // Retrieve the current user shop memberships.

    public async Task<IEnumerable<AssociatedShopResponse>>
        GetAssociatedShopsAsync(
            Guid currentUserId,
            CancellationToken cancellationToken = default)
    {
        var memberships =
            await staffRepository
                .GetShopsByStaffUserIdAsync(
                    currentUserId,
                    cancellationToken
                );

        return memberships.Select(
            MapToAssociatedShopResponse
        );
    }

    public async Task<StaffShopInfoResponse>
        GetShopAndStaffInfoAsync(
            Guid currentUserId,
            Guid shopId,
            CancellationToken cancellationToken = default)
    {
        var staff =
            await staffRepository
                .GetStaffByShopAndUserAsync(
                    shopId,
                    currentUserId,
                    cancellationToken
                );

        if (staff == null ||
            staff.Status != ShopStaffStatus.Active)
        {
            throw AppException.Forbidden(
                "You are not an active staff member of this shop"
            );
        }

        var shop = staff.Shop;

        var shopResponse =
            new StaffShopDetailResponse(
                shop.Id,
                shop.Name,
                shop.Description,
                shop.AddressLine,
                shop.Ward,
                shop.District,
                shop.City,
                shop.LogoUrl,
                shop.CoverImageUrl,
                shop.OpeningTime,
                shop.ClosingTime,
                shop.Status
            );

        var staffResponse =
            MapToStaffResponse(staff);

        return new StaffShopInfoResponse(
            shopResponse,
            staffResponse
        );
    }

    // Provide private helper methods.

    // Resolve current owner's shop.
    // Owner APIs never trust a shopId supplied by FE.
    private async Task<Shop> GetOwnerShopEntityAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        var shop =
            await staffRepository
                .GetShopByOwnerUserIdAsync(
                    ownerUserId,
                    cancellationToken
                );

        if (shop == null)
        {
            throw AppException.NotFound(
                "Shop not found."
            );
        }

        return shop;
    }

    private static void NormalizePagination(
        ref int page,
        ref int pageSize)
    {
        if (page < 1)
            page = 1;

        if (pageSize < 1)
            pageSize = 10;

        // Prevent clients from requesting.
        // Excessively large pages.
        if (pageSize > 50)
            pageSize = 50;
    }

    // Map entities to response models.

    private static AssociatedShopResponse
        MapToAssociatedShopResponse(
            ShopStaff staff)
    {
        return new AssociatedShopResponse(
            staff.Id,
            staff.ShopId,
            staff.Shop.Name,
            staff.Status,
            staff.JoinedAt
        );
    }

    public async Task LeaveShopAsync(
        Guid currentUserId,
        Guid shopId,
        CancellationToken cancellationToken = default)
    {
        var staff =
            await staffRepository.GetStaffByShopAndUserAsync(
                shopId,
                currentUserId,
                cancellationToken
            );

        if (staff == null)
        {
            throw new AppException(
                "You are not a staff member of this shop",
                ErrorCodes.StaffNotFound,
                HttpStatusCode.NotFound
            );
        }

        if (staff.Status == ShopStaffStatus.Removed)
        {
            throw AppException.BadRequest(
                "You have already left this shop",
                ErrorCodes.InvalidStaffStatus
            );
        }

        staff.Status = ShopStaffStatus.Removed;

        var hasOtherMembership =
            await staffRepository.HasOtherStaffMembershipAsync(
                currentUserId,
                staff.Id,
                cancellationToken
            );

        if (!hasOtherMembership &&
            staff.User.Role == UserRole.Staff)
        {
            staff.User.Role = UserRole.User;
            staff.User.UpdatedAt = DateTime.UtcNow;
        }

        await staffRepository.UpdateStaffAsync(
            staff,
            cancellationToken
        );

        await staffRepository.SaveChangesAsync(
            cancellationToken
        );
    }

    private static ShopStaffResponse MapToStaffResponse(
        ShopStaff staff)
    {
        return new ShopStaffResponse(
            staff.Id,
            staff.UserId,
            staff.User.Email,
            staff.DisplayName,
            staff.Nickname,
            staff.Status,
            staff.JoinedAt
        );
    }
}
