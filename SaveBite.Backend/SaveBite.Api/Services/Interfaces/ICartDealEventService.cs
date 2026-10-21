namespace SaveBite.Backend.Services.Interfaces;

// Apply external deal closure events to affected customer carts.
public interface ICartDealEventService
{
    Task HandleAsync(Guid flashDealId, CancellationToken ct = default);
}
