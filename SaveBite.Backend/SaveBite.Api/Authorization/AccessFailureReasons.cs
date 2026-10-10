namespace SaveBite.Backend.Authorization;

public static class AccessFailureReasons
{
    public const string AccountInactive = "ACCOUNT_INACTIVE";
    public const string CustomerSuspended = "CUSTOMER_SUSPENDED";
    public const string CustomerRequired = "CUSTOMER_REQUIRED";
    public const string CustomerOrStaffOrStoreOwnerRequired = "CUSTOMER_OR_STAFF_OR_STORE_OWNER_REQUIRED";
    public const string ShopSuspended = "SHOP_SUSPENDED";
    public const string StaffRequired = "STAFF_REQUIRED";
    public const string StoreOwnerRequired = "STORE_OWNER_REQUIRED";
    public const string StoreOwnerOrStaffRequired = "STORE_OWNER_OR_STAFF_REQUIRED";
    public const string AdminRequired = "ADMIN_REQUIRED";
}
