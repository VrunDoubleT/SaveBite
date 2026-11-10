using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Helpers;

public static class PurchaseSnapshots
{
    public static string ShopName(Order order) => order.ShopNameSnapshot ?? order.Shop.Name;
    public static string? ShopLogo(Order order) => order.SnapshotCapturedAt.HasValue ? order.ShopLogoUrlSnapshot : order.Shop.LogoUrl;
    public static string ShopAddress(Order order) => order.ShopAddressSnapshot ?? string.Join(", ",
        new[] { order.Shop.AddressLine, order.Shop.Ward, order.Shop.District, order.Shop.City }.Where(v => !string.IsNullOrWhiteSpace(v)));
    public static string? ProductImage(OrderItem item, Product? legacyProduct = null) => item.SnapshotCapturedAt.HasValue ? item.ProductImageUrlSnapshot
        : (legacyProduct ?? item.FlashDealVariant?.FlashDeal?.Product)?.Images.OrderBy(i => i.Id).Select(i => i.ImageUrl).FirstOrDefault();
}
