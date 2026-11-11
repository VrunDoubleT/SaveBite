namespace SaveBite.Backend.Services.Interfaces;

public interface IShopStatusEventService
{
    Task NotifyChangedAsync(Guid shopId, string status, CancellationToken cancellationToken = default);
}
