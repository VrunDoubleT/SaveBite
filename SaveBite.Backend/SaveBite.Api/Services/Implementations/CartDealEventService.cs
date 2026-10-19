using SaveBite.Backend.Constants;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Remove cart items affected by external deal lifecycle events and notify their owners.
public sealed class CartDealEventService : ICartDealEventService
{
    private readonly ICartRepository _cartRepository;
    private readonly IRealtimePublisher _realtimePublisher;

    public CartDealEventService(ICartRepository cart, IRealtimePublisher realtime)
    {
        _cartRepository = cart;
        _realtimePublisher = realtime;
    }

    // The producer owns deal transitions; this handler updates carts without changing stock or orders.
    public async Task HandleAsync(Guid flashDealId, CancellationToken ct = default)
    {
        if (flashDealId == Guid.Empty) throw new ArgumentException("FlashDealId is required.", nameof(flashDealId));
        var owners = await _cartRepository.RemoveFlashDealItemsAsync(flashDealId, ct);
        // DELETE has committed before publishing; repeated events find no rows and have no additional effect.
        foreach (var userId in owners)
            await _realtimePublisher.SendToUserAsync(userId, RealtimeEvents.CartChanged, new CartChangedEvent(RealtimeEvents.CartReasons.DealUnavailable));
    }
}
