using System.Net;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

using InvitationStatus = SaveBite.Backend.Models.Enums.StaffInvitation;
using StaffInvitation = SaveBite.Backend.Models.Entities.StaffInvitation;

namespace SaveBite.Backend.Services.Implementations;

public class ShopStaffService(
    IShopStaffRepository staffRepository
) : IShopStaffService
{
    private const string StatusPending =
        nameof(InvitationStatus.Pending);

    private const string StatusAccepted =
        nameof(InvitationStatus.Accepted);

    private const string StatusDeclined =
        nameof(InvitationStatus.Declined);

    private const string StatusCancelled =
        nameof(InvitationStatus.Cancelled);

    private const string StatusExpired = "Expired";

    private const int InvitationLifetimeDays = 3;


    // =========================================================
    // OWNER SHOP
    // =========================================================

    public async Task<OwnerShopResponse> GetOwnerShopAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        var shop = await GetOwnerShopEntityAsync(
            ownerUserId,
            cancellationToken
        );

        return new OwnerShopResponse(
            shop.Id,
            shop.Name
        );
    }


    // =========================================================
    // SEARCH STAFF CANDIDATES
    // =========================================================

    public async Task<PagedResult<StaffCandidateResponse>>
        SearchStaffCandidatesAsync(
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
            await staffRepository.SearchStaffCandidatesAsync(
                shop.Id,
                currentUserId,
                keyword,
                page,
                pageSize,
                cancellationToken
            );

        var items = result.Items
            .Select(user =>
                new StaffCandidateResponse(
                    user.Id,
                    user.FullName,
                    user.Email
                ))
            .ToList();

        return PagedResult<StaffCandidateResponse>.Create(
            items,
            page,
            pageSize,
            result.TotalItems
        );
    }


    // =========================================================
    // OWNER - INVITATIONS
    // =========================================================

    public async Task<StaffInvitationResponse> InviteStaffAsync(
        Guid currentUserId,
        InviteStaffRequest request,
        CancellationToken cancellationToken = default)
    {
        var shop = await GetOwnerShopEntityAsync(
            currentUserId,
            cancellationToken
        );

        var invitedUser =
            await staffRepository.GetUserByIdAsync(
                request.UserId,
                cancellationToken
            )
            ?? throw new AppException(
                "User not found",
                ErrorCodes.UserNotFound,
                HttpStatusCode.NotFound
            );

        // Only active accounts can become staff
        if (invitedUser.Status != UserStatus.Active)
        {
            throw new AppException(
                "This account is not active and cannot be invited as staff",
                ErrorCodes.AccountInactive,
                HttpStatusCode.Conflict
            );
        }

        if (invitedUser.Role == UserRole.Admin)
        {
            throw AppException.BadRequest(
                "Admin accounts cannot be invited as staff",
                ErrorCodes.InvalidStaffCandidate
            );
        }

        if (invitedUser.Id == currentUserId)
        {
            throw AppException.BadRequest(
                "Cannot invite yourself as staff",
                ErrorCodes.CannotInviteSelf
            );
        }

        var existingStaff =
            await staffRepository.GetStaffByShopAndUserAsync(
                shop.Id,
                invitedUser.Id,
                cancellationToken
            );

        if (existingStaff != null)
        {
            if (existingStaff.Status == ShopStaffStatus.Active)
            {
                throw new AppException(
                    "User is already an active staff member of this shop",
                    ErrorCodes.AlreadyActiveStaff,
                    HttpStatusCode.Conflict
                );
            }

            if (existingStaff.Status == ShopStaffStatus.Suspended)
            {
                throw new AppException(
                    "This user has been suspended from working at this shop",
                    ErrorCodes.StaffSuspended,
                    HttpStatusCode.Conflict
                );
            }
        }

        var pendingInvitation =
            await staffRepository.GetPendingInvitationAsync(
                shop.Id,
                invitedUser.Id,
                cancellationToken
            );

        if (pendingInvitation != null)
        {
            // Invitation is still valid
            if (!IsExpired(pendingInvitation))
            {
                throw new AppException(
                    "A pending invitation already exists for this user",
                    ErrorCodes.PendingInvitationExists,
                    HttpStatusCode.Conflict
                );
            }

            // Old invitation has expired.
            // Close it before creating another invitation.
            pendingInvitation.Status = StatusCancelled;
            pendingInvitation.RespondedAt = DateTime.UtcNow;

            await staffRepository.UpdateInvitationAsync(
                pendingInvitation,
                cancellationToken
            );
        }

        var invitation = new StaffInvitation
        {
            Id = Guid.NewGuid(),

            ShopId = shop.Id,

            InvitedUserId = invitedUser.Id,

            InvitedBy = currentUserId,

            Status = StatusPending,

            InvitedAt = DateTime.UtcNow
        };

        await staffRepository.AddInvitationAsync(
            invitation,
            cancellationToken
        );

        await staffRepository.SaveChangesAsync(
            cancellationToken
        );

        // Reload navigation properties
        var created =
            await staffRepository.GetInvitationByIdAsync(
                invitation.Id,
                cancellationToken
            );

        if (created == null)
        {
            throw new AppException(
                "Failed to retrieve created invitation",
                ErrorCodes.InvitationNotFound,
                HttpStatusCode.InternalServerError
            );
        }

        return MapToInvitationResponse(created);
    }


    public async Task RevokeInvitationAsync(
        Guid currentUserId,
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        var shop = await GetOwnerShopEntityAsync(
            currentUserId,
            cancellationToken
        );

        var invitation =
            await staffRepository.GetInvitationByIdAsync(
                invitationId,
                cancellationToken
            );

        // Prevent owner from revoking invitations
        // belonging to another shop
        if (invitation == null ||
            invitation.ShopId != shop.Id)
        {
            throw new AppException(
                "Invitation not found",
                ErrorCodes.InvitationNotFound,
                HttpStatusCode.NotFound
            );
        }

        if (invitation.Status != StatusPending)
        {
            throw AppException.BadRequest(
                "Only pending invitations can be revoked",
                ErrorCodes.InvitationInvalidOrExpired
            );
        }

        if (IsExpired(invitation))
        {
            throw AppException.BadRequest(
                "Invitation has already expired",
                ErrorCodes.InvitationInvalidOrExpired
            );
        }

        invitation.Status = StatusCancelled;
        invitation.RespondedAt = DateTime.UtcNow;

        await staffRepository.UpdateInvitationAsync(
            invitation,
            cancellationToken
        );

        await staffRepository.SaveChangesAsync(
            cancellationToken
        );
    }


    public async Task<PagedResult<StaffInvitationResponse>>
        GetShopInvitationsAsync(
            Guid currentUserId,
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

        var result =
            await staffRepository.GetInvitationsByShopIdPagedAsync(
                shop.Id,
                page,
                pageSize,
                cancellationToken
            );

        var items = result.Items
            .Select(MapToInvitationResponse)
            .ToList();

        return PagedResult<StaffInvitationResponse>.Create(
            items,
            page,
            pageSize,
            result.TotalItems
        );
    }


    // =========================================================
    // CUSTOMER - INVITATIONS
    // =========================================================

    public async Task<IEnumerable<StaffInvitationResponse>>
        GetCustomerInvitationsAsync(
            Guid currentUserId,
            CancellationToken cancellationToken = default)
    {
        var invitations =
            await staffRepository.GetInvitationsByUserIdAsync(
                currentUserId,
                cancellationToken
            );

        return invitations.Select(
            MapToInvitationResponse
        );
    }


    public async Task AcceptInvitationAsync(
        Guid currentUserId,
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        var invitation =
            await staffRepository.GetInvitationByIdAsync(
                invitationId,
                cancellationToken
            );

        if (invitation == null ||
            invitation.InvitedUserId != currentUserId)
        {
            throw new AppException(
                "Invitation not found",
                ErrorCodes.InvitationNotFound,
                HttpStatusCode.NotFound
            );
        }

        if (invitation.InvitedUser.Status != UserStatus.Active)
        {
            throw new AppException(
                "This account is not active and cannot accept staff invitations",
                ErrorCodes.AccountInactive,
                HttpStatusCode.Forbidden
            );
        }
        
        if (invitation.InvitedUser.Role == UserRole.Admin)
        {
            throw AppException.BadRequest(
                "Admin accounts cannot become shop staff",
                ErrorCodes.InvalidStaffCandidate
            );
        }

        if (invitation.Status != StatusPending ||
            IsExpired(invitation))
        {
            throw AppException.BadRequest(
                "Invitation is invalid, expired or no longer pending",
                ErrorCodes.InvitationInvalidOrExpired
            );
        }

        invitation.Status = StatusAccepted;
        invitation.RespondedAt = DateTime.UtcNow;

        var existingStaff =
            await staffRepository.GetStaffByShopAndUserAsync(
                invitation.ShopId,
                currentUserId,
                cancellationToken
            );

        if (existingStaff?.Status ==
            ShopStaffStatus.Suspended)
        {
            throw new AppException(
                "This staff membership has been suspended by the shop",
                ErrorCodes.StaffSuspended,
                HttpStatusCode.Forbidden
            );
        }

        if (existingStaff != null)
        {
            // Reactivate an existing removed membership
            existingStaff.Status =
                ShopStaffStatus.Active;

            // Entity is kept unchanged.
            existingStaff.DisplayName =
                invitation.InvitedUser.FullName;

            await staffRepository.UpdateStaffAsync(
                existingStaff,
                cancellationToken
            );
        }
        else
        {
            var newStaff = new ShopStaff
            {
                Id = Guid.NewGuid(),

                ShopId = invitation.ShopId,

                UserId = currentUserId,

                // Keep existing entity field
                DisplayName =
                    invitation.InvitedUser.FullName,

                // Store-specific nickname is initially empty
                Nickname = null,

                Status = ShopStaffStatus.Active,

                JoinedAt = DateTime.UtcNow
            };

            await staffRepository.AddStaffAsync(
                newStaff,
                cancellationToken
            );
        }

        // Promote normal User to Staff role
        if (invitation.InvitedUser.Role ==
            UserRole.User)
        {
            invitation.InvitedUser.Role =
                UserRole.Staff;

            invitation.InvitedUser.UpdatedAt =
                DateTime.UtcNow;
        }

        await staffRepository.UpdateInvitationAsync(
            invitation,
            cancellationToken
        );

        await staffRepository.SaveChangesAsync(
            cancellationToken
        );
    }


    public async Task DeclineInvitationAsync(
        Guid currentUserId,
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        var invitation =
            await staffRepository.GetInvitationByIdAsync(
                invitationId,
                cancellationToken
            );

        if (invitation == null ||
            invitation.InvitedUserId != currentUserId)
        {
            throw new AppException(
                "Invitation not found",
                ErrorCodes.InvitationNotFound,
                HttpStatusCode.NotFound
            );
        }

        if (invitation.Status != StatusPending ||
            IsExpired(invitation))
        {
            throw AppException.BadRequest(
                "Invitation is expired or no longer pending",
                ErrorCodes.InvitationInvalidOrExpired
            );
        }

        invitation.Status = StatusDeclined;
        invitation.RespondedAt = DateTime.UtcNow;

        await staffRepository.UpdateInvitationAsync(
            invitation,
            cancellationToken
        );

        await staffRepository.SaveChangesAsync(
            cancellationToken
        );
    }


    // =========================================================
    // OWNER - STAFF LIST
    // =========================================================

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


    // =========================================================
    // OWNER - UPDATE STAFF
    // =========================================================

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

        // Removed must only be performed through
        // DELETE /staffs/{staffId}
        if (request.Status ==
            ShopStaffStatus.Removed)
        {
            throw AppException.BadRequest(
                "Use the remove endpoint to remove a staff member",
                ErrorCodes.InvalidStaffStatus
            );
        }

        // Nickname behaves like a store-specific
        // contact name.
        //
        // null / "" => display User.FullName
        staff.Nickname =
            string.IsNullOrWhiteSpace(
                request.StaffNickname
            )
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


    // =========================================================
    // OWNER - REMOVE STAFF
    // =========================================================

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

        // Soft delete
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


    // =========================================================
    // OWNER - STAFF ACTIVITY LOGS
    // =========================================================

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


    // =========================================================
    // STAFF SIDE
    // =========================================================

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


    // =========================================================
    // PRIVATE HELPERS
    // =========================================================

    /// <summary>
    /// Resolve current owner's shop.
    /// Owner APIs never trust a shopId supplied by FE.
    /// </summary>
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

        // Prevent clients from requesting
        // excessively large pages.
        if (pageSize > 50)
            pageSize = 50;
    }


    private static bool IsExpired(
        StaffInvitation invitation)
    {
        return invitation.Status ==
                   StatusPending
               &&
               invitation.InvitedAt
                   .AddDays(InvitationLifetimeDays)
               <= DateTime.UtcNow;
    }


    // =========================================================
    // MAPPERS
    // =========================================================

    private static StaffInvitationResponse
        MapToInvitationResponse(
            StaffInvitation invitation)
    {
        return new StaffInvitationResponse(
            invitation.Id,
            invitation.ShopId,
            invitation.Shop.Name,
            invitation.InvitedUserId,
            invitation.InvitedUser.FullName,
            invitation.InvitedUser.Email,

            IsExpired(invitation)
                ? StatusExpired
                : invitation.Status,

            invitation.InvitedAt,

            invitation.InvitedAt.AddDays(
                InvitationLifetimeDays
            )
        );
    }


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


    private static ShopStaffResponse
        MapToStaffResponse(
            ShopStaff staff)
    {
        // Store nickname has priority.
        // If no nickname exists, use account FullName.
        var displayName =
            string.IsNullOrWhiteSpace(
                staff.Nickname
            )
                ? staff.User.FullName
                : staff.Nickname;

        return new ShopStaffResponse(
            staff.Id,
            staff.UserId,
            staff.User.FullName,
            staff.User.Email,
            staff.Nickname,
            displayName,
            staff.Status,
            staff.JoinedAt
        );
    }
}