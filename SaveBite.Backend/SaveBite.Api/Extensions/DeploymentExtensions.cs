using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Npgsql;
using StackExchange.Redis;

namespace SaveBite.Backend.Extensions;

public static class DeploymentExtensions
{
    public static WebApplicationBuilder AddDeploymentConfiguration(this WebApplicationBuilder builder)
    {
        if (!builder.Configuration.GetValue<bool>("Deployment:Containerized")) return builder;
        var configuration = builder.Configuration;
        var singleContainer = configuration.GetValue<bool>("Deployment:SingleContainer");
        var postgres = new NpgsqlConnectionStringBuilder
        {
            Host = singleContainer ? "127.0.0.1" : "postgres", Port = 5432,
            Database = configuration["POSTGRES_DB"], Username = configuration["POSTGRES_USER"],
            Password = configuration["POSTGRES_PASSWORD"]
        };
        var redis = new ConfigurationOptions
        {
            Password = configuration["REDIS_PASSWORD"], AbortOnConnectFail = false
        };
        redis.EndPoints.Add(singleContainer ? "127.0.0.1" : "redis", 6379);
        // Provider builders correctly escape punctuation in credentials.
        configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = postgres.ConnectionString,
            ["ConnectionStrings:Redis"] = redis.ToString(includePassword: true)
        });
        return builder;
    }

    public static IServiceCollection AddDeployment(this IServiceCollection services, IConfiguration configuration)
    {
        var keyPath = configuration["Deployment:DataProtectionPath"];
        if (!string.IsNullOrWhiteSpace(keyPath))
            services.AddDataProtection().SetApplicationName("SaveBite")
                .PersistKeysToFileSystem(new DirectoryInfo(keyPath));

        if (configuration.GetValue<bool>("Deployment:BehindProxy"))
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                // Backend has no published port. The sole public ingress is Nginx.
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = 1;
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Postgres")!, name: "postgres")
            .AddRedis(configuration.GetConnectionString("Redis")!, name: "redis");
        return services;
    }
}
