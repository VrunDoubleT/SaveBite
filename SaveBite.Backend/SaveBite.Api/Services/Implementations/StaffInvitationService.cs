using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.BackgroundJobs.Jobs;
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
using StaffInvitation = SaveBite.Backend.Models.Entities.StaffInvitation;

namespace SaveBite.Backend.Services.Implementations;

public sealed class StaffInvitationService(
    IStaffInvitationRepository invitationRepository,
    IShopStaffRepository staffRepository,
    IStaffInvitationScheduler scheduler,
    StaffInvitationNotificationService notifications,
    SaveBite.Backend.Data.AppDbContext db,
    IRealtimePublisher realtime
) : IStaffInvitationService
{
    private Task TryNotifyAsync(Guid invitationId, StaffInvitationStatus status, CancellationToken ct)
        => notifications.NotifyAsync(invitationId, status, ct);

    private async Task<bool> TransitionAndNotifyAsync(Guid id, string status, DateTime now, CancellationToken ct)
    {
        var hasOuterTransaction = db.Database.CurrentTransaction != null;
        var changed = await NotificationTransaction.RunAsync(db, async () =>
        {
            var transitioned = await invitationRepository.TryTransitionPendingAsync(id, status, now, ct);
            if (transitioned && status != StatusAccepted)
                await notifications.NotifyAsync(id, Enum.Parse<StaffInvitationStatus>(status), ct);
            return transitioned;
        }, ct);
        if (changed && !hasOuterTransaction)
        {
            var invitation = await invitationRepository.GetInvitationByIdAsync(id, ct);
            if (invitation != null) await StaffRealtimeEvents.InvitationChangedAsync(realtime, invitation);
        }
        return changed;
    }

    private const string StatusPending =
        nameof(StaffInvitationStatus.Pending);

    private const string StatusAccepted =
        nameof(StaffInvitationStatus.Accepted);

    private const string StatusDeclined =
        nameof(StaffInvitationStatus.Declined);

    private const string StatusCancelled =
        nameof(StaffInvitationStatus.Cancelled);

    private const string StatusExpired = nameof(StaffInvitationStatus.Expired);

    private const int InvitationLifetimeDays = StaffInvitationJob.LifetimeDays;
    

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
            await invitationRepository.SearchStaffCandidatesAsync(
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
            await invitationRepository.GetUserByIdAsync(
                request.UserId,
                cancellationToken
            )
            ?? throw new AppException(
                "User not found",
                ErrorCodes.UserNotFound,
                HttpStatusCode.NotFound
            );

        if (invitedUser.Status != UserStatus.Active ||
            invitedUser.CustomerStatus != CustomerStatus.Active)
        {
            throw new AppException(
                "This account does not have active customer access and cannot be invited as staff",
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
        if (invitedUser.Role == UserRole.StoreOwner)
        {
            throw AppException.BadRequest(
                "Store owner accounts cannot be invited as staff",
                ErrorCodes.InvalidStaffCandidate
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

        await using var transaction =
            await staffRepository.BeginTransactionAsync(
                cancellationToken
            );

        var pendingInvitation =
            await invitationRepository.GetPendingInvitationAsync(
                shop.Id,
                invitedUser.Id,
                cancellationToken
            );

        if (pendingInvitation != null)
        {
            if (!IsExpired(pendingInvitation))
            {
                throw new AppException(
                    "A pending invitation already exists for this user",
                    ErrorCodes.PendingInvitationExists,
                    HttpStatusCode.Conflict
                );
            }

            if (!await TransitionAndNotifyAsync(pendingInvitation.Id, StatusExpired, DateTime.UtcNow, cancellationToken))
                throw AppException.Conflict("Invitation changed. Please refresh and try again.");
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

        await invitationRepository.AddInvitationAsync(
            invitation,
            cancellationToken
        );

        await invitationRepository.SaveChangesAsync(
            cancellationToken
        );

        await notifications.NotifyAsync(invitation.Id, StaffInvitationStatus.Pending, cancellationToken);
        await transaction.CommitAsync(
            cancellationToken
        );

        var created =
            await invitationRepository.GetInvitationByIdAsync(
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

        await StaffRealtimeEvents.InvitationChangedAsync(realtime, created);
        scheduler.ScheduleExpiration(created.Id, created.InvitedAt.AddDays(InvitationLifetimeDays));

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
            await invitationRepository.GetInvitationByIdAsync(
                invitationId,
                cancellationToken
            );

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
            await TransitionAndNotifyAsync(invitation.Id, StatusExpired, DateTime.UtcNow, cancellationToken);
            throw AppException.BadRequest("Invitation has expired", ErrorCodes.InvitationInvalidOrExpired);
        }

        if (!await TransitionAndNotifyAsync(invitation.Id, StatusCancelled, DateTime.UtcNow, cancellationToken))
            throw AppException.Conflict("Invitation has already been answered or expired.");
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
            await invitationRepository.GetInvitationsByShopIdPagedAsync(
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

    public async Task<IEnumerable<StaffInvitationResponse>>
        GetCustomerInvitationsAsync(
            Guid currentUserId,
            CancellationToken cancellationToken = default)
    {
        var invitations = (await invitationRepository.GetInvitationsByUserIdAsync(
            currentUserId,
            cancellationToken
        )).ToList();

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
            await invitationRepository.GetInvitationByIdAsync(
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

        if (invitation.InvitedUser.Role == UserRole.StoreOwner)
        {
            throw AppException.BadRequest(
                "Store owner accounts cannot become shop staff",
                ErrorCodes.InvalidStaffCandidate
            );
        }

        if (IsExpired(invitation))
        {
            await TransitionAndNotifyAsync(invitation.Id, StatusExpired, DateTime.UtcNow, cancellationToken);
            throw AppException.BadRequest("Invitation has expired", ErrorCodes.InvitationInvalidOrExpired);
        }

        if (invitation.Status != StatusPending)
        {
            throw AppException.BadRequest(
                "Only pending invitations can be accepted",
                ErrorCodes.InvitationInvalidOrExpired
            );
        }

        if (invitation.Shop.Status != ShopStatus.Active &&
            invitation.Shop.Status != ShopStatus.Paused)
        {
            throw new AppException(
                "A suspended shop cannot receive a new staff membership",
                ErrorCodes.ShopSuspended,
                HttpStatusCode.Conflict
            );
        }

        var ownsApprovedShop =
            await staffRepository.UserOwnsApprovedShopAsync(
                currentUserId,
                cancellationToken
            );

        if (ownsApprovedShop)
        {
            throw AppException.BadRequest(
                "An account that owns an approved shop cannot have an active staff membership",
                ErrorCodes.InvalidStaffCandidate
            );
        }

        var hasActiveStaffMembership =
            await staffRepository.HasActiveStaffMembershipPlatformWideAsync(
                currentUserId,
                cancellationToken
            );

        if (hasActiveStaffMembership)
        {
            throw new AppException(
                "User already has an active staff membership platform-wide. You must end your current staff membership before accepting a new one.",
                ErrorCodes.AlreadyActiveStaff,
                HttpStatusCode.Conflict
            );
        }

        await using var transaction =
            await staffRepository.BeginTransactionAsync(
                cancellationToken
            );

        if (!await TransitionAndNotifyAsync(invitation.Id, StatusAccepted, DateTime.UtcNow, cancellationToken))
            throw AppException.Conflict("Invitation has already been answered or expired.");
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
            existingStaff.Status =
                ShopStaffStatus.Active;

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
                DisplayName =
                    invitation.InvitedUser.FullName,
                Nickname = null,
                Status = ShopStaffStatus.Active,
                JoinedAt = DateTime.UtcNow
            };

            await staffRepository.AddStaffAsync(
                newStaff,
                cancellationToken
            );
        }

        if (invitation.InvitedUser.Role ==
            UserRole.User)
        {
            invitation.InvitedUser.Role =
                UserRole.Staff;

            invitation.InvitedUser.UpdatedAt =
                DateTime.UtcNow;
            db.UserRoleChangeLogs.Add(new UserRoleChangeLog
            {
                Id = Guid.NewGuid(),
                UserId = currentUserId,
                PreviousRole = UserRole.User,
                NewRole = UserRole.Staff,
                Reason = $"Accepted staff invitation {invitation.Id} for shop {invitation.ShopId}.",
                CreatedAt = invitation.InvitedUser.UpdatedAt
            });
        }

        await invitationRepository.UpdateInvitationAsync(
            invitation,
            cancellationToken
        );

        await invitationRepository.SaveChangesAsync(
            cancellationToken
        );

        await staffRepository.SaveChangesAsync(
            cancellationToken
        );

        await TryNotifyAsync(invitation.Id, StaffInvitationStatus.Accepted, cancellationToken);
        await transaction.CommitAsync(
            cancellationToken
        );
        await StaffRealtimeEvents.InvitationChangedAsync(realtime, invitation, staffJoined: true);
        await realtime.SendToUserAsync(currentUserId, RealtimeEvents.UserRoleChanged, new
        {
            userId = currentUserId,
            shopId = invitation.ShopId,
            reason = "staff-invitation-accepted"
        });
    }


    public async Task DeclineInvitationAsync(
        Guid currentUserId,
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        var invitation =
            await invitationRepository.GetInvitationByIdAsync(
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

        if (IsExpired(invitation))
        {
            await TransitionAndNotifyAsync(invitation.Id, StatusExpired, DateTime.UtcNow, cancellationToken);
            throw AppException.BadRequest("Invitation has expired", ErrorCodes.InvitationInvalidOrExpired);
        }

        if (invitation.Status != StatusPending)
        {
            throw AppException.BadRequest(
                "Only pending invitations can be declined",
                ErrorCodes.InvitationInvalidOrExpired
            );
        }

        if (!await TransitionAndNotifyAsync(invitation.Id, StatusDeclined, DateTime.UtcNow, cancellationToken))
            throw AppException.Conflict("Invitation has already been answered or expired.");
    }


    private async Task<Shop> GetOwnerShopEntityAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken)
    {
        var shop =
            await invitationRepository
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
            ),
            invitation.Shop.LogoUrl,
            invitation.Shop.CoverImageUrl,
            string.Join(", ", new[] { invitation.Shop.AddressLine, invitation.Shop.Ward, invitation.Shop.District, invitation.Shop.City }
                .Where(value => !string.IsNullOrWhiteSpace(value)))
        );
    }
}
