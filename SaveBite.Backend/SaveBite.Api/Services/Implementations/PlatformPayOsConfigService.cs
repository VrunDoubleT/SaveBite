using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class PlatformPayOsConfigService : IPlatformPayOsConfigService
{
    private static readonly Guid ConfigId = Guid.Parse("a4dd31a7-541d-4c3a-8be8-22cdb32a728a");
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;
    private readonly IDataProtector _protector;
    private readonly IAdminAuditWriter _audit;
    private readonly TimeProvider _clock;
    private readonly IPayOsCredentialsValidator _validator;
    private readonly IPayOsCredentialProtector _paymentProtector;

    public PlatformPayOsConfigService(
        AppDbContext db,
        IConfiguration configuration,
        IDataProtectionProvider protectionProvider,
        IAdminAuditWriter audit,
        TimeProvider clock, IPayOsCredentialsValidator validator, IPayOsCredentialProtector paymentProtector)
    {
        _db = db;
        _configuration = configuration;
        _protector = protectionProvider.CreateProtector("SaveBite.PlatformPayOsConfig.v1");
        _audit = audit;
        _clock = clock;
        _validator = validator;
        _paymentProtector = paymentProtector;
    }

    public async Task<PlatformPayOsConfigResponse> GetAdminConfigAsync(CancellationToken cancellationToken = default)
    {
        var stored = await GetStoredConfigAsync(cancellationToken);
        if (stored is not null)
            return ToResponse(stored);

        var credentials = GetEnvironmentCredentials();
        return new PlatformPayOsConfigResponse(
            Mask(credentials.ClientId),
            !string.IsNullOrWhiteSpace(credentials.ClientId),
            !string.IsNullOrWhiteSpace(credentials.ApiKey),
            !string.IsNullOrWhiteSpace(credentials.ChecksumKey),
            null,
            null,
            null);
    }

    public async Task<IReadOnlyList<PlatformPayOsConfigHistoryResponse>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        var history = await _db.PlatformPayOsConfigHistories
            .AsNoTracking()
            .Include(x => x.ChangedByUser)
            .OrderByDescending(x => x.ChangedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        return history.Select(x => new PlatformPayOsConfigHistoryResponse(
            x.Id,
            x.ChangedByUserId,
            x.ChangedByUser.FullName,
            JsonSerializer.Deserialize<string[]>(x.ChangedFieldsJson) ?? [],
            x.PreviousMaskedClientId,
            x.MaskedClientId,
            x.ChangedAt)).ToList();
    }

    public async Task<PlatformPayOsConfigResponse> UpdateAsync(
        Guid adminId,
        UpdatePlatformPayOsConfigRequest request,
        CancellationToken cancellationToken = default)
    {
        if (adminId == Guid.Empty)
            throw AppException.Unauthorized();

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194602)", cancellationToken);
        await _db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(728194604)", cancellationToken);

        var config = await GetStoredConfigAsync(cancellationToken);
        var old = config is null
            ? ToCredentials(GetEnvironmentCredentials())
            : Unprotect(config);

        var newClientId = ValueOrExisting(request.ClientId, old.ClientId);
        var newApiKey = ValueOrExisting(request.ApiKey, old.ApiKey);
        var newChecksumKey = ValueOrExisting(request.ChecksumKey, old.ChecksumKey);

        if (string.IsNullOrWhiteSpace(newClientId)
            || string.IsNullOrWhiteSpace(newApiKey)
            || string.IsNullOrWhiteSpace(newChecksumKey))
            throw AppException.BadRequest("Client ID, API key and checksum key are required before platform-fee payments can be enabled.");

        if (newClientId.Length > 255 || newApiKey.Length > 1000 || newChecksumKey.Length > 1000)
            throw AppException.BadRequest("PayOS credentials exceed the supported maximum length.");

        var newCredentials = new PlatformPayOsCredentials(newClientId, newApiKey, newChecksumKey);
        var fields = GetChangedFields(old, newCredentials);
        var creatingDatabaseConfig = config is null;
        if (config is not null && fields.Count == 0 && config.VerifiedAt != null && !config.IsInvalid)
        {
            await _validator.ValidatePlatformAsync(newCredentials, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ToResponse(config);
        }

        if (creatingDatabaseConfig && fields.Count == 0)
            fields.Add("ConfigurationSource");

        await _validator.ValidatePlatformAsync(newCredentials, cancellationToken);
        // Preserve legacy pending links before rotating the global credential set.
        if (!string.IsNullOrWhiteSpace(old.ClientId) && !string.IsNullOrWhiteSpace(old.ApiKey)
            && !string.IsNullOrWhiteSpace(old.ChecksumKey))
        {
            var legacy = await _db.PlatformFeePayments.Where(p => p.ProtectedPayOsCredentials == null
                && p.GatewayTransactionId != null)
                .ToListAsync(cancellationToken);
            foreach (var payment in legacy) payment.ProtectedPayOsCredentials = _paymentProtector.Protect(old);
        }
        var now = _clock.GetUtcNow().UtcDateTime;
        if (config is null)
        {
            config = new PlatformPayOsConfig { Id = ConfigId };
            _db.PlatformPayOsConfigs.Add(config);
        }

        config.Revision++;
        config.VerifiedAt = now;
        config.IsInvalid = false;
        if (fields.Count == 0) fields.Add("Verification");
        config.ClientId = newCredentials.ClientId;
        config.ProtectedApiKey = _protector.Protect(newCredentials.ApiKey);
        config.ProtectedChecksumKey = _protector.Protect(newCredentials.ChecksumKey);
        config.UpdatedByUserId = adminId;
        config.UpdatedAt = now;

        var fieldsJson = JsonSerializer.Serialize(fields);
        var previousMaskedClientId = Mask(old.ClientId);
        var maskedClientId = Mask(newCredentials.ClientId);
        _db.PlatformPayOsConfigHistories.Add(new PlatformPayOsConfigHistory
        {
            Id = Guid.NewGuid(),
            ChangedByUserId = adminId,
            ChangedFieldsJson = fieldsJson,
            PreviousMaskedClientId = previousMaskedClientId,
            MaskedClientId = maskedClientId ?? string.Empty,
            ChangedAt = now
        });

        _audit.Record(new AuditLog
        {
            ActorUserId = adminId,
            Action = AuditActionTypes.PlatformPayOsConfigUpdated,
            TargetType = "PlatformPayOsConfig",
            TargetId = ConfigId,
            OldValuesJson = JsonSerializer.Serialize(new
            {
                Source = creatingDatabaseConfig ? "Environment" : "Database",
                ClientId = previousMaskedClientId,
                ChangedFields = fields
            }),
            NewValuesJson = JsonSerializer.Serialize(new
            {
                Source = "Database",
                ClientId = maskedClientId,
                ChangedFields = fields,
                CredentialFieldsConfigured = 3
            }),
            CreatedAt = now
        });

        var outboxId = Guid.NewGuid();
        _db.Set<OrderEventOutbox>().Add(new OrderEventOutbox
        {
            Id = outboxId,
            EventKey = $"platform-payos-verified:{config.Revision}",
            RoutingKey = SaveBite.Backend.Messaging.Constants.RabbitMqRoutingKeys.PlatformPayOsVerified,
            PayloadJson = JsonSerializer.Serialize(new SaveBite.Backend.Messaging.Events.PlatformPayOsVerifiedEvent(config.Revision)),
            CreatedAt = now
        });
        _db.TrackNotificationOutbox(outboxId);
        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        config.UpdatedByUser = await _db.Users.SingleAsync(x => x.Id == adminId, cancellationToken);
        return ToResponse(config);
    }

    public async Task<PlatformPayOsCredentials> GetCredentialsAsync(CancellationToken cancellationToken = default)
    {
        var stored = await GetStoredConfigAsync(cancellationToken);
        if (stored is null)
        {
            var environment = GetEnvironmentCredentials();
            if (!environment.HasAllCredentials)
                throw AppException.ServiceUnavailable("Platform PayOS account is not configured in the database or environment.");
            return ToCredentials(environment);
        }

        return Unprotect(stored);
    }

    private Task<PlatformPayOsConfig?> GetStoredConfigAsync(CancellationToken cancellationToken)
        => _db.PlatformPayOsConfigs
            .Include(x => x.UpdatedByUser)
            .SingleOrDefaultAsync(x => x.Id == ConfigId, cancellationToken);

    private PlatformPayOsCredentials Unprotect(PlatformPayOsConfig config)
    {
        try
        {
            return new PlatformPayOsCredentials(
                config.ClientId,
                _protector.Unprotect(config.ProtectedApiKey),
                _protector.Unprotect(config.ProtectedChecksumKey));
        }
        catch (CryptographicException ex)
        {
            throw new InvalidOperationException("Unable to decrypt platform PayOS credentials. Verify the persistent ASP.NET Data Protection key ring.", ex);
        }
    }

    private PlatformPayOsConfigResponse ToResponse(PlatformPayOsConfig config)
        => new(
            Mask(config.ClientId),
            !string.IsNullOrWhiteSpace(config.ClientId),
            !string.IsNullOrWhiteSpace(config.ProtectedApiKey),
            !string.IsNullOrWhiteSpace(config.ProtectedChecksumKey),
            config.UpdatedAt,
            config.UpdatedByUserId,
            config.UpdatedByUser?.FullName)
        { IsVerified = config.VerifiedAt != null, PaymentsAvailable = config.VerifiedAt != null && !config.IsInvalid };

    private (string ClientId, string ApiKey, string ChecksumKey, bool HasAny, bool HasAllCredentials) GetEnvironmentCredentials()
    {
        var clientId = _configuration["PlatformPayOS:ClientId"] ?? string.Empty;
        var apiKey = _configuration["PlatformPayOS:ApiKey"] ?? string.Empty;
        var checksumKey = _configuration["PlatformPayOS:ChecksumKey"] ?? string.Empty;
        return (clientId, apiKey, checksumKey,
            !string.IsNullOrWhiteSpace(clientId) || !string.IsNullOrWhiteSpace(apiKey) || !string.IsNullOrWhiteSpace(checksumKey),
            !string.IsNullOrWhiteSpace(clientId) && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(checksumKey));
    }

    private static string ValueOrExisting(string? value, string existing)
        => string.IsNullOrWhiteSpace(value) ? existing : value.Trim();

    private static PlatformPayOsCredentials ToCredentials(
        (string ClientId, string ApiKey, string ChecksumKey, bool HasAny, bool HasAllCredentials) environment)
        => new(environment.ClientId, environment.ApiKey, environment.ChecksumKey);

    private static List<string> GetChangedFields(PlatformPayOsCredentials old, PlatformPayOsCredentials current)
    {
        var fields = new List<string>();
        if (!string.Equals(old.ClientId, current.ClientId, StringComparison.Ordinal)) fields.Add("ClientId");
        if (!string.Equals(old.ApiKey, current.ApiKey, StringComparison.Ordinal)) fields.Add("ApiKey");
        if (!string.Equals(old.ChecksumKey, current.ChecksumKey, StringComparison.Ordinal)) fields.Add("ChecksumKey");
        return fields;
    }

    private static string? Mask(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.Length <= 6) return new string('*', value.Length);
        return $"{value[..3]}{new string('*', value.Length - 6)}{value[^3..]}";
    }
}
