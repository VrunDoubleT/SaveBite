using System.Net;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;
using InvitationStatus = SaveBite.Backend.Models.Enums.StaffInvitation;
using StaffInvitation = SaveBite.Backend.Models.Entities.StaffInvitation;

namespace SaveBite.Backend.Services.Implementations;

public class ShopStaffService(IShopStaffRepository staffRepository) : IShopStaffService
{
    // Entity stores status as string, so the values come from the enum member names
    private const string StatusPending = nameof(InvitationStatus.Pending);
    private const string StatusAccepted = nameof(InvitationStatus.Accepted);
    private const string StatusDeclined = nameof(InvitationStatus.Declined);
    private const string StatusCancelled = nameof(InvitationStatus.Cancelled);

    // Display-only status: an unanswered invitation past its lifetime (never stored in DB)
    private const string StatusExpired = "Expired";
    private const int InvitationLifetimeDays = 3;

    public async Task<StaffInvitationResponse> InviteStaffAsync(
        Guid currentUserId, Guid shopId, InviteStaffRequest request, CancellationToken cancellationToken = default)
    {
        var invitedUser = await staffRepository.GetUserByEmailAsync(request.InvitedUserEmail, cancellationToken)
            ?? throw new AppException("User not found", ErrorCodes.UserNotFound, HttpStatusCode.NotFound);
          if (invitedUser.Status != UserStatus.Active)
          {
              throw new AppException(
                  "This account is not active and cannot be invited as staff",
                  ErrorCodes.AccountInactive,
                  HttpStatusCode.Conflict);
          }
        if (invitedUser.Id == currentUserId)
            throw AppException.BadRequest("Cannot invite yourself as staff", ErrorCodes.CannotInviteSelf);

        var existingStaff = await staffRepository.GetStaffByShopAndUserAsync(
            shopId,
            invitedUser.Id,
            cancellationToken);
        
        if (existingStaff != null)
        {
            if (existingStaff.Status == ShopStaffStatus.Active)
            {
                throw new AppException(
                    "User is already an active staff member of this shop",
                    ErrorCodes.AlreadyActiveStaff,
                    HttpStatusCode.Conflict);
            }
        
            if (existingStaff.Status == ShopStaffStatus.Suspended)
            {
                throw new AppException(
                    "This user has been suspended from working at this shop",
                    ErrorCodes.StaffSuspended,
                    HttpStatusCode.Conflict);
            }
        }

        var pendingInvitation = await staffRepository.GetPendingInvitationAsync(shopId, invitedUser.Id, cancellationToken);
        if (pendingInvitation != null)
        {
            if (!IsExpired(pendingInvitation))
                throw new AppException(
                    "A pending invitation already exists for this user",
                    ErrorCodes.PendingInvitationExists,
                    HttpStatusCode.Conflict);

            // Old invitation timed out: close it so a fresh one can be sent
            pendingInvitation.Status = StatusCancelled;
            await staffRepository.UpdateInvitationAsync(pendingInvitation, cancellationToken);
        }

        var invitation = new StaffInvitation
        {
            Id = Guid.NewGuid(),
            ShopId = shopId,
            InvitedUserId = invitedUser.Id,
            InvitedBy = currentUserId,
            Status = StatusPending,
            InvitedAt = DateTime.UtcNow
        };

        await staffRepository.AddInvitationAsync(invitation, cancellationToken);
        await staffRepository.SaveChangesAsync(cancellationToken);

        // Reload so navigation properties (Shop, InvitedUser) are populated
        var created = await staffRepository.GetInvitationByIdAsync(invitation.Id, cancellationToken);
        return MapToInvitationResponse(created!);
    }
    
    public async Task<OwnerShopResponse> GetOwnerShopAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        var shop =
            await staffRepository.GetShopByOwnerUserIdAsync(
                ownerUserId,
                cancellationToken
            );

        if (shop is null)
        {
            throw AppException.NotFound("Shop not found.");
        }

        return new OwnerShopResponse(
            shop.Id,
            shop.Name
        );
    }

    public async Task RevokeInvitationAsync(
        Guid currentUserId, Guid shopId, Guid invitationId, CancellationToken cancellationToken = default)
    {
        var invitation = await staffRepository.GetInvitationByIdAsync(invitationId, cancellationToken);
        if (invitation == null || invitation.ShopId != shopId)
            throw new AppException("Invitation not found", ErrorCodes.InvitationNotFound, HttpStatusCode.NotFound);

        if (invitation.Status != StatusPending)
            throw AppException.BadRequest("Only pending invitations can be revoked", ErrorCodes.InvitationInvalidOrExpired);

        invitation.Status = StatusCancelled; // Revoke is stored as Cancelled
        invitation.RespondedAt = DateTime.UtcNow;
        await staffRepository.UpdateInvitationAsync(invitation, cancellationToken);
        await staffRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task AcceptInvitationAsync(
        Guid currentUserId, Guid invitationId, CancellationToken cancellationToken = default)
    {
        var invitation = await staffRepository.GetInvitationByIdAsync(invitationId, cancellationToken);
        if (invitation == null || invitation.InvitedUserId != currentUserId)
            throw new AppException("Invitation not found", ErrorCodes.InvitationNotFound, HttpStatusCode.NotFound);
     if (invitation.InvitedUser.Status != UserStatus.Active)
     {
         throw new AppException(
             "This account is not active and cannot accept staff invitations",
             ErrorCodes.AccountInactive,
             HttpStatusCode.Forbidden);
     }
        if (invitation.Status != StatusPending || IsExpired(invitation))
            throw AppException.BadRequest("Invitation is invalid, expired or no longer pending",
                ErrorCodes.InvitationInvalidOrExpired);

        invitation.Status = StatusAccepted;
        invitation.RespondedAt = DateTime.UtcNow;

        var existingStaff =
            await staffRepository.GetStaffByShopAndUserAsync(invitation.ShopId, currentUserId, cancellationToken);
        if (existingStaff?.Status == ShopStaffStatus.Suspended)
        {
            throw new AppException(
                "This staff membership has been suspended by the shop",
                ErrorCodes.StaffSuspended,
                HttpStatusCode.Forbidden);
        }
        if (existingStaff != null)
        {
            existingStaff.Status = ShopStaffStatus.Active;
            existingStaff.DisplayName = invitation.InvitedUser.FullName;

            await staffRepository.UpdateStaffAsync(
                existingStaff,
                cancellationToken);
        }
        else
        {
            var newStaff = new ShopStaff
            {
                Id = Guid.NewGuid(),
                ShopId = invitation.ShopId,
                UserId = currentUserId,
                DisplayName = invitation.InvitedUser.FullName,
                Status = ShopStaffStatus.Active,
                JoinedAt = DateTime.UtcNow
            };

            await staffRepository.AddStaffAsync(
                newStaff,
                cancellationToken);
        }

        if (invitation.InvitedUser.Role == UserRole.User)
        {
            invitation.InvitedUser.Role = UserRole.Staff;
            invitation.InvitedUser.UpdatedAt = DateTime.UtcNow;
        }

        await staffRepository.UpdateInvitationAsync(
            invitation,
            cancellationToken);

        await staffRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task DeclineInvitationAsync(
        Guid currentUserId, Guid invitationId, CancellationToken cancellationToken = default)
    {
        var invitation = await staffRepository.GetInvitationByIdAsync(invitationId, cancellationToken);
        if (invitation == null || invitation.InvitedUserId != currentUserId)
            throw new AppException("Invitation not found", ErrorCodes.InvitationNotFound, HttpStatusCode.NotFound);

        if (invitation.Status != StatusPending || IsExpired(invitation))
            throw AppException.BadRequest("Invitation is expired or no longer pending", ErrorCodes.InvitationInvalidOrExpired);

        invitation.Status = StatusDeclined;
        invitation.RespondedAt = DateTime.UtcNow;

        await staffRepository.UpdateInvitationAsync(invitation, cancellationToken);
        await staffRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<StaffInvitationResponse>> GetShopInvitationsAsync(
        Guid currentUserId, Guid shopId, CancellationToken cancellationToken = default)
    {
        var invitations = await staffRepository.GetInvitationsByShopIdAsync(shopId, cancellationToken);
        return invitations.Select(MapToInvitationResponse);
    }

    public async Task<IEnumerable<StaffInvitationResponse>> GetCustomerInvitationsAsync(
        Guid currentUserId, CancellationToken cancellationToken = default)
    {
        var invitations = await staffRepository.GetInvitationsByUserIdAsync(currentUserId, cancellationToken);
        return invitations.Select(MapToInvitationResponse);
    }

    public async Task<IEnumerable<ShopStaffResponse>> GetShopStaffsAsync(
        Guid currentUserId, Guid shopId, CancellationToken cancellationToken = default)
    {
        var staffs = await staffRepository.GetStaffsByShopIdAsync(shopId, cancellationToken);
        return staffs.Select(MapToStaffResponse);
    }

    public async Task UpdateStaffInfoAsync(
        Guid currentUserId, Guid shopId, Guid staffId, UpdateStaffInfoRequest request, CancellationToken cancellationToken = default)
    {
        var staff = await staffRepository.GetStaffByIdAsync(staffId, cancellationToken);
        if (staff == null || staff.ShopId != shopId || staff.Status == ShopStaffStatus.Removed)
            throw new AppException("Staff member not found", ErrorCodes.StaffNotFound, HttpStatusCode.NotFound);

        // Removing goes through RemoveStaffAsync only
        if (request.Status == ShopStaffStatus.Removed)
            throw AppException.BadRequest("Use the remove endpoint to remove a staff member", ErrorCodes.InvalidStaffStatus);

        staff.Nickname = string.IsNullOrWhiteSpace(request.StaffNickname) ? null : request.StaffNickname.Trim();
        staff.Status = request.Status;

        await staffRepository.UpdateStaffAsync(staff, cancellationToken);
        await staffRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveStaffAsync(
        Guid currentUserId, Guid shopId, Guid staffId, CancellationToken cancellationToken = default)
    {
        var staff = await staffRepository.GetStaffByIdAsync(staffId, cancellationToken);
        if (staff == null || staff.ShopId != shopId || staff.Status == ShopStaffStatus.Removed)
            throw new AppException("Staff member not found", ErrorCodes.StaffNotFound, HttpStatusCode.NotFound);

        staff.Status = ShopStaffStatus.Removed; // Soft delete

        await staffRepository.UpdateStaffAsync(staff, cancellationToken);
        await staffRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<StaffActivityLogResponse>> GetStaffActivityLogsAsync(
        Guid currentUserId,
        Guid shopId,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        var staff = await staffRepository.GetStaffByIdAsync(
            staffId,
            cancellationToken);

        if (staff == null || staff.ShopId != shopId)
        {
            throw new AppException(
                "Staff member not found",
                ErrorCodes.StaffNotFound,
                HttpStatusCode.NotFound);
        }


        var logs = await staffRepository.GetStaffActivityLogsAsync(
            shopId,
            staff.UserId,
            cancellationToken);


        return logs.Select(l =>
            new StaffActivityLogResponse(
                l.Id,
                l.ActionBy,
                l.Action.ToString(),
                l.Note ?? string.Empty,
                l.CreatedAt
            ));
    }

    public async Task<IEnumerable<AssociatedShopResponse>> GetAssociatedShopsAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        var memberships = await staffRepository
            .GetShopsByStaffUserIdAsync(currentUserId, cancellationToken);

        return memberships.Select(MapToAssociatedShopResponse);
    }

    public async Task<StaffShopInfoResponse> GetShopAndStaffInfoAsync(
        Guid currentUserId,
        Guid shopId,
        CancellationToken cancellationToken = default)
    {
        var staff = await staffRepository
            .GetStaffByShopAndUserAsync(
                shopId,
                currentUserId,
                cancellationToken);

        if (staff == null || staff.Status != ShopStaffStatus.Active)
            throw AppException.Forbidden(
                "You are not an active staff member of this shop");

        var shop = staff.Shop;

        var shopResponse = new StaffShopDetailResponse(
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

        var staffResponse = new ShopStaffResponse(
            staff.Id,
            staff.UserId,
            staff.User.FullName,
            staff.User.Email,
            staff.Nickname,
            staff.Status,
            staff.JoinedAt
        );

        return new StaffShopInfoResponse(
            shopResponse,
            staffResponse
        );
    }

    private static bool IsExpired(StaffInvitation invitation) =>
        invitation.Status == StatusPending
        && invitation.InvitedAt.AddDays(InvitationLifetimeDays) <= DateTime.UtcNow;

    private static StaffInvitationResponse MapToInvitationResponse(StaffInvitation invitation) =>
        new(
            invitation.Id,
            invitation.ShopId,
            invitation.Shop.Name,
            invitation.InvitedUserId,
            invitation.InvitedUser.FullName,
            invitation.InvitedUser.Email,
            IsExpired(invitation) ? StatusExpired : invitation.Status,
            invitation.InvitedAt,
            invitation.InvitedAt.AddDays(InvitationLifetimeDays));

    private static AssociatedShopResponse MapToAssociatedShopResponse(ShopStaff staff) =>
        new(
            staff.Id,
            staff.ShopId,
            staff.Shop.Name,
            staff.Status,
            staff.JoinedAt
        );
    
    private static ShopStaffResponse MapToStaffResponse(ShopStaff staff) =>
        new(
            staff.Id,
            staff.UserId,
            staff.User.FullName,
            staff.User.Email,
            staff.Nickname,
            staff.Status,
            staff.JoinedAt);
}