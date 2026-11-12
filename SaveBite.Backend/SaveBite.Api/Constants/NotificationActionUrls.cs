namespace SaveBite.Backend.Constants;

public static class NotificationActionUrls
{
    // Orders
    public static string CustomerOrder(Guid orderId) => $"/orders/{orderId}";
    public static string CustomerOrders() => "/orders";
    public static string StoreOwnerOrder(Guid orderId) => $"/store-owner/orders/{orderId}";
    public static string StoreOwnerOrders() => "/store-owner/orders";
    public static string ShopStaffOrder(Guid shopId, Guid orderId) => $"/staff/shops/{shopId}/orders/{orderId}";
    public static string ShopStaffOrders(Guid shopId) => $"/staff/shops/{shopId}/orders";

    // Refunds
    public static string CustomerRefund(Guid refundId) => $"/account/refunds/{refundId}";
    public static string CustomerRefunds() => "/account/refunds";
    public static string StoreOwnerRefund(Guid refundId) => $"/store-owner/refunds/{refundId}";
    public static string StoreOwnerRefunds(string? status = null) =>
        string.IsNullOrEmpty(status) ? "/store-owner/refunds" : $"/store-owner/refunds?status={status}";

    // Deals & Products
    public static string FlashDeal(Guid dealId) => $"/flash-deals/{dealId}";
    public static string StoreOwnerProducts() => "/store-owner/products";
    public static string StoreOwnerProduct(Guid productId) => $"/store-owner/products/{productId}";
    public static string StoreOwnerWarnings() => "/store-owner/warnings";

    // Shop Applications
    public static string AdminShopApplications() => "/admin/shop-applications";
    public static string CustomerShopApplication() => "/account/shop-application";

    // Reviews & Feedback
    public static string CustomerReviews(Guid? feedbackId = null) =>
        feedbackId.HasValue ? $"/account/reviews?feedbackId={feedbackId.Value}" : "/account/reviews";
    public static string StoreOwnerReviews() => "/store-owner/reviews";
    public static string ShopStaffReviews(Guid? shopId = null) =>
        shopId.HasValue ? $"/staff/shops/{shopId.Value}/reviews" : "/staff/reviews";

    // Platform Fees
    public static string AdminPlatformFees() => "/admin/platform-fees";
    public static string StoreOwnerPlatformFees() => "/store-owner/platform-fees";
    public static string StoreOwnerPlatformFee(Guid statementId) => $"/store-owner/platform-fees/{statementId}";

    // Shop Settings & Profile
    public static string Shop(Guid shopId) => $"/shops/{shopId}";
    public static string StoreOwnerProfile() => "/store-owner/profile";
    public static string StoreOwnerPaymentSettings() => "/store-owner/payment-settings";

    // Staff & Workspace
    public static string StaffWorkspace() => "/account/workspace";
    public static string StoreOwnerStaff() => "/store-owner/staff";
    public static string CustomerStaffInvitations() => "/account/staff-invitations";
    public static string StoreOwnerStaffInvitations() => "/store-owner/staff/invitations";

    // Trust Score & Account
    public static string CustomerTrustScore() => "/account/trust-score";
    public static string CustomerTrustScorePenalties() => "/account/trust-score/penalties";
    public static string CustomerProfile() => "/account/profile";
    public static string Login() => "/login";
}
