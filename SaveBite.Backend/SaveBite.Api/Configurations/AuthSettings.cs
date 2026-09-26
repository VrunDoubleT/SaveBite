namespace SaveBite.Backend.Configurations;

public sealed class AuthSettings
{
    public const string SectionName = "Auth";

    public int OtpLifetimeMinutes { get; set; } = 15;
    public int OtpResendCooldownSeconds { get; set; } = 60;
    public int MaxOtpRequestsPerEmailPerDay { get; set; } = 5;
    public int MaxOtpVerificationAttempts { get; set; } = 5;
    public int LoginAttemptWindowMinutes { get; set; } = 15;
    public int MaxLoginAttemptsPerEmail { get; set; } = 5;
}
