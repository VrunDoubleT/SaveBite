using SaveBite.Backend.Helpers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
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
    private readonly AppDbContext _db;
    private readonly TimeProvider _clock;

    public WarningService(IWarningRepository warningRepository, AppDbContext db, TimeProvider clock)
    {
        _warningRepository = warningRepository;
        _db = db;
        _clock = clock;
    }

    private async Task<Guid> GetShopIdAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        var shopId = await _warningRepository.GetOwnedShopIdAsync(ownerId, cancellationToken);
        if (!shopId.HasValue)
            throw AppException.NotFound("Shop not found.");

        return shopId.Value;
    }

    // View Warning History.
    public async Task<PagedResult<WarningResponse>> GetHistoryAsync(Guid ownerId, int page, int pageSize, CancellationToken cancellationToken, string? status = null)
    {
        if (status is not (null or "all" or "new" or "submitted" or "resolved"))
            throw AppException.BadRequest("Invalid warning status filter.");
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var shopId = await GetShopIdAsync(ownerId, cancellationToken);
        var (items, total) = await _warningRepository.GetHistoryAsync(shopId, page, pageSize, cancellationToken, status);
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

        if (await _db.ProductWarnings.AnyAsync(x => x.ProductId == productId && x.ResolvedAt == null, ct))
            throw AppException.Conflict("This product already has an unresolved warning.");

        var now = _clock.GetUtcNow().UtcDateTime;
        revision.AttributeValuesJson ??= JsonSerializer.Serialize(ProductAttributeContentHelper.Capture(product));
        revision.Status = ProductReviewStatus.NeedsRevision;
        revision.ReviewedByUserId = adminId;
        revision.ReviewedAt = now;
        revision.ReviewNote = requiredCorrection;
        _db.ProductWarnings.Add(new ProductWarning
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ShopId = product.ShopId,
            IssuedByUserId = adminId,
            BaselineRevisionId = revision.Id,
            ProductName = product.Name,
            Reason = reason,
            RequiredCorrection = requiredCorrection,
            CreatedAt = now,
            DueAt = now.AddDays(7)
        });
        await _db.SaveChangesAsync(ct);

        return new ModerationActionResponse
        {
            TargetId = product.ShopId,
            Action = "SHOP_WARNING_ISSUED",
            Message = "Warning saved. The shop has seven days to submit corrections."
        };
    }

    // Map Responses.
    private WarningResponse Map(ProductWarning warning)
    {
        return new WarningResponse
        {
            Id = warning.Id,
            ProductId = warning.ProductId,
            ProductName = warning.ProductName,
            RequiredCorrection = warning.RequiredCorrection,
            ShopId = warning.ShopId,
            Action = "SHOP_WARNING_ISSUED",
            Reason = warning.Reason,
            NewValuesJson = JsonSerializer.Serialize(new { warning.ProductId, warning.ProductName, warning.RequiredCorrection }),
            IssuedByUserId = warning.IssuedByUserId,
            CreatedAt = warning.CreatedAt,
            DueAt = warning.DueAt,
            FirstEditedAt = warning.FirstEditedAt,
            ResolvedAt = warning.ResolvedAt,
            DealBlocked = warning.IsDealBlocked(_clock.GetUtcNow().UtcDateTime)
        };
    }
}
