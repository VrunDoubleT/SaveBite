namespace SaveBite.Backend.Services.Interfaces;

public interface ITrustNotificationService
{
    Task PublishForOrderAsync(Guid orderId, CancellationToken ct);
}
