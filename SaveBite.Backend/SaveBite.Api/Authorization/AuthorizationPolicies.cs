namespace SaveBite.Backend.Authorization;

public static class AuthorizationPolicies
{
    public const string JwtOnly = "JwtOnly";
    public const string AccountOnly = "AccountOnly";
    public const string Customer = "Customer";
    public const string Staff = "Staff";
    public const string StoreOwner = "StoreOwner";
    public const string StoreOwnerOrStaff = "StoreOwnerOrStaff";
    public const string Admin = "Admin";
}
