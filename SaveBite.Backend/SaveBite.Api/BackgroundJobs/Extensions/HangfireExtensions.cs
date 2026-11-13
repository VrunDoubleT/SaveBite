using SaveBite.Backend.Configurations;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Options;
using SaveBite.Backend.BackgroundJobs.Configuration;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.BackgroundJobs.Jobs;
using SaveBite.Backend.BackgroundJobs.Scheduling;
using SaveBite.Backend.BackgroundJobs.Service;
using SaveBite.Backend.Uploads;

namespace SaveBite.Backend.BackgroundJobs.Extensions;

public static class HangfireExtensions
{
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IStaffInvitationScheduler, StaffInvitationScheduler>();
        services.AddScoped<StaffInvitationJob>();
        services.AddScoped<StaffInvitationRecoveryJob>();
        services.AddScoped<IRefundDeadlineScheduler, RefundDeadlineScheduler>();
        services.AddScoped<RefundDeadlineJob>();
        services.AddScoped<RefundDeadlineRecoveryJob>();
        services.AddScoped<ICheckoutPaymentExpirationScheduler, CheckoutPaymentExpirationScheduler>();
        services.AddScoped<IShopOrderConfirmationScheduler, ShopOrderConfirmationScheduler>();
        services.AddScoped<IShopOrderNoShowScheduler, ShopOrderNoShowScheduler>();
        // Register scoped job classes here as they are added.
        services.AddScoped<IFlashDealJobService, FlashDealJobService>();

        services.AddScoped<CheckoutPaymentExpirationJob>();
        services.AddScoped<OrderEventDispatchJob>();
        services.AddScoped<ShopOrderConfirmationJob>();
        services.AddScoped<ShopOrderNoShowJob>();
        services.AddScoped<PickupDeadlineReminderJob>();
        services.AddScoped<ResaleDecisionExpirationJob>();
        services.AddScoped<OrderDeadlineRecoveryJob>();
        services.AddScoped<OrderOutboxRecoveryJob>();
        services.AddScoped<RabbitFailureRecoveryJob>();
        services.AddScoped<CartAvailabilityRecoveryJob>();
        services.AddScoped<FlashDealRecoveryJob>();
        services.AddScoped<PlatformFeeRecoveryJob>();
        services.AddScoped<PlatformFeeScheduleActivationJob>();
        services.AddScoped<PlatformFeeOverdueJob>();
        services.AddScoped<PlatformFeeMonthlyStatementJob>();
        services.AddScoped<UploadCleanupJob>();

        services.AddOptions<HangfireOptions>()
            .Bind(configuration.GetSection(HangfireOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var connectionString = configuration.GetConnectionString("Hangfire");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            connectionString = configuration.GetConnectionString("Postgres");
        }

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Hangfire requires ConnectionStrings:Hangfire or ConnectionStrings:Postgres.");
        }

        services.AddHangfire((provider, config) =>
        {
            var options = provider.GetRequiredService<IOptions<HangfireOptions>>().Value;
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(storage => storage.UseNpgsqlConnection(connectionString),
                    new PostgreSqlStorageOptions
                    {
                        SchemaName = "hangfire",
                        PrepareSchemaIfNecessary = true
                    })
                .UseFilter(new AutomaticRetryAttribute { Attempts = options.RetryAttempts });
        });

        services.AddHangfireServer((provider, options) =>
        {
            options.WorkerCount = provider.GetRequiredService<IOptions<HangfireOptions>>().Value.WorkerCount;
            options.Queues = new[] { "default" };
        });

        return services;
    }

    public static WebApplication UseBackgroundJobs(this WebApplication app)
    {
        var recurring = app.Services.GetRequiredService<IRecurringJobManager>();
        recurring.AddOrUpdate<OrderDeadlineRecoveryJob>("order-deadline-recovery", job => job.ExecuteAsync(CancellationToken.None), TimingConfiguration.Jobs.OrderDeadlineRecoveryCron);
        recurring.AddOrUpdate<OrderOutboxRecoveryJob>("order-outbox-dispatch", job => job.ExecuteAsync(CancellationToken.None), TimingConfiguration.Jobs.OutboxRecoveryCron);
        recurring.AddOrUpdate<RabbitFailureRecoveryJob>("rabbit-failure-recovery", job => job.ExecuteAsync(CancellationToken.None), TimingConfiguration.Jobs.RabbitFailureRecoveryCron);
        recurring.AddOrUpdate<CartAvailabilityRecoveryJob>("cart-availability-recovery", job => job.ExecuteAsync(CancellationToken.None), TimingConfiguration.Jobs.CartRecoveryCron);
        recurring.AddOrUpdate<RefundDeadlineRecoveryJob>("refund-deadline-recovery", job => job.ExecuteAsync(CancellationToken.None), TimingConfiguration.Jobs.RefundRecoveryCron);
        recurring.AddOrUpdate<FlashDealRecoveryJob>("flash-deal-recovery", job => job.ExecuteAsync(CancellationToken.None), TimingConfiguration.Jobs.FlashDealRecoveryCron);
        recurring.AddOrUpdate<StaffInvitationRecoveryJob>("staff-invitation-recovery", job => job.ExecuteAsync(false, CancellationToken.None), TimingConfiguration.Jobs.InvitationRecoveryCron);
        recurring.AddOrUpdate<PlatformFeeScheduleActivationJob>("platform-fee-schedule-activation",
            job => job.ExecuteAsync(CancellationToken.None), "* * * * *");
        recurring.AddOrUpdate<PlatformFeeRecoveryJob>("platform-fee-recovery", job => job.ExecuteAsync(CancellationToken.None), TimingConfiguration.Jobs.PlatformFeeRecoveryCron);
        recurring.AddOrUpdate<UploadCleanupJob>("temporary-upload-cleanup", job => job.ExecuteAsync(CancellationToken.None), TimingConfiguration.Jobs.UploadCleanupCron);
        recurring.RemoveIfExists("pickup-deadline-reminder-recovery");
        var jobs = app.Services.GetRequiredService<IBackgroundJobClient>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("BackgroundJobStartup");
        SafeSchedule.Try(() => jobs.Enqueue<RefundDeadlineRecoveryJob>(job => job.ExecuteAsync(CancellationToken.None)), logger, "refund-startup", Guid.Empty);
        SafeSchedule.Try(() => jobs.Enqueue<StaffInvitationRecoveryJob>(job => job.ExecuteAsync(true, CancellationToken.None)), logger, "invitation-startup", Guid.Empty);
        SafeSchedule.Try(() => jobs.Enqueue<OrderDeadlineRecoveryJob>(job => job.ExecuteAsync(CancellationToken.None)), logger, "order-startup", Guid.Empty);
        SafeSchedule.Try(() => jobs.Enqueue<FlashDealRecoveryJob>(job => job.ExecuteAsync(CancellationToken.None)), logger, "flash-startup", Guid.Empty);
        SafeSchedule.Try(() => jobs.Enqueue<OrderOutboxRecoveryJob>(job => job.ExecuteAsync(CancellationToken.None)), logger, "outbox-startup", Guid.Empty);
        SafeSchedule.Try(() => jobs.Enqueue<PlatformFeeScheduleActivationJob>(job => job.ExecuteAsync(CancellationToken.None)), logger, "fee-schedule-startup", Guid.Empty);
        SafeSchedule.Try(() => jobs.Enqueue<PlatformFeeRecoveryJob>(job => job.ExecuteAsync(CancellationToken.None)), logger, "fee-startup", Guid.Empty);
        var options = app.Services.GetRequiredService<IOptions<HangfireOptions>>().Value;
        if (app.Environment.IsDevelopment() && options.DashboardEnabled)
        {
            // Hangfire's default dashboard authorization only permits local requests.
            app.UseHangfireDashboard("/hangfire");
        }
        var monthlyBillingCron = TimingConfiguration.Jobs.MonthlyBillingCron; // 00:00 UTC = 07:00 Vietnam, day 1.
        recurring.AddOrUpdate<PlatformFeeMonthlyStatementJob>("platform-fee-monthly-statement-generation",
            job => job.ExecuteAsync(CancellationToken.None), monthlyBillingCron); // UTC: 07:00 Vietnam
        recurring.AddOrUpdate<SaveBite.Backend.Audit.Archives.AuditArchiveService>("audit-log-monthly-archive",
            job => job.ExecuteAsync(CancellationToken.None), TimingConfiguration.Jobs.AuditArchiveCron); // Same UTC schedule as platform fees.
        recurring.AddOrUpdate<PlatformFeeOverdueJob>("platform-fee-overdue-check",
            job => job.ExecuteAsync(CancellationToken.None), TimingConfiguration.Jobs.PlatformFeeOverdueCron);

        // Shared event_outbox recovery now dispatches notifications as well.
        app.Services.GetRequiredService<IRecurringJobManager>().RemoveIfExists("notification-outbox-recovery");
        return app;
    }
}