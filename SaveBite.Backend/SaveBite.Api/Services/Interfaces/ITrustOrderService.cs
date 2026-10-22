namespace SaveBite.Backend.Services.Interfaces;

// Apply terminal-order trust effects once using authoritative order state.
public interface ITrustOrderService
{
    Task HandleAsync(Guid orderId, bool completed, CancellationToken ct = default);
}
