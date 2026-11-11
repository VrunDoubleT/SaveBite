using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Constants;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Remove cart items affected by external deal lifecycle events and notify their owners.
public sealed class CartDealEventService : ICartDealEventService
{
    private readonly AppDbContext _db;
    private readonly ICartRepository _cartRepository;

    public CartDealEventService(ICartRepository cart, AppDbContext db)
    {
        _db = db;
        _cartRepository = cart;
    }

    // The producer owns deal transitions; this handler updates carts without changing stock or orders.
    public async Task HandleAsync(Guid flashDealId, CancellationToken ct = default)
    {
        if (flashDealId == Guid.Empty) throw new ArgumentException("FlashDealId is required.", nameof(flashDealId));
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        await _db.Database.ExecuteSqlInterpolatedAsync($"SELECT id FROM flash_deals WHERE id = {flashDealId} FOR UPDATE", ct);
        var deal = await _db.FlashDeals.AsNoTracking().SingleOrDefaultAsync(d => d.Id == flashDealId, ct);
        if (deal != null && deal.Status is not (FlashDealStatus.Hidden or FlashDealStatus.Closed or FlashDealStatus.Cancelled)
            && deal.OrderEndTime > DateTime.UtcNow) return;
        var owners = await _cartRepository.RemoveFlashDealItemsAsync(flashDealId, ct);
        // Persist recipients before commit. Retries no longer need the deleted cart items.
        foreach (var userId in owners)
            _db.Add(new SaveBite.Backend.Models.Entities.OrderEventOutbox
            {
                Id = Guid.NewGuid(), RoutingKey = "internal.cart.refresh", CreatedAt = DateTime.UtcNow,
                PayloadJson = System.Text.Json.JsonSerializer.Serialize(new SaveBite.Backend.Messaging.Events.CartRefreshEvent(userId))
            });
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}
