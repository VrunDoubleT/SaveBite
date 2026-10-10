namespace SaveBite.Backend.Models.Enums;

public enum AccessScope
{
    Account = 1,
    Customer = 2,
    StoreOwner = 3,
    Admin = 4,
    StoreOwnerOrStaff = 5,
    Staff = 6,
    CustomerOrStaffOrStoreOwner = 7,
    CustomerView = 8,
    ShopView = 9,
    Guest = 10,
    StaffAnyStatus = 11,
    StoreOwnerAnyStatus = 12
}
