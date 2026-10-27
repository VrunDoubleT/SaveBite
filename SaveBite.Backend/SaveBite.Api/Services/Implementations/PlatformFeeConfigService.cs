using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class PlatformFeeConfigService : IPlatformFeeConfigService
{
    private static readonly TimeSpan EffectiveFromTolerance = TimeSpan.FromMinutes(1);

    private readonly IPlatformFeeConfigRepository _repository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PlatformFeeConfigService> _logger;

    public PlatformFeeConfigService(
        IPlatformFeeConfigRepository repository,
        TimeProvider timeProvider,
        ILogger<PlatformFeeConfigService> logger)
    {
        _repository = repository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    // UC-148: View Fee Configuration (BR-189, BR-093)
    public async Task<PlatformFeeConfigResponse> GetActiveConfigAsync(CancellationToken ct = default)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var config = await _repository.GetActiveConfigAsync(now, ct);

        if (config == null)
        {
            throw AppException.NotFound("Active platform fee configuration not found.");
        }

        return ToResponse(config);
    }
    
     public async Task<IReadOnlyList<PlatformFeeConfigResponse>> GetAllConfigsAsync(CancellationToken ct = default)
        {
            var configs = await _repository.GetAllConfigsAsync(ct);
            return configs.Select(ToResponse).ToList();
        }

    // UC-149: Update Fee Configuration (BR-189, BR-164, BR-165, BR-163)
    public async Task<PlatformFeeConfigResponse> UpdateConfigAsync(
        Guid adminId,
        UpdatePlatformFeeConfigRequest request,
        CancellationToken ct = default)
    {
        if (adminId == Guid.Empty)
        {
            throw AppException.Forbidden("Only an Administrator may update platform-fee configuration.");
        }

        // BR-164: Monthly platform-fee percentage must be between 0% and 100%
        if (request.FeeRate < 0 || request.FeeRate > 100)
        {
            throw AppException.BadRequest("Platform fee rate must be between 0% and 100%.");
        }

        // Normalize fee rate from percentage (0% to 100%) to fraction (0.0 to 1.0)
        var normalizedRate = Math.Round(request.FeeRate / 100m, 6);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        // BR-165: Reject effective dates in the past (allowing 1 minute tolerance for network clock drift)
        if (request.EffectiveFrom.HasValue && request.EffectiveFrom.Value < now - EffectiveFromTolerance)
        {
            throw AppException.BadRequest("Effective date must be in the future or current time.");
        }

        var effectiveFrom = request.EffectiveFrom ?? now;

        if (effectiveFrom < now)
        {
            effectiveFrom = now;
        }

        PlatformFeeConfig resultConfig;

        if (effectiveFrom > now)
        {
            // CASE 1: Future Scheduled Configuration (Upcoming Rate)
            // NO DELETE: If an upcoming scheduled config already exists, edit it in place!
            var existingUpcoming = await _repository.GetUpcomingConfigAsync(now, ct);
            if (existingUpcoming != null)
            {
                var oldRate = existingUpcoming.FeeRate;
                var oldEffective = existingUpcoming.EffectiveFrom;

                existingUpcoming.FeeRate = normalizedRate;
                existingUpcoming.EffectiveFrom = effectiveFrom;
                resultConfig = existingUpcoming;

                // Sync current active config's EffectiveTo with the updated upcoming start date
                var current = await _repository.GetActiveConfigAsync(now, ct);
                if (current != null && current.EffectiveFrom < effectiveFrom)
                {
                    current.EffectiveTo = effectiveFrom;
                }

                var audit = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = adminId,
                    ActorType = AuditActorType.User,
                    Action = "UpdateScheduledFeeConfiguration",
                    TargetType = "PlatformFeeConfig",
                    TargetId = existingUpcoming.Id,
                    OldValuesJson = JsonSerializer.Serialize(new { FeeRate = oldRate, EffectiveFrom = oldEffective }),
                    NewValuesJson = JsonSerializer.Serialize(new { FeeRate = normalizedRate, EffectiveFrom = effectiveFrom }),
                    CreatedAt = now
                };
                _repository.AddAuditLog(audit);
            }
            else
            {
                // No existing upcoming config, insert new future scheduled config
                var current = await _repository.GetActiveConfigAsync(now, ct);
                if (current != null && current.EffectiveFrom < effectiveFrom)
                {
                    current.EffectiveTo = effectiveFrom;
                }

                resultConfig = new PlatformFeeConfig
                {
                    Id = Guid.NewGuid(),
                    FeeRate = normalizedRate,
                    EffectiveFrom = effectiveFrom,
                    EffectiveTo = null,
                    CreatedBy = adminId,
                    CreatedAt = now
                };
                await _repository.AddConfigAsync(resultConfig, ct);

                var audit = new AuditLog
                {
                    Id = Guid.NewGuid(),
                    ActorUserId = adminId,
                    ActorType = AuditActorType.User,
                    Action = "CreateScheduledFeeConfiguration",
                    TargetType = "PlatformFeeConfig",
                    TargetId = resultConfig.Id,
                    OldValuesJson = null,
                    NewValuesJson = JsonSerializer.Serialize(new { FeeRate = normalizedRate, EffectiveFrom = effectiveFrom }),
                    CreatedAt = now
                };
                _repository.AddAuditLog(audit);
            }
        }
        else
        {
            // CASE 2: Immediate Fee Update (takes effect now)
            var currentConfig = await _repository.GetActiveConfigAsync(now, ct);
            if (currentConfig != null)
            {
                // Ensure check constraint "effective_to" > "effective_from" is always strictly satisfied
                var closeTime = now > currentConfig.EffectiveFrom
                    ? now
                    : currentConfig.EffectiveFrom.AddSeconds(1);

                currentConfig.EffectiveTo = closeTime;
                effectiveFrom = closeTime;
            }
            
            DateTime? upcomingStart = null;
            if (request.CancelUpcomingScheduled)
            {
                var pendingConfigs = await _repository.GetPendingConfigsAsync(now, ct);
                foreach (var pending in pendingConfigs)
                {
                    _repository.RemoveConfig(pending);
                }
            }
            else
            {
                var upcoming = await _repository.GetUpcomingConfigAsync(now, ct);
                if (upcoming != null && upcoming.EffectiveFrom > effectiveFrom)
                {
                    upcomingStart = upcoming.EffectiveFrom;
                }
            }


            resultConfig = new PlatformFeeConfig
            {
                Id = Guid.NewGuid(),
                FeeRate = normalizedRate,
                EffectiveFrom = effectiveFrom,
                EffectiveTo = upcomingStart,
                CreatedBy = adminId,
                CreatedAt = now
            };

            await _repository.AddConfigAsync(resultConfig, ct);

            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = adminId,
                ActorType = AuditActorType.User,
                Action = "UpdateFeeConfiguration",
                TargetType = "PlatformFeeConfig",
                TargetId = resultConfig.Id,
                OldValuesJson = currentConfig == null
                    ? null
                    : JsonSerializer.Serialize(new { currentConfig.FeeRate, currentConfig.EffectiveFrom, currentConfig.EffectiveTo }),
                NewValuesJson = JsonSerializer.Serialize(new { resultConfig.FeeRate, resultConfig.EffectiveFrom }),
                CreatedAt = now
            };
            _repository.AddAuditLog(audit);
        }

        await _repository.SaveChangesAsync(ct);
        _logger.LogInformation("Platform fee configuration updated by Admin {AdminId}. Rate: {FeeRate}", adminId, normalizedRate);

        return ToResponse(resultConfig);
    }

    public async Task CancelScheduledConfigAsync(Guid adminId, Guid configId, CancellationToken ct = default)
    {
        if (adminId == Guid.Empty)
        {
            throw AppException.Forbidden("Only an Administrator may cancel scheduled platform-fee configurations.");
        }

        var config = await _repository.GetByIdAsync(configId, ct);
        if (config == null)
        {
            throw AppException.NotFound("Scheduled platform fee configuration not found.");
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        if (config.EffectiveFrom <= now)
        {
            throw AppException.BadRequest("Only upcoming scheduled configurations may be cancelled.");
        }

        _repository.RemoveConfig(config);

        // Also clean up any other pending configs scheduled for the future to avoid ghost configs
        var pendingConfigs = await _repository.GetPendingConfigsAsync(now, ct);
        foreach (var pending in pendingConfigs.Where(p => p.Id != config.Id))
        {
            _repository.RemoveConfig(pending);
        }

        // Re-open current active config if its EffectiveTo was capped
        var currentConfig = await _repository.GetActiveConfigAsync(now, ct);
        if (currentConfig != null)
        {
            currentConfig.EffectiveTo = null;
        }

        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = adminId,
            ActorType = AuditActorType.User,
            Action = "CancelScheduledFeeConfiguration",
            TargetType = "PlatformFeeConfig",
            TargetId = config.Id,
            OldValuesJson = JsonSerializer.Serialize(new { config.FeeRate, config.EffectiveFrom }),
            CreatedAt = now
        };
        _repository.AddAuditLog(audit);

        await _repository.SaveChangesAsync(ct);
        _logger.LogInformation("Upcoming scheduled platform fee config {ConfigId} cancelled by Admin {AdminId}.", configId, adminId);
    }

    private static PlatformFeeConfigResponse ToResponse(PlatformFeeConfig config)
    {
        return new PlatformFeeConfigResponse(
            config.Id,
            config.FeeRate,
            Math.Round(config.FeeRate * 100m, 4),
            config.EffectiveFrom,
            config.EffectiveTo,
            config.CreatedBy,
            config.CreatedAt
        );
    }
}
