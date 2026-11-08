namespace SaveBite.Backend.Constants;

public static class NotificationMessages
{
    public static class Orders
    {
        public const string NewOrderAwaitingConfirmationTitle = "New Order Awaiting Confirmation";
        public const string NewOrderAwaitingConfirmationBody = "A customer has placed a new order. Please confirm it within 5 minutes.";

        public const string OrderConfirmedTitle = "Order Confirmed";
        public const string OrderConfirmedBody = "The store has accepted your order. Open order details to follow its progress.";

        public const string OrderRejectedTitle = "Order Rejected";
        public const string OrderRejectedBody = "The store could not accept your order. Please check your order details.";

        public const string ReadyForPickupTitle = "Your Order is Ready for Pickup!";
        public const string ReadyForPickupBody = "Please visit the store to pick up your order.";

        public const string OrderCompletedTitle = "Order Completed";
        public const string OrderCompletedBody = "Thank you for using SaveBite! Feel free to leave a review for your meal.";

        public const string OrderCancelledTitle = "Order cancelled";
        public static string OrderCancelledBody(string orderCode) => $"The customer cancelled order {orderCode}.";

        public const string OrderPaymentExpiredTitle = "Order payment expired";
        public static string OrderPaymentExpiredBody(string orderCode) => $"The payment window for order {orderCode} has expired.";

        public const string OrderNoShowTitle = "Order Pickup Missed";
        public const string OrderNoShowBody = "You did not pick up your order before the deadline.";

        public const string PickupDeadlineApproachingTitle = "Pickup deadline approaching";
        public static string PickupDeadlineApproachingBody(string orderCode, string shopName, DateTime deadlineUtcPlus7, string shopAddress) =>
            $"Order {orderCode} at {shopName} must be collected before {deadlineUtcPlus7:dd/MM/yyyy HH:mm} (UTC+7). Address: {shopAddress}. Please collect it before this hard deadline to avoid a missed pickup.";
    }

    public static class Payments
    {
        public const string PaymentSucceededTitle = "Payment Succeeded";
        public const string PaymentSucceededAwaitingConfirmationBody = "Your payment was successful and your order is awaiting store confirmation.";
        public const string RemainingPaymentVerifiedBody = "The remaining payment for your order has been verified.";

        public const string PaymentFailedTitle = "Payment Failed";
        public static string PaymentFailedBody(string? reason = null) =>
            string.IsNullOrWhiteSpace(reason) ? "Your payment could not be processed." : $"Payment failed: {reason.Trim()}";
    }

    public static class Refunds
    {
        public const string NewRefundRequestTitle = "New refund request";
        public static string NewRefundRequestBody(string customerName, string orderCode) =>
            $"{customerName} requested a refund for order {orderCode}.";

        public const string RefundStatusUpdatedTitle = "Refund status updated";
        public static string RefundStatusUpdatedBody(string orderCode, object status, string? rejectReason = null) =>
            $"Refund for order {orderCode}: {status}." + (string.IsNullOrWhiteSpace(rejectReason) ? "" : $" {rejectReason.Trim()}");
    }

    public static class Staff
    {
        public const string StaffInvitationReceivedTitle = "New staff invitation";
        public static string StaffInvitationReceivedBody(string shopName) =>
            $"{shopName} invited you to join their team. Respond within 3 days.";

        public const string StaffInvitationAcceptedTitle = "Staff invitation accepted";
        public static string StaffInvitationAcceptedBody(string customerName, string shopName) =>
            $"{customerName} accepted your invitation and joined {shopName}.";

        public const string StaffJoinedShopTitle = "Welcome to the team";
        public static string StaffJoinedShopBody(string shopName) =>
            $"You have joined {shopName} as a staff member.";

        public const string StaffInvitationDeclinedTitle = "Staff invitation declined";
        public static string StaffInvitationDeclinedBody(string customerName, string shopName) =>
            $"{customerName} declined your invitation to join {shopName}.";

        public const string StaffInvitationExpiredTitle = "Staff invitation expired";
        public static string StaffInvitationExpiredOwnerBody(string customerName, string shopName) =>
            $"The invitation for {customerName} to join {shopName} expired without a response.";
        public static string StaffInvitationExpiredCustomerBody(string shopName) =>
            $"Your invitation to join {shopName} has expired.";

        public const string StaffStatusUpdatedTitle = "Staff access updated";
        public static string StaffStatusUpdatedBody(string shopName, object status) =>
            $"Your staff access at {shopName} is now {status}.";

        public const string StaffMembershipRemovedTitle = "Staff membership removed";
        public static string StaffMembershipRemovedBody(string shopName) =>
            $"Your staff membership at {shopName} has been removed.";

        public const string StaffLeftShopTitle = "A staff member left your shop";
        public static string StaffLeftShopBody(string staffDisplayName, string shopName) =>
            $"{staffDisplayName} has left {shopName}.";
    }

    public static class ShopApplications
    {
        public const string NewApplicationSubmittedTitle = "New shop registration application";
        public static string NewApplicationSubmittedBody(string shopName) =>
            $"A new shop registration application has been submitted for '{shopName}'. Please review the application.";

        public const string ApplicationResubmittedTitle = "Shop registration application resubmitted";
        public static string ApplicationResubmittedBody(string shopName) =>
            $"Shop registration application for '{shopName}' has been resubmitted for review.";

        public const string ApplicationReviewedTitle = "Shop application reviewed";
        public static string ApplicationReviewedBody(string shopName, object nextStatus, string? note = null) =>
            $"Your application for {shopName} is now {nextStatus}." + (string.IsNullOrWhiteSpace(note) ? "" : $" {note.Trim()}");
    }

    public static class Shop
    {
        public const string ShopStatusUpdatedTitle = "Shop status updated";
        public static string ShopStatusUpdatedBody(string shopName, object currentStatus, object targetStatus, string? reason = null) =>
            $"{shopName}: {currentStatus} → {targetStatus}." + (string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason.Trim()}");
    }

    public static class DealsAndProducts
    {
        public static string FlashDealAvailableTitle(string shopName) => $"New Flash Deal from {shopName}!";
        public static string FlashDealAvailableBody(string productName) => $"'{productName}' is now on sale. Order now before it runs out!";

        public const string ProductCorrectionRequestedTitle = "Product correction requested";
        public static string ProductCorrectionRequestedBody(string productName, string? reason, string? requiredCorrection) =>
            $"{productName}: {reason} Required correction: {requiredCorrection}. Submit corrections within 7 days.";

        public const string ProductContentApprovedTitle = "Product content approved";
        public static string ProductContentApprovedBody(string productName) =>
            $"The content of {productName} has been approved by an administrator.";

        public const string ProductRemovedViolationTitle = "Product removed for a violation";
        public static string ProductRemovedViolationBody(string productName, string? reason) =>
            $"{productName} was removed by an administrator." + (string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason.Trim()}");
    }

    public static class Feedback
    {
        public const string ReviewRemovedByAdminTitle = "Your review was removed";
        public static string ReviewRemovedByAdminBody(string productName, string? reason) =>
            $"An administrator removed your review of {productName}." + (string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason.Trim()}");

        public const string ReviewRestoredByAdminTitle = "Your review was restored";
        public static string ReviewRestoredByAdminBody(string productName) =>
            $"An administrator restored your review of {productName}.";

        public const string NewCustomerReviewTitle = "New customer review";
        public static string NewCustomerReviewBody(string customerName, int rating, string productName) =>
            $"{customerName} left a {rating}-star review for {productName}.";

        public const string ReviewUpdatedLowRatingTitle = "Customer updated review (low rating)";
        public static string ReviewUpdatedLowRatingBody(string customerName, int rating, string productName) =>
            $"{customerName} updated their review to {rating} stars for {productName}.";

        public const string ReviewRepliedTitle = "Your review has a reply";
        public static string ReviewRepliedBody(string shopName, string productName) =>
            $"{shopName} replied to your review of {productName}.";

        public const string FeedbackReplyRemovedTitle = "Feedback reply removed";
        public static string FeedbackReplyRemovedBody(string ownerName) =>
            $"{ownerName} removed your reply on a customer review.";
    }

    public static class TrustAndPenalties
    {
        public const string TrustScoreUpdatedTitle = "Trust score updated";
        public static string TrustScoreUpdatedBody(int scoreBefore, int scoreAfter, string? reason = null) =>
            $"Your trust score changed from {scoreBefore} to {scoreAfter}." + (string.IsNullOrWhiteSpace(reason) ? "" : $" {reason.Trim()}");

        public const string MissedPickupPenaltyTitle = "Missed pickup penalty";
        public static string MissedPickupPenaltyBody(int scoreDeducted) =>
            $"A missed pickup penalty was recorded. Score deducted: {scoreDeducted}. Check your recovery requirements.";

        public const string TrustLevelUpdatedTitle = "Trust level updated";
        public static string TrustLevelUpdatedBody(string levelName) =>
            $"Your trust level is now {levelName}.";
        public static string TrustLevelUpdatedFromConfigBody(string levelName) =>
            $"Your trust level is now {levelName} after an administrator updated the level configuration.";
    }

    public static class Accounts
    {
        public const string CustomerAccessUpdatedTitle = "Customer access updated";
        public static string CustomerAccessUpdatedBody(bool isSuspended, string? reason = null) =>
            $"An administrator {(isSuspended ? "suspended" : "reactivated")} your customer features." +
            (string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason.Trim()}");

        public const string AccountAccessUpdatedTitle = "Account access updated";
        public static string AccountAccessUpdatedBody(bool isSuspended, string? reason = null) =>
            $"An administrator {(isSuspended ? "suspended" : "reactivated")} your account." +
            (string.IsNullOrWhiteSpace(reason) ? "" : $" Reason: {reason.Trim()}");
    }

    public static class PlatformFees
    {
        public const string PaymentReceivedTitle = "Platform fee payment received";
        public static string PaymentReceivedBody(string shopName, short month, int year) =>
            $"Payment for {shopName}, period {month:D2}/{year}, has been recorded.";

        public const string OverdueTitle = "Platform fee overdue";
        public static string OverdueBody(string shopName, decimal totalFeeAmount, DateTime? dueAtUtc) =>
            $"{shopName}: {totalFeeAmount:N0} VND is due on {dueAtUtc:dd/MM/yyyy HH:mm} UTC. Please settle the statement to restore shop access.";

        public const string DueSoonTitle = "Platform fee due within 24 hours";
        public static string DueSoonBody(string shopName, decimal totalFeeAmount, DateTime? dueAtUtc) =>
            $"{shopName}: {totalFeeAmount:N0} VND is due on {dueAtUtc:dd/MM/yyyy HH:mm} UTC. Please pay before the deadline.";

        public const string MonthlyStatementIssuedTitle = "Monthly platform fee statement";
        public static string MonthlyStatementIssuedBody(string shopName, short month, int year, string paymentInfo) =>
            $"{shopName}: your platform fee statement for {month:D2}/{year} is available. {paymentInfo}";

        public const string PaymentNotRequired = "No payment is required.";
        public static string PaymentRequiredInfo(decimal totalFeeAmount, DateTime? dueAt) =>
            $"Amount: {totalFeeAmount:N0} VND." + (dueAt.HasValue ? $" Due: {dueAt.Value:dd/MM/yyyy}." : "");
    }
}
