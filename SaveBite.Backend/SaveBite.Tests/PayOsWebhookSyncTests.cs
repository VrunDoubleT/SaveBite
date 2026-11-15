using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Payments.Webhooks;
using SaveBite.Backend.Services.Interfaces;
using Xunit;

namespace SaveBite.Tests;

public sealed class WebhookPostgresFactAttribute : FactAttribute
{
    public WebhookPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SAVEBITE_WEBHOOK_TEST_POSTGRES")))
            Skip = "Set SAVEBITE_WEBHOOK_TEST_POSTGRES to an isolated PostgreSQL test database.";
    }
}

public sealed class PayOsWebhookSyncTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "savebite-sync-" + Guid.NewGuid().ToString("N"));
    private readonly Sources _sources = new();
    private readonly Api _api = new();
    private readonly Validator _validator = new();
    private IConfiguration _configuration = null!;
    private string StatePath => Path.Combine(_directory, "endpoint.json");

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        _configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Deployment:PublicEndpointFile"] = StatePath }).Build();
        Endpoint();
        return Task.CompletedTask;
    }

    private void Endpoint(string host = "first-shop", string session = "first") => File.WriteAllText(StatePath,
        JsonSerializer.Serialize(new { Url = $"https://{host}.trycloudflare.com", Ready = true, Session = session,
            UpdatedAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() }));

    private AppDbContext Context() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql(Environment.GetEnvironmentVariable("SAVEBITE_WEBHOOK_TEST_POSTGRES")).Options);

    private async Task PrepareDatabaseAsync()
    {
        await using var db = Context();
        await db.Database.MigrateAsync();
        // This test suite only runs with the explicitly supplied disposable database.
        await db.Set<PayOsWebhookSyncState>().ExecuteDeleteAsync();
    }

    private PayOsAccount Shop(string suffix = "one")
    {
        var account = new PayOsAccount(PayOsAccount.Shop(Guid.NewGuid()), new("client-" + suffix, "api-" + suffix, "checksum-" + suffix));
        _sources.Items[account.Id] = account;
        return account;
    }

    private async Task SyncAsync(string id)
    {
        await using var db = Context();
        await new PayOsWebhookSynchronizer(db, _sources, new(_configuration), _validator, _api,
            Options.Create(new PayOsWebhookSyncOptions()), TimeProvider.System, NullLogger<PayOsWebhookSynchronizer>.Instance)
            .SynchronizeAsync(id, default);
    }

    private async Task<PayOsWebhookSyncState> StateAsync(string id)
    {
        await using var db = Context();
        return await db.Set<PayOsWebhookSyncState>().AsNoTracking().SingleAsync(x => x.AccountId == id);
    }

    private async Task<bool> ReadyAsync(PayOsAccount account)
    {
        await using var db = Context();
        var readiness = new PayOsWebhookReadiness(db, _sources, new(_configuration), _configuration);
        if (account.Id == PayOsAccount.Platform) return await readiness.IsPlatformReadyAsync(default);
        try
        {
            await readiness.EnsureShopReadyAsync(new ShopPaymentConfig { ShopId = Guid.Parse(account.Id[5..]),
                PayosClientId = account.Credentials.ClientId, PayosApiKey = account.Credentials.ApiKey,
                PayosChecksumKey = account.Credentials.ChecksumKey }, default);
            return true;
        }
        catch (AppException) { return false; }
    }

    [WebhookPostgresFact]
    public async Task EachAccountRequiresItsOwnSuccessfulConfirmationAndSurvivesNewScopes()
    {
        await PrepareDatabaseAsync();
        var first = Shop(); var second = Shop("two");
        Assert.False(await ReadyAsync(first));
        await SyncAsync(first.Id);
        Assert.True(await ReadyAsync(first));
        Assert.False(await ReadyAsync(second));
        Assert.Equal("Synced", (await StateAsync(first.Id)).Status);
        await SyncAsync(first.Id);
        Assert.Equal(1, _api.Calls);
        Assert.Equal(1, _validator.Calls);
    }

    [WebhookPostgresFact]
    public async Task UrlAndSessionChangesInvalidateReadinessAndTriggerConfirmationAgain()
    {
        await PrepareDatabaseAsync(); var account = Shop();
        await SyncAsync(account.Id);
        Endpoint("second-shop", "second");
        Assert.False(await ReadyAsync(account));
        await SyncAsync(account.Id);
        Assert.True(await ReadyAsync(account));
        Assert.Equal("https://second-shop.trycloudflare.com/api/payments/payos/webhook", (await StateAsync(account.Id)).WebhookUrl);
        Endpoint("second-shop", "third");
        Assert.False(await ReadyAsync(account));
        await SyncAsync(account.Id);
        Assert.Equal(3, _api.Calls);
        Assert.Equal(1, _validator.Calls); // A URL change does not re-probe unchanged credentials.
    }

    [WebhookPostgresFact]
    public async Task RetryStatePersistsAndNewCredentialsBypassOldBackoff()
    {
        await PrepareDatabaseAsync(); var account = Shop();
        _api.Action = _ => throw new InvalidOperationException("secret-provider-body");
        await SyncAsync(account.Id);
        var failed = await StateAsync(account.Id);
        Assert.Equal("Retrying", failed.Status);
        Assert.Equal("InvalidOperationException", failed.LastErrorCode);
        Assert.True(failed.NextAttemptAt > failed.UpdatedAt);
        Assert.False(await ReadyAsync(account));
        await SyncAsync(account.Id);
        Assert.Equal(1, _api.Calls);
        var rotated = account with { Credentials = new("client-new", "api-new", "checksum-new") };
        _sources.Items[account.Id] = rotated;
        _api.Action = null;
        await SyncAsync(account.Id);
        Assert.True(await ReadyAsync(rotated));
        Assert.False(await ReadyAsync(account));
        Assert.Equal(2, _validator.Calls);
    }

    [WebhookPostgresFact]
    public async Task OldSuccessCannotOpenPaymentsAfterUrlChangesDuringProviderRequest()
    {
        await PrepareDatabaseAsync(); var account = Shop();
        _api.Action = url => { Endpoint("new-shop", "new"); return JsonSerializer.SerializeToElement(new { webhookUrl = url }); };
        await SyncAsync(account.Id);
        Assert.NotEqual("Synced", (await StateAsync(account.Id)).Status);
        Assert.False(await ReadyAsync(account));
        _api.Action = null;
        await SyncAsync(account.Id);
        Assert.True(await ReadyAsync(account));
    }

    [WebhookPostgresFact]
    public async Task PlatformUsesSeparateEndpointAndCredentialsIncludingEnvironmentOnlyAccount()
    {
        await PrepareDatabaseAsync(); var shop = Shop();
        var platform = new PayOsAccount(PayOsAccount.Platform, new("platform-client", "platform-api", "platform-checksum"));
        _sources.Items[platform.Id] = platform;
        await SyncAsync(platform.Id);
        Assert.True(await ReadyAsync(platform));
        Assert.False(await ReadyAsync(shop));
        Assert.EndsWith("/api/platform-fees/payos/webhook", (await StateAsync(platform.Id)).WebhookUrl);
        Assert.Equal(platform.Credentials, _api.LastCredentials);
    }

    [WebhookPostgresFact]
    public async Task ConcurrentWorkersCannotRegisterTheSameAccountTwice()
    {
        await PrepareDatabaseAsync(); var account = Shop();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var finish = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _api.AsyncAction = async url => { started.SetResult(); await finish.Task; return JsonSerializer.SerializeToElement(new { webhookUrl = url }); };
        var first = SyncAsync(account.Id);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        try { await SyncAsync(account.Id); Assert.Equal(1, _api.Calls); }
        finally { finish.TrySetResult(); await first; }
        Assert.True(await ReadyAsync(account));
    }

    [WebhookPostgresFact]
    public async Task IncorrectConfirmedUrlNeverEnablesPayments()
    {
        await PrepareDatabaseAsync(); var account = Shop();
        _api.Action = _ => JsonSerializer.SerializeToElement(new { webhookUrl = "https://wrong.example.com" });
        await SyncAsync(account.Id);
        Assert.Equal("Retrying", (await StateAsync(account.Id)).Status);
        Assert.False(await ReadyAsync(account));
    }

    [WebhookPostgresFact]
    public async Task DueRetryRecoversFromProviderOutageUsingPersistedState()
    {
        await PrepareDatabaseAsync(); var account = Shop();
        _api.Action = _ => throw new HttpRequestException("provider temporarily unavailable");
        await SyncAsync(account.Id);
        await using (var db = Context())
            await db.Set<PayOsWebhookSyncState>().Where(x => x.AccountId == account.Id)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.NextAttemptAt, DateTime.UtcNow.AddSeconds(-1)));
        _api.Action = null;
        await SyncAsync(account.Id);
        Assert.True(await ReadyAsync(account));
        Assert.Equal(2, (await StateAsync(account.Id)).Attempts);
        Assert.Equal(2, _api.Calls);
    }

    [WebhookPostgresFact]
    public async Task OldCredentialsCannotEnablePaymentsWhenKeysChangeDuringConfirmation()
    {
        await PrepareDatabaseAsync(); var account = Shop();
        var rotated = account with { Credentials = new("new-client", "new-api", "new-checksum") };
        _api.Action = url => { _sources.Items[account.Id] = rotated; return JsonSerializer.SerializeToElement(new { webhookUrl = url }); };
        await SyncAsync(account.Id);
        Assert.False(await ReadyAsync(rotated));
        Assert.NotEqual("Synced", (await StateAsync(account.Id)).Status);
        _api.Action = null;
        await SyncAsync(account.Id);
        Assert.True(await ReadyAsync(rotated));
    }

    [WebhookPostgresFact]
    public async Task MissingTunnelSnapshotBlocksPaymentsEvenAfterSuccessfulRegistration()
    {
        await PrepareDatabaseAsync(); var account = Shop();
        await SyncAsync(account.Id);
        File.Delete(StatePath);
        Assert.False(await ReadyAsync(account));
        await SyncAsync(account.Id);
        Assert.Equal(1, _api.Calls);
    }

    [Theory]
    [InlineData(1, 5)] [InlineData(2, 10)] [InlineData(20, 300)]
    public void BackoffIsExponentialAndBounded(int attempts, double seconds)
        => Assert.Equal(TimeSpan.FromSeconds(seconds), new PayOsWebhookSyncOptions().Backoff(attempts, 1));

    [Fact]
    public void CredentialVersionIncludesEveryKeyAndRevisionWithoutStoringPlaintext()
    {
        var first = Shop();
        Assert.DoesNotContain(first.Credentials.ApiKey, first.CredentialVersion);
        Assert.NotEqual(first.CredentialVersion, (first with { Credentials = first.Credentials with { ChecksumKey = "changed" } }).CredentialVersion);
        Assert.NotEqual(first.CredentialVersion, (first with { Revision = 1 }).CredentialVersion);
    }

    public Task DisposeAsync()
    {
        Directory.Delete(_directory, recursive: true);
        return Task.CompletedTask;
    }

    private sealed class Sources : IPayOsAccountSource
    {
        public Dictionary<string, PayOsAccount> Items { get; } = new();
        public Task<PayOsAccount?> ReadAsync(string id, CancellationToken ct) => Task.FromResult(Items.GetValueOrDefault(id));
    }
    private sealed class Validator : IPayOsCredentialsValidator
    {
        public int Calls;
        public Task ValidateAsync(PlatformPayOsCredentials credentials, CancellationToken ct = default) { Calls++; return Task.CompletedTask; }
        public Task ValidateShopAsync(PlatformPayOsCredentials credentials, CancellationToken ct = default) => throw new NotSupportedException();
        public Task ValidatePlatformAsync(PlatformPayOsCredentials credentials, CancellationToken ct = default) => throw new NotSupportedException();
    }
    private sealed class Api : IPayOsApiClient
    {
        public int Calls;
        public PlatformPayOsCredentials? LastCredentials;
        public Func<string, JsonElement>? Action;
        public Func<string, Task<JsonElement>>? AsyncAction;
        public Task<JsonElement> SendAsync(HttpMethod method, string path, PlatformPayOsCredentials credentials, object? body, CancellationToken ct)
        {
            Assert.Equal("confirm-webhook", path);
            Interlocked.Increment(ref Calls); LastCredentials = credentials;
            var url = JsonSerializer.SerializeToElement(body).GetProperty("webhookUrl").GetString()!;
            if (AsyncAction != null) return AsyncAction(url);
            return Task.FromResult(Action?.Invoke(url) ?? JsonSerializer.SerializeToElement(new { webhookUrl = url }));
        }
    }
}
