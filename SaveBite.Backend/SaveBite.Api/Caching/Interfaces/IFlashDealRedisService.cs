using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Caching.Interfaces;

public interface IFlashDealRedisService
{
    Task<FlashDealResponse?> GetDealAsync(Guid  dealId);
    Task<List<FlashDealResponse>> GetDealsByShopAsync(Guid shopId);
    Task<List<FlashDealResponse>> GetNearbyDealsAsync(double latitude, double longitude, double radius);
    Task SaveDealAsync(FlashDealResponse deal, double shopLat, double shopLon, TimeSpan? expiration = null);
    Task<bool> DealExistsAsync(Guid dealId);
    Task DeleteDealAsync(Guid dealId, Guid shopId);
}