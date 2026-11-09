namespace SaveBite.Backend.Caching;

public static class RedisKeys
{
    public static string OrderCode(Guid shopId, DateOnly date) => $"order-code:{shopId:N}:{date:yyyyMMdd}";

    public static string CheckoutQuote(Guid userId, Guid quoteId)
        => $"checkout:quote:{userId:N}:{quoteId:N}";

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

    // Location & Geo Keys (Section 3.2 of Geo Solution)
    public const string UsersGpsGeo = "savebite:geo:users:gps";
    public const string UsersDefaultGeo = "savebite:geo:users:default";

    public static string UserGpsMetadata(Guid userId) => $"savebite:user:{userId:N}:gps";
    public static string UserDefaultAddressVersion(Guid userId) => $"savebite:user:{userId:N}:default-version";

    public static string PublicationGpsTemp(Guid publicationId) => $"savebite:tmp:publication:{publicationId:N}:gps";
    public static string PublicationDefaultTemp(Guid publicationId) => $"savebite:tmp:publication:{publicationId:N}:default";
}
