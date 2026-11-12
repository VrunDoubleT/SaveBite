namespace SaveBite.Backend.Configurations;

/// <summary>
/// Shared backend timing policy. Units are explicit in each name; cron schedules use UTC.
/// Changes require a rebuild/restart and apply to newly calculated deadlines.
/// Persisted deadlines and historical migrations deliberately keep their original values.
/// Hangfire worker/retry overrides remain in the existing Hangfire appsettings section.
/// </summary>
public static class TimingConfiguration
{
    public static class Jobs
    {
        public const int DefaultWorkerCount = 5;
        public const int DefaultRetryAttempts = 3;
        public const int ConcurrencyLockTimeoutSeconds = 600;
        public const int RecoveryBatchSize = 100;
        public const string OrderDeadlineRecoveryCron = "* * * * *";
        public const string OutboxRecoveryCron = "* * * * *";
        public const string RabbitFailureRecoveryCron = "*/5 * * * *";
        public const string CartRecoveryCron = "* * * * *";
        public const string RefundRecoveryCron = "* * * * *";
        public const string FlashDealRecoveryCron = "* * * * *";
        public const string InvitationRecoveryCron = "* * * * *";
        public const string UploadCleanupCron = "* * * * *";
        public const string PlatformFeeRecoveryCron = "0 * * * *";
        public const string PlatformFeeOverdueCron = "0 * * * *";
        public const string MonthlyBillingCron = "0 0 1 * *"; // Day 1, 00:00 UTC / 07:00 GMT+7.
        public const string AuditArchiveCron = MonthlyBillingCron;
        public const int RabbitRetryBaseMinutes = 5;
        public const int RabbitRetryMaxMinutes = 60;
        public const int RabbitRetryMaxExponent = 4;
    }

    public static class EmailReminders
    {
        public const string ScanCron = "*/5 * * * *";
        public const int CustomerDetailsFirstHours = 12;
        public const int CustomerDetailsFinalLeadHours = 6;
        public const int ShopTransferFirstLeadHours = 24;
        public const int ShopTransferFinalLeadHours = 2;
        public const int InvitationLeadHours = 24;
    }

    public static class Refund
    {
        public const int CustomerDetailsHours = 24;
        public const int ShopProcessingHours = 72;
        public const int CustomerRequestWindowHours = 6;
        public const int EmailCodeLifetimeMinutes = 15;
        public const int EmailCodeResendMinutes = 1;
        public const int TransferClockSkewSeconds = 30;
    }

    public static class Orders
    {
        public const int ShopConfirmationMinutes = 5;
        public const int ResaleDecisionMinutes = 5;
        public const int PaymentExpirationSeconds = 120;
        public const int QuoteLifetimeSeconds = 60;
        public const int LocationMaxAgeMinutes = 2;
        public const int LocationClockSkewSeconds = 30;
        public const int MinimumPickupEtaMinutes = 5;
        public const int MinimumPickupBufferMinutes = 10;
        public const decimal PickupBufferRatio = 0.30m;
        public const int PickupReminderLeadMinutes = 10;
    }

    public static class FlashDeals
    {
        public const int MinimumDurationMinutes = 30;
        public const int MaximumDurationMinutes = 120;
        public const int MinimumScheduleAheadMinutes = 15;
        public const int MaximumScheduleAheadMinutes = 90;
        public const int ImmediateStartToleranceMinutes = 5;
        public const int LegacyRequestToleranceMinutes = 1;
    }

    public static class PlatformFees
    {
        public const int PaymentLinkGraceDays = 1;
        public const int StatementPaymentDays = 7;
        public const int DueReminderLeadHours = 24;
    }

    public static class Uploads
    {
        public const int SessionLifetimeHours = 24;
    }

    public static class StaffInvitations
    {
        public const int LifetimeDays = 3;
    }
}
