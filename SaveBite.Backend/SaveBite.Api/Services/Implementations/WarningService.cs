using System.Text.Json;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Messaging.Constants;
using SaveBite.Backend.Messaging.Events;
using SaveBite.Backend.Messaging.Interfaces;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Requests;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

public sealed class WarningService : IWarningService
{
    private readonly IWarningRepository _warningRepository;
    private readonly IRabbitMqPublisher _publisher;

    public WarningService(IWarningRepository warningRepository, IRabbitMqPublisher publisher)
    {
        _warningRepository = warningRepository;
        _publisher = publisher;
    }

    private async Task<Guid> GetShopIdAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var shopId = await _warningRepository.GetOwnedShopIdAsync(ownerId, cancellationToken);
        if (!shopId.HasValue)
            throw AppException.NotFound("Shop not found.");

        return shopId.Value;
    }

    // View Warning History.
    public async Task<PagedResult<WarningResponse>> GetHistoryAsync(Guid ownerId, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var shopId = await GetShopIdAsync(ownerId, cancellationToken);
        var (items, total) = await _warningRepository.GetHistoryAsync(shopId, page, pageSize, cancellationToken);
        var responses = items.Select(Map).ToArray();
        return PagedResult<WarningResponse>.Create(responses, page, pageSize, total);
    }

    // View Warning Details.
    public async Task<WarningResponse> GetDetailsAsync(Guid ownerId, Guid warningId, CancellationToken cancellationToken)
    {
        if (warningId == Guid.Empty)
            throw AppException.BadRequest("Warning ID is invalid.");

        var shopId = await GetShopIdAsync(ownerId, cancellationToken);
        var warning = await _warningRepository.GetByIdAsync(shopId, warningId, cancellationToken);

        if (warning == null)
            throw AppException.NotFound("Warning not found.");
        
        return Map(warning);
    }
    
    // Send Violation Warning.
    public async Task<ModerationActionResponse> IssueWarningAsync(Guid adminId, Guid productId, IssueViolationWarningRequest request, CancellationToken ct)
    {
        var product = await _warningRepository.GetProductForWarningAsync(productId, ct);

        if (product == null)
            throw AppException.NotFound("Product not found.");
        
        if (product.DeletedAt != null)
            throw AppException.Conflict("Product has been removed.");
        
        var reason = request.Reason.Trim();
        var requiredCorrection = request.RequiredCorrection.Trim();

        var revision = await _warningRepository.GetLatestRevisionForWarningAsync(productId, ct)
            ?? throw AppException.Conflict("Product has no content revision to review.");
        if (revision.Status == ProductReviewStatus.Suspended)
            throw AppException.Conflict("The current revision is suspended.");
        if (revision.Status == ProductReviewStatus.NeedsRevision)
            throw AppException.Conflict("A correction has already been requested for this revision.");

        revision.Status = ProductReviewStatus.NeedsRevision;
        revision.ReviewedByUserId = adminId;
        revision.ReviewedAt = DateTime.UtcNow;
        revision.ReviewNote = requiredCorrection;
        await _warningRepository.SaveChangesAsync(ct);

        var moderationEvent = new ModerationEvent
        {
            Id = Guid.NewGuid(),
            ActorUserId = adminId,
            Action = "SHOP_WARNING_ISSUED",
            TargetType = "Shop",
            TargetId = product.ShopId,
            Reason = reason,
            OldValuesJson = null,
            NewValuesJson = JsonSerializer.Serialize(new
            {
                ProductId = product.Id,
                ProductName = product.Name,
                RevisionId = revision.Id,
                RequiredCorrection = requiredCorrection
            }),
            CreatedAt = DateTime.UtcNow
        };
        await _publisher.PublishAsync(RabbitMqRoutingKeys.ModerationAudit, moderationEvent, ct);

        return new ModerationActionResponse
        {
            TargetId = product.ShopId,
            Action = "SHOP_WARNING_ISSUED",
            Message = "Warning queued for delivery to audit log."
        };
    }
    
    // Map Responses.
    private static WarningResponse Map(AuditLog auditLog)
    {
        return new WarningResponse
        {
            Id = auditLog.Id,
            ShopId = auditLog.TargetId,
            Action = auditLog.Action,
            Reason = auditLog.Reason,
            OldValuesJson = auditLog.OldValuesJson,
            NewValuesJson = auditLog.NewValuesJson,
            IssuedByUserId = auditLog.ActorUserId,
            CreatedAt = auditLog.CreatedAt
        };
    }
}
