namespace SaveBite.Backend.Constants;

public static class NotificationActionUrls
{
    public static string CustomerOrder(Guid orderId) => $"/orders/{orderId}";

    public static string StoreOwnerOrder(Guid orderId) => $"/store-owner/orders/{orderId}";

    public static string ShopStaffOrder(Guid shopId, Guid orderId) => $"/staff/shops/{shopId}/orders/{orderId}";

    public static string FlashDeal(Guid dealId) => $"/flash-deals/{dealId}";
}
