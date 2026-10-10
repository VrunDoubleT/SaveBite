namespace SaveBite.Backend.Authorization;

public static class AuthorizationPolicies
{
    // Guest, User (Customer), Staff, StoreOwner: excludes authenticated Admin accounts.
    // Anonymous requests are allowed; authenticated accounts must have UserStatus.Active.
    public const string Guest = "Guest";

    // User (Customer), Staff, StoreOwner: allows Active or Suspended CustomerStatus.
    // Requires UserStatus.Active; excludes Admin and does not check shop access.
    public const string CustomerAnyStatus = "CustomerAnyStatus";

    // User (Customer), Staff, StoreOwner: requires UserStatus.Active and CustomerStatus.Active.
    // Excludes Admin; shop suspension does not affect customer features.
    public const string CustomerActive = "CustomerActive";

    // Staff: allows Active or Suspended ShopStatus and related shop status.
    // Requires UserStatus.Active and an Active staff membership; excludes all other roles.
    public const string StaffAnyStatus = "StaffAnyStatus";

    // Staff: requires UserStatus.Active, ShopStatus.Active, and an Active membership.
    // The related shop must not be Suspended; excludes all other roles.
    public const string StaffActive = "StaffActive";

    // StoreOwner: allows Active or Suspended ShopStatus and related shop status.
    // Requires UserStatus.Active and shop ownership; excludes all other roles.
    public const string StoreOwnerAnyStatus = "StoreOwnerAnyStatus";

    // StoreOwner: requires UserStatus.Active, ShopStatus.Active, and shop ownership.
    // The related shop must not be Suspended; excludes all other roles.
    public const string StoreOwnerActive = "StoreOwnerActive";

    // Staff, StoreOwner: allows Active or Suspended ShopStatus and related shop status.
    // Requires UserStatus.Active and ownership or an Active staff membership; excludes User and Admin.
    public const string StoreOwnerOrStaffAnyStatus = "StoreOwnerOrStaffAnyStatus";

    // Staff, StoreOwner: requires UserStatus.Active and ShopStatus.Active.
    // Requires ownership or an Active membership at a shop that is not Suspended.
    public const string StoreOwnerOrStaffActive = "StoreOwnerOrStaffActive";

    // Admin only: requires UserStatus.Active and the Admin role in both the DB and JWT.
    public const string Admin = "Admin";

    // Infrastructure only: any authenticated role with a valid JWT; no DB status check.
    // Do not use this policy to authorize customer, shop, or admin business features.
    public const string JwtOnly = "JwtOnly";

    // Infrastructure only: User, Staff, StoreOwner, Admin with UserStatus.Active.
    // This remains the default for compatibility; select an explicit actor policy for business actions.
    public const string AccountOnly = "AccountOnly";

    // Legacy alias for CustomerAnyStatus: User, Staff, StoreOwner with UserStatus.Active.
    public const string CustomerOrStaffOrStoreOwner = "CustomerOrStaffOrStoreOwner";

    // Legacy alias for CustomerAnyStatus; this policy does not restrict HTTP methods.
    public const string CustomerView = "CustomerView";

    // Legacy alias for StoreOwnerOrStaffAnyStatus; this policy does not restrict HTTP methods.
    public const string ShopView = "ShopView";

    // Legacy alias for CustomerActive: User, Staff, StoreOwner with active customer access.
    public const string Customer = "Customer";

    // Legacy alias for StaffActive: Staff with active shop access and membership.
    public const string Staff = "Staff";

    // Legacy alias for StoreOwnerActive: StoreOwner with active shop access and ownership.
    public const string StoreOwner = "StoreOwner";

    // Legacy alias for StoreOwnerOrStaffActive: Staff, StoreOwner with active shop access.
    public const string StoreOwnerOrStaff = "StoreOwnerOrStaff";
}
