using System.Text.Json;
using Microsoft.Extensions.Configuration;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;

namespace SaveBite.Tests;

public class PayOsWebhookConfigTests
{
    private static IConfiguration Config(string? backend = null) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> { ["API_PORT"] = "8080", ["Checkout:BackendBaseUrl"] = backend }).Build();

    [Fact] public void LocalWebhookUsesApiPort() => Assert.Equal(
        "http://localhost:8080/api/payments/payos/webhook", PayOsWebhookAddress.Resolve(Config()));

    [Fact] public void PublicUrlOverridesLocalPort() => Assert.Equal(
        "https://backend.example.com/api/payments/payos/webhook", PayOsWebhookAddress.Resolve(Config("https://backend.example.com/")));

    [Fact] public async Task ShopKeysRegisterWebhookButPlatformValidationDoesNot()
    {
        var api = new FakeApi();
        var validator = new PayOsCredentialsValidator(api, Config("https://backend.example.com"), TimeProvider.System);
        var credentials = new PlatformPayOsCredentials("client", "key", "checksum");
        await validator.ValidateAsync(credentials);
        Assert.Equal(0, api.Registrations);
        await validator.ValidateShopAsync(credentials);
        Assert.Equal(1, api.Registrations);
        Assert.Equal("https://backend.example.com/api/payments/payos/webhook", api.Webhook);
        await validator.ValidatePlatformAsync(credentials);
        Assert.Equal(2, api.Registrations);
        Assert.Equal("https://backend.example.com/api/platform-fees/payos/webhook", api.Webhook);
    }

    private sealed class FakeApi : IPayOsApiClient
    {
        public int Registrations;
        public string? Webhook;
        public Task<JsonElement> SendAsync(HttpMethod method, string path, PlatformPayOsCredentials credentials, object? body, CancellationToken ct)
        {
            var request = JsonSerializer.SerializeToElement(body);
            if (path == "confirm-webhook")
            {
                Registrations++; Webhook = request.GetProperty("webhookUrl").GetString();
                return Task.FromResult(JsonSerializer.SerializeToElement(new { webhookUrl = Webhook }));
            }
            if (path.EndsWith("/cancel")) return Task.FromResult(JsonSerializer.SerializeToElement(new { status = "CANCELLED" }));
            return Task.FromResult(JsonSerializer.SerializeToElement(new {
                orderCode = request.GetProperty("orderCode").GetInt64(), amount = request.GetProperty("amount").GetInt64() }));
        }
    }
}
