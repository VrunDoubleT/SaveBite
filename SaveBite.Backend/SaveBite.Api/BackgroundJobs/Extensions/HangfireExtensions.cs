using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.Options;
using SaveBite.Backend.BackgroundJobs.Configuration;
using SaveBite.Backend.BackgroundJobs.Interfaces;
using SaveBite.Backend.BackgroundJobs.Jobs;
using SaveBite.Backend.BackgroundJobs.Scheduling;

namespace SaveBite.Backend.BackgroundJobs.Extensions;

public static class HangfireExtensions
{
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<ICheckoutPaymentExpirationScheduler, CheckoutPaymentExpirationScheduler>();
        services.AddScoped<IOrderEventDispatchScheduler, OrderEventDispatchScheduler>();
        services.AddScoped<IShopOrderConfirmationScheduler, ShopOrderConfirmationScheduler>();
        services.AddScoped<IShopOrderNoShowScheduler, ShopOrderNoShowScheduler>();

        services.AddScoped<CheckoutPaymentExpirationJob>();
        services.AddScoped<OrderEventDispatchJob>();
        services.AddScoped<ShopOrderConfirmationJob>();
        services.AddScoped<ShopOrderNoShowJob>();

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
        var options = app.Services.GetRequiredService<IOptions<HangfireOptions>>().Value;
        if (app.Environment.IsDevelopment() && options.DashboardEnabled)
        {
            // Hangfire's default dashboard authorization only permits local requests.
            app.UseHangfireDashboard("/hangfire");
        }

        return app;
    }
}
