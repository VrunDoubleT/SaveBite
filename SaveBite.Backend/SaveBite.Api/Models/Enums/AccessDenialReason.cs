namespace SaveBite.Backend.Models.Enums;

public enum AccessDenialReason
{
    AccountInactive = 1,
    CustomerSuspended = 2,
    ShopSuspended = 3,
    AdminRequired = 4,
    StoreOwnerRequired = 5,
    StoreOwnerOrStaffRequired = 6,
    StaffRequired = 7,
    CustomerRequired = 8
}
