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
    public static string FlashDealDetail(Guid dealId)
        => $"flashdeal:detail:{dealId}";
    
    public static string FlashDealShopDeals(Guid dealId) 
        => $"flashdeal:shop:{dealId}";
    public const string FlashDealGeoShops = "flashdeal:geoshops";
    public const string FlashDealActiveDeals = "flashdeal:activedeals";

}
