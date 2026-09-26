using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class UserAccessService : IUserAccessService
{
    private readonly IUserAccessRepository _userAccessRepository;

    public UserAccessService(IUserAccessRepository userAccessRepository)
    {
        _userAccessRepository = userAccessRepository;
    }

    public async Task<UserAccessDecision> AuthorizeAsync(
        Guid userId,
        AccessScope scope,
        bool hasAdminRoleClaim,
        CancellationToken cancellationToken = default)
    {
        var state = await _userAccessRepository.GetAccessStateAsync(
            userId,
            cancellationToken);

        if (state is null || state.Status != UserStatus.Active)
            return UserAccessDecision.Deny(
                AccessDenialReason.AccountInactive);

        return scope switch
        {
            AccessScope.Account => UserAccessDecision.Allow(),
            AccessScope.Customer => AuthorizeCustomer(state),
            AccessScope.Staff => await AuthorizeStaffAsync(
                userId,
                state,
                cancellationToken),
            AccessScope.StoreOwner => await AuthorizeStoreOwnerAsync(
                userId,
                state,
                cancellationToken),
            AccessScope.StoreOwnerOrStaff => await AuthorizeStoreOwnerOrStaffAsync(
                userId,
                state,
                cancellationToken),
            AccessScope.Admin =>
                state.Role == UserRole.Admin && hasAdminRoleClaim
                    ? UserAccessDecision.Allow()
                    : UserAccessDecision.Deny(
                        AccessDenialReason.AdminRequired),
            _ => UserAccessDecision.Deny(
                AccessDenialReason.AccountInactive)
        };
    }

    private static UserAccessDecision AuthorizeCustomer(UserAccessState state)
    {
        if (state.Role is not (UserRole.User or UserRole.Staff or UserRole.StoreOwner))
            return UserAccessDecision.Deny(AccessDenialReason.CustomerRequired);

        return state.CustomerStatus == CustomerStatus.Active
            ? UserAccessDecision.Allow()
            : UserAccessDecision.Deny(AccessDenialReason.CustomerSuspended);
    }

    private async Task<UserAccessDecision> AuthorizeStaffAsync(
        Guid userId,
        UserAccessState state,
        CancellationToken cancellationToken)
    {
        if (state.Role != UserRole.Staff)
            return UserAccessDecision.Deny(AccessDenialReason.StaffRequired);

        if (state.ShopStatus != ShopAccessStatus.Active)
            return UserAccessDecision.Deny(AccessDenialReason.ShopSuspended);

        return await _userAccessRepository.HasStaffAccessAsync(
            userId,
            cancellationToken)
            ? UserAccessDecision.Allow()
            : UserAccessDecision.Deny(AccessDenialReason.StaffRequired);
    }

    private async Task<UserAccessDecision> AuthorizeStoreOwnerAsync(
        Guid userId,
        UserAccessState state,
        CancellationToken cancellationToken)
    {
        if (state.Role != UserRole.StoreOwner)
            return UserAccessDecision.Deny(AccessDenialReason.StoreOwnerRequired);

        if (state.ShopStatus != ShopAccessStatus.Active)
            return UserAccessDecision.Deny(AccessDenialReason.ShopSuspended);

        return await _userAccessRepository.HasStoreOwnerAccessAsync(
            userId,
            cancellationToken)
            ? UserAccessDecision.Allow()
            : UserAccessDecision.Deny(
                AccessDenialReason.StoreOwnerRequired);
    }

    private async Task<UserAccessDecision> AuthorizeStoreOwnerOrStaffAsync(
        Guid userId,
        UserAccessState state,
        CancellationToken cancellationToken)
    {
        if (state.Role is not (UserRole.StoreOwner or UserRole.Staff))
        {
            return UserAccessDecision.Deny(
                AccessDenialReason.StoreOwnerOrStaffRequired);
        }

        if (state.ShopStatus != ShopAccessStatus.Active)
            return UserAccessDecision.Deny(AccessDenialReason.ShopSuspended);

        return await _userAccessRepository.HasStoreOwnerOrStaffAccessAsync(
            userId,
            cancellationToken)
            ? UserAccessDecision.Allow()
            : UserAccessDecision.Deny(
                AccessDenialReason.StoreOwnerOrStaffRequired);
    }
}
