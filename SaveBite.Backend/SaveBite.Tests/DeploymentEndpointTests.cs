using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SaveBite.Backend.Extensions;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Services.Implementations;
using StackExchange.Redis;
using Xunit;

namespace SaveBite.Tests;

public sealed class DeploymentEndpointTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), "savebite-endpoint-" + Guid.NewGuid() + ".json");

    private IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Deployment:PublicEndpointFile"] = _path,
        ["Checkout:BackendBaseUrl"] = "https://stale-shop.trycloudflare.com"
    }).Build();

    private void Write(string url, bool ready = true, int age = 0) => File.WriteAllText(_path, JsonSerializer.Serialize(new
    {
        Url = url, Ready = ready, UpdatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - age
    }));

    [Fact]
    public void MissingOrCorruptSnapshotNeverFallsBackToOldUrl()
    {
        Assert.Throws<AppException>(() => PayOsWebhookAddress.Resolve(Configuration()));
        File.WriteAllText(_path, "{broken");
        Assert.Throws<AppException>(() => PayOsWebhookAddress.Resolve(Configuration()));
    }

    [Fact]
    public void UrlChangeIsUsedForBothWebhookTypesWithoutRestart()
    {
        var configuration = Configuration();
        Write("https://first-shop.trycloudflare.com");
        Assert.Equal("https://first-shop.trycloudflare.com/api/payments/payos/webhook", PayOsWebhookAddress.Resolve(configuration));
        Write("https://second-shop.trycloudflare.com");
        Assert.Equal("https://second-shop.trycloudflare.com/api/platform-fees/payos/webhook", PayOsWebhookAddress.Resolve(configuration, true));
        Assert.Equal("https://second-shop.trycloudflare.com", DeploymentEndpoint.Resolve(configuration, "Checkout:FrontendBaseUrl"));
    }

    [Theory]
    [InlineData("https://valid-shop.trycloudflare.com", false, 0)]
    [InlineData("https://valid-shop.trycloudflare.com", true, 60)]
    [InlineData("https://valid-shop.trycloudflare.com", true, -60)]
    [InlineData("http://valid-shop.trycloudflare.com", true, 0)]
    [InlineData("https://valid-shop.trycloudflare.com.evil.test", true, 0)]
    [InlineData("https://user@valid-shop.trycloudflare.com", true, 0)]
    [InlineData("https://valid-shop.trycloudflare.com/path", true, 0)]
    public void RejectsUnavailableOrInvalidEndpoints(string url, bool ready, int age)
    {
        Write(url, ready, age);
        Assert.Throws<AppException>(() => PayOsWebhookAddress.Resolve(Configuration()));
    }

    [Fact]
    public void PreparationModeRejectsNewPayments()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Deployment:PaymentCreationEnabled"] = "false"
        }).Build();
        Assert.Throws<AppException>(() => DeploymentEndpoint.EnsurePaymentCreationEnabled(configuration));
        DeploymentEndpoint.EnsurePaymentCreationEnabled(new ConfigurationBuilder().Build());
    }

    [Fact]
    public void ContainerConnectionsUseServiceNamesAndPreserveCredentialPunctuation()
    {
        const string password = "quote\";dollar$hash#secret";
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Deployment:Containerized"] = "true", ["POSTGRES_DB"] = "savebite",
            ["POSTGRES_USER"] = "app", ["POSTGRES_PASSWORD"] = password, ["REDIS_PASSWORD"] = password
        });
        builder.AddDeploymentConfiguration();
        var postgres = new NpgsqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Postgres"));
        Assert.Equal("postgres", postgres.Host);
        Assert.Equal(password, postgres.Password);
        var redis = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("Redis")!);
        Assert.Equal("redis", Assert.IsType<System.Net.DnsEndPoint>(Assert.Single(redis.EndPoints)).Host);
        Assert.Equal(password, redis.Password);
        Assert.False(redis.AbortOnConnectFail);
    }

    [Fact]
    public void SingleContainerConnectionsUseLoopback()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Deployment:Containerized"] = "true", ["Deployment:SingleContainer"] = "true",
            ["POSTGRES_DB"] = "savebite", ["POSTGRES_USER"] = "app",
            ["POSTGRES_PASSWORD"] = "password", ["REDIS_PASSWORD"] = "password"
        });
        builder.AddDeploymentConfiguration();
        var postgres = new NpgsqlConnectionStringBuilder(builder.Configuration.GetConnectionString("Postgres"));
        Assert.Equal("127.0.0.1", postgres.Host);
        var redis = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("Redis")!);
        Assert.Equal("127.0.0.1:6379", redis.EndPoints.Single().ToString());
    }

    [Fact]
    public void LocalConnectionSettingsArePreserved()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Deployment:Containerized"] = "false", ["ConnectionStrings:Postgres"] = "Host=localhost;Database=local"
        });
        builder.AddDeploymentConfiguration();
        Assert.Equal("Host=localhost;Database=local", builder.Configuration.GetConnectionString("Postgres"));
    }

    public void Dispose() => File.Delete(_path);
}
