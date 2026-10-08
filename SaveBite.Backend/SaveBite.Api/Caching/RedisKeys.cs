namespace SaveBite.Backend.Caching;

public static class RedisKeys
{
    public static string AuthPendingRegistration(string subjectHash)
        => $"auth:register:pending:{subjectHash}";

    public static string AuthPendingPasswordReset(string subjectHash)
        => $"auth:password-reset:pending:{subjectHash}";

    public static string AuthOtp(string purpose, string subjectHash)
        => $"auth:otp:{purpose}:{subjectHash}";

    public static string AuthOtpAttempts(string purpose, string subjectHash)
        => $"auth:otp:attempts:{purpose}:{subjectHash}";

    public static string AuthOtpCooldown(string purpose, string subjectHash)
        => $"auth:otp:cooldown:{purpose}:{subjectHash}";

    public static string AuthOtpDailyEmail(string purpose, string subjectHash)
        => $"auth:otp:daily:email:{purpose}:{subjectHash}";

    public static string AuthLoginFailuresByEmail(string subjectHash)
        => $"auth:login:fail:email:{subjectHash}";

    public static string Deal(Guid dealId)
        => $"savebite:deal:{dealId}";
    
    public static string DealVariants(Guid dealId)
        => $"savebite:deal:{dealId}:variants";
    
    public static string DealStock(Guid dealId)
        => $"savebite:deal:{dealId}:stock";
    
    public static string ShopDeals(Guid shopId)
        => $"savebite:shop:{shopId}:deals";
    
    public const string GeoActiveShops = "savebite:geo:active-shops";
    
    public static string FlashDealDetail(Guid dealId) => Deal(dealId);
    public static string FlashDealShopDeals(Guid shopId) => ShopDeals(shopId);
    public const string FlashDealGeoShops = GeoActiveShops;
    public const string FlashDealActiveDeals = "savebite:deals:active";

}
