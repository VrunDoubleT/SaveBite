using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Payments.Webhooks;

public sealed class PayOsWebhookSynchronizer(AppDbContext db, IPayOsAccountSource accounts, PayOsWebhookEndpoint endpoint,
    IPayOsCredentialsValidator validator, IPayOsApiClient api, IOptions<PayOsWebhookSyncOptions> options,
    TimeProvider clock, ILogger<PayOsWebhookSynchronizer> logger)
{
    public async Task SynchronizeAsync(string accountId, CancellationToken ct)
    {
        // A session advisory lock covers provider I/O without an open database transaction.
        // Closing the connection releases it after a crash, so another worker can recover.
        await db.Database.OpenConnectionAsync(ct);
        var locked = false;
        var lockName = "payos-webhook:" + accountId;
        try
        {
            locked = await db.Database.SqlQuery<bool>($"SELECT pg_try_advisory_lock(hashtextextended({lockName}, 0)) AS \"Value\"").SingleAsync(ct);
            if (!locked) return;
            var state = await db.Set<PayOsWebhookSyncState>().SingleOrDefaultAsync(x => x.AccountId == accountId, ct);
            if (state?.LastErrorCode == "CredentialReadFailure" && state.NextAttemptAt > clock.GetUtcNow().UtcDateTime) return;
            PayOsAccount? account;
            try { account = await accounts.ReadAsync(accountId, ct); }
            catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or InvalidOperationException)
            {
                if (state is null) { state = new() { AccountId = accountId }; db.Add(state); }
                state.Status = "Retrying";
                state.CredentialsVerified = false;
                state.LastErrorCode = "CredentialReadFailure";
                state.Attempts++;
                state.UpdatedAt = clock.GetUtcNow().UtcDateTime;
                state.LastAttemptAt = state.UpdatedAt;
                state.NextAttemptAt = state.UpdatedAt + options.Value.Backoff(state.Attempts, Random.Shared.NextDouble());
                await db.SaveChangesAsync(ct);
                logger.LogWarning("Cannot read PayOS credentials for {AccountId}; retry at {RetryAt}.", accountId, state.NextAttemptAt);
                return;
            }
            if (account is null) return;
            PayOsWebhookTarget target;
            try { target = endpoint.Target(account); }
            catch (AppException) { return; } // Tunnel unavailable: do not reuse the old URL.
            var now = clock.GetUtcNow().UtcDateTime;
            if (state is null)
            {
                state = new() { AccountId = accountId };
                db.Add(state);
            }
            var changed = !target.Matches(state);
            if (!changed && target.IsReady(state) && !account.NeedsValidation) return;
            if (!changed && state.NextAttemptAt > now) return;
            if (changed)
            {
                if (state.CredentialVersion != target.CredentialVersion) state.CredentialsVerified = false;
                state.CredentialVersion = target.CredentialVersion;
                state.WebhookUrl = target.WebhookUrl;
                state.EndpointSession = target.Session;
                state.Attempts = 0;
                state.SucceededAt = null;
            }
            state.Status = "Synchronizing";
            state.Attempts++;
            state.LastAttemptAt = now;
            state.NextAttemptAt = now;
            state.LastErrorCode = null;
            state.UpdatedAt = now;
            await db.SaveChangesAsync(ct);
            try
            {
                if (!state.CredentialsVerified || account.NeedsValidation)
                {
                    await validator.ValidateAsync(account.Credentials, ct);
                    state.CredentialsVerified = true;
                    await db.SaveChangesAsync(ct);
                }
                var confirmation = await api.SendAsync(HttpMethod.Post, "confirm-webhook", account.Credentials,
                    new { webhookUrl = target.WebhookUrl }, ct);
                if (!confirmation.TryGetProperty("webhookUrl", out var confirmed)
                    || confirmed.ValueKind != System.Text.Json.JsonValueKind.String || confirmed.GetString() != target.WebhookUrl)
                    throw AppException.ServiceUnavailable("PayOS did not confirm the expected webhook URL.");

                var latest = await accounts.ReadAsync(accountId, ct);
                if (latest is null || target != endpoint.Target(latest))
                {
                    state.Status = "Pending";
                    state.LastErrorCode = "STALE_TARGET";
                    state.NextAttemptAt = clock.GetUtcNow().UtcDateTime;
                }
                else
                {
                    if (accountId == PayOsAccount.Platform && account.NeedsValidation)
                    {
                        await db.PlatformPayOsConfigs.Where(x => x.Revision == account.Revision)
                            .ExecuteUpdateAsync(update => update.SetProperty(x => x.IsInvalid, false)
                                .SetProperty(x => x.VerifiedAt, clock.GetUtcNow().UtcDateTime), ct);
                    }
                    state.Status = "Synced";
                    state.SucceededAt = clock.GetUtcNow().UtcDateTime;
                    logger.LogInformation("PayOS webhook synchronized for {AccountId}; session {Session}.", accountId, target.Session);
                }
                state.UpdatedAt = clock.GetUtcNow().UtcDateTime;
                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                // Persist only a bounded machine code, never provider text or credential material.
                state.Status = "Retrying";
                var error = ex is AppException app ? app.ErrorCode : ex.GetType().Name;
                state.LastErrorCode = error.Length > 128 ? error[..128] : error;
                state.UpdatedAt = clock.GetUtcNow().UtcDateTime;
                state.NextAttemptAt = state.UpdatedAt + options.Value.Backoff(state.Attempts, Random.Shared.NextDouble());
                logger.LogWarning("PayOS webhook failed for {AccountId}; attempt {Attempt}; code {Code}; retry at {RetryAt}.",
                    accountId, state.Attempts, state.LastErrorCode, state.NextAttemptAt);
                await db.SaveChangesAsync(ct);
            }
        }
        finally
        {
            try
            {
                if (locked) await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_unlock(hashtextextended({lockName}, 0))", CancellationToken.None);
            }
            finally { await db.Database.CloseConnectionAsync(); }
        }
    }
}
