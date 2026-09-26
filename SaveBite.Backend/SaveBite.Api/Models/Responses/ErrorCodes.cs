namespace SaveBite.Backend.Models.Responses;

public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string AccountInactive = "ACCOUNT_INACTIVE";
    public const string CustomerSuspended = "CUSTOMER_SUSPENDED";
    public const string CustomerRequired = "CUSTOMER_REQUIRED";
    public const string ShopSuspended = "SHOP_SUSPENDED";
    public const string StaffRequired = "STAFF_REQUIRED";
    public const string StoreOwnerRequired = "STORE_OWNER_REQUIRED";
    public const string StoreOwnerOrStaffRequired = "STORE_OWNER_OR_STAFF_REQUIRED";
    public const string AdminRequired = "ADMIN_REQUIRED";
    public const string Conflict = "CONFLICT";
    public const string TooManyRequests = "TOO_MANY_REQUESTS";
    public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    public const string InvalidOtp = "INVALID_OTP";
    public const string InternalError = "INTERNAL_ERROR";

    // Domain-specific SaveBite errors
    public const string FoodExpired = "FOOD_EXPIRED";
    public const string OutOfStock = "OUT_OF_STOCK";
    public const string OrderClosed = "ORDER_CLOSED";
}
