using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.DTOs;
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
    IShopStaffRepository staffRepository,
    IUserRepository  userRepository,
    INotificationQueue notifications,
    SaveBite.Backend.Data.AppDbContext db
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
        await NotificationTransaction.RunAsync(db, async () =>
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

           var previousStatus = staff.Status;
           staff.Status = request.Status;

           await staffRepository.UpdateStaffAsync(
               staff,
               cancellationToken
           );

           await staffRepository.SaveChangesAsync(
               cancellationToken
           );
           if (previousStatus != staff.Status)
               await notifications.EnqueueAsync(NotificationTarget.User(staff.UserId), new CreateNotificationCommand(
                   NotificationType.StaffStatusChanged, "Staff access updated",
                   $"Your staff access at {shop.Name} is now {staff.Status}.", "/account/workspace",
                   $"staff-status:{staff.Id}:{staff.Status}:{Guid.NewGuid()}"), cancellationToken);

        }, cancellationToken);
    }

    // Remove staff members from the owned shop.

    public async Task RemoveStaffAsync(
        Guid currentUserId,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        await NotificationTransaction.RunAsync(db, async () =>
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
            
            var user = await userRepository.GetUserByIdAsync(
                staff.UserId,
                cancellationToken
            );

            if (user != null && user.Role == UserRole.Staff)
            {
                user.Role = UserRole.User;
            }

            await staffRepository.SaveChangesAsync(
                cancellationToken
            );
            await notifications.EnqueueAsync(NotificationTarget.User(staff.UserId), new CreateNotificationCommand(
                NotificationType.StaffStatusChanged, "Staff membership removed",
                $"Your staff membership at {shop.Name} has been removed.", "/account/workspace",
                $"staff-removed:{staff.Id}:{Guid.NewGuid()}"), cancellationToken);

        }, cancellationToken);
    }

    // Retrieve staff activity logs for the owned shop.

    public async Task<PagedResult<StaffActivityLogResponse>>
        GetStaffActivityLogsAsync(
            Guid currentUserId,
            Guid staffId,
            int page,
            int pageSize,
            DateOnly? fromDate,
            DateOnly? toDate,
            CancellationToken cancellationToken = default)
    {
        NormalizePagination(
            ref page,
            ref pageSize
        );

        if (fromDate.HasValue && toDate.HasValue && fromDate > toDate)
        {
            throw AppException.BadRequest("From date must not be after to date.");
        }

        // Dates are Vietnam calendar days: [from 00:00, day after "to" 00:00) converted to UTC.
        DateTime? fromUtc = fromDate.HasValue
            ? OrderBusinessDate.StartOfDayUtc(fromDate.Value)
            : null;
        DateTime? toUtcExclusive = toDate.HasValue
            ? OrderBusinessDate.StartOfDayUtc(toDate.Value.AddDays(1))
            : null;

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
                fromUtc,
                toUtcExclusive,
                cancellationToken
            );

        var items = result.Items
            .Select(log =>
                new StaffActivityLogResponse(
                    log.Id,
                    log.ActionBy,
                    log.Action.ToString(),
                    log.Note ?? string.Empty,
                    log.OrderId,
                    log.CreatedAt,
                    log.ActorNameSnapshot ?? staff.DisplayName,
                    log.ActorRoleSnapshot ?? "Staff",
                    log.Order?.OrderCode
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
            staff.JoinedAt,
            staff.Shop.LogoUrl,
            staff.Shop.CoverImageUrl
        );
    }

        public async Task LeaveShopAsync(
        Guid currentUserId,
        Guid shopId,
        CancellationToken cancellationToken = default)
    {
        await NotificationTransaction.RunAsync(db, async () =>
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

            if (staff.Status != ShopStaffStatus.Active)
            {
                if (staff.Status == ShopStaffStatus.Removed)
                {
                    throw AppException.BadRequest(
                        "You have already left this shop",
                        ErrorCodes.InvalidStaffStatus
                    );
                }

                if (staff.Status == ShopStaffStatus.Suspended)
                {
                    throw new AppException(
                        "This staff membership has been suspended by the shop",
                        ErrorCodes.StaffSuspended,
                        HttpStatusCode.Forbidden
                    );
                }

                throw AppException.BadRequest(
                    "Only active staff members can leave this shop",
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
            await notifications.EnqueueAsync(NotificationTarget.User(staff.Shop.OwnerUserId), new CreateNotificationCommand(
                NotificationType.StaffLeftShop, "A staff member left your shop",
                $"{staff.DisplayName} has left {staff.Shop.Name}.", "/store-owner/staff",
                $"staff-left:{staff.Id}:{Guid.NewGuid()}"), cancellationToken);

        }, cancellationToken);
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
