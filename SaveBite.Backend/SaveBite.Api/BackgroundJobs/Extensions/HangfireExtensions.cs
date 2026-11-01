using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Options;
using SaveBite.Backend.BackgroundJobs.Configuration;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.BackgroundJobs.Jobs;
using SaveBite.Backend.BackgroundJobs.Scheduling;
using SaveBite.Backend.BackgroundJobs.Service;

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
        services.AddScoped<IOrderEventDispatchScheduler, OrderEventDispatchScheduler>();
        services.AddScoped<IShopOrderConfirmationScheduler, ShopOrderConfirmationScheduler>();
        services.AddScoped<IShopOrderNoShowScheduler, ShopOrderNoShowScheduler>();
        // Register scoped job classes here as they are added.
        services.AddScoped<IFlashDealJobService, FlashDealJobService>();

        services.AddScoped<CheckoutPaymentExpirationJob>();
        services.AddScoped<OrderEventDispatchJob>();
        services.AddScoped<ShopOrderConfirmationJob>();
        services.AddScoped<ShopOrderNoShowJob>();
        services.AddScoped<PickupDeadlineReminderJob>();
        services.AddScoped<PickupReminderRecoveryJob>();
        services.AddScoped<PlatformFeeOverdueJob>();
        services.AddScoped<PlatformFeeMonthlyStatementJob>();

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
        app.Services.GetRequiredService<IBackgroundJobClient>().Enqueue<RefundDeadlineRecoveryJob>(job => job.ExecuteAsync(CancellationToken.None));
        app.Services.GetRequiredService<IBackgroundJobClient>().Enqueue<StaffInvitationRecoveryJob>(job => job.ExecuteAsync(true, CancellationToken.None));
        app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate<StaffInvitationRecoveryJob>(
            "staff-invitation-recovery", job => job.ExecuteAsync(false, CancellationToken.None), Cron.Minutely());
        app.Services.GetRequiredService<IRecurringJobManager>().AddOrUpdate<PickupReminderRecoveryJob>(
            "pickup-deadline-reminder-recovery", job => job.ExecuteAsync(CancellationToken.None), Cron.Minutely());
        var options = app.Services.GetRequiredService<IOptions<HangfireOptions>>().Value;
        if (app.Environment.IsDevelopment() && options.DashboardEnabled)
        {
            // Hangfire's default dashboard authorization only permits local requests.
            app.UseHangfireDashboard("/hangfire");
        }
        using (var scope = app.Services.CreateScope())
               {
                   var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
                   // Monthly calculation: Runs on the 1st of every month at midnight (12:00 AM / 00:00) to calculate statements for previous month (BR-093, BR-192)
                   recurringJobs.AddOrUpdate<PlatformFeeMonthlyStatementJob>(
                       "platform-fee-monthly-statement-generation",
                       job => job.ExecuteAsync(CancellationToken.None),
                       Cron.Monthly(1, 0)); // 00:00 on day-of-month 1

                   // Check hourly for fees due within 24 hours and suspend overdue shops (BR-095, BR-193).
                   recurringJobs.AddOrUpdate<PlatformFeeOverdueJob>(
                       "platform-fee-overdue-check",
                       job => job.ExecuteAsync(CancellationToken.None),
                       Cron.Hourly()); // Check overdue fees and the 24-hour reminder window every hour
               }


        // Stop the legacy notification recovery schedule; notifications now publish directly to RabbitMQ.
        app.Services.GetRequiredService<IRecurringJobManager>().RemoveIfExists("notification-outbox-recovery");
        return app;
    }
}
