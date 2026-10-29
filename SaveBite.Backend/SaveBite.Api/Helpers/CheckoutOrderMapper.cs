using System.Text.Json;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;

namespace SaveBite.Backend.Helpers;

// Map historical order data and verified payment totals into customer responses.
public static class CheckoutOrderMapper
{
    // Return the verified amount collected without treating unpaid amounts as received.
    public static decimal Collected(Payment payment) => Math.Max(payment.CollectedAmount, payment.Status == PaymentStatus.Succeeded ? payment.Amount : 0);
    // Select the initial full payment or deposit required to submit the order.
    public static Payment? RequiredPayment(Order order) => order.Payments
        .Where(x => x.PaymentType is PaymentType.Full or PaymentType.Deposit)
        .OrderByDescending(x => x.CreatedAt).FirstOrDefault();
        
    // Prefer a settled receipt or the active attempt while retaining closed attempts for payment history.
    public static Payment? RemainingPayment(Order order) => order.Payments.Where(x => x.PaymentType == PaymentType.Remaining)
        .OrderByDescending(x => x.Status == PaymentStatus.Succeeded)
        .ThenByDescending(x => x.Status == PaymentStatus.Pending)
        .ThenByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).FirstOrDefault();

    // Build the order response from recorded purchase data and current payment totals.
    public static CheckoutOrderResponse Map(Order order, string? message = null, DateTime? now = null)
    {
        var payment = RequiredPayment(order);
        var policy = order.CheckoutPolicySnapshotJson == null ? null : JsonSerializer.Deserialize<CheckoutPolicySnapshot>(order.CheckoutPolicySnapshotJson);
        var cancellationReason = CheckoutRules.CancellationUnavailableReason(order, now ?? DateTime.UtcNow);
        var paidAmount = order.Payments.Sum(Collected);
        var outstandingAmount = order.Status is OrderStatus.Cancelled or OrderStatus.Expired or OrderStatus.NoShow or OrderStatus.Rejected
            ? 0 : Math.Max(0, order.TotalAmount - paidAmount);
        return new CheckoutOrderResponse
        {
            Id = order.Id,
            OrderCode = order.OrderCode,
            ShopId = order.ShopId,
            ShopName = order.Shop.Name,
            PaymentMethod = order.PaymentMethod.ToString(),
            Status = order.Status.ToString(),
            PaymentStatus = payment?.Status.ToString() ?? "Unknown",
            TotalAmount = order.TotalAmount,
            DepositAmount = order.DepositAmount,
            RequiredOnlineAmount = order.CheckoutQuoteId == null
                ? payment?.Method == PaymentMethod.OnlinePayment ? payment.Amount : 0
                : order.RequiredOnlineAmount,
            RemainingAtShop = order.PaymentMethod == PaymentMethod.PayAtShop
                && order.Status is not (OrderStatus.Cancelled or OrderStatus.Rejected or OrderStatus.Expired or OrderStatus.NoShow)
                ? Math.Max(0, order.RemainingAmount - order.Payments.Where(x => x.PaymentType == PaymentType.Remaining && x.Status == PaymentStatus.Succeeded).Sum(x => x.Amount))
                : 0,
            SoftDeadlineAt = order.SoftDeadlineAt,
            HardDeadlineAt = order.HardDeadlineAt,
            PaymentExpiresAt = order.PaymentExpiresAt,
            PaymentReviewRequired = order.PaymentReviewRequired,
            CheckoutUrl = payment?.Status == PaymentStatus.Pending && order.Status == OrderStatus.AwaitingPayment
                ? payment.GatewayCheckoutUrl
                : null,
            Policy = policy,
            Items = order.Items.Select(x => new CheckoutOrderItemResponse
            {
                FlashDealVariantId = x.FlashDealVariantId,
                ProductName = x.ProductNameSnapshot,
                VariantName = x.VariantNameSnapshot,
                UnitPrice = x.UnitPrice,
                Quantity = x.Quantity,
                Subtotal = x.Subtotal ?? x.UnitPrice * x.Quantity,
                ImageUrl = x.FlashDealVariant?.FlashDeal?.Product?.Images.OrderBy(image => image.Id).Select(image => image.ImageUrl).FirstOrDefault(),
                FlashDealId = x.FlashDealVariant?.FlashDealId
            }).ToList(),
            PaymentMessage = message,
            CanCancel = cancellationReason == null,
            CancellationUnavailableReason = cancellationReason,
            CancellationPolicy = CheckoutRules.CustomerCancellationPolicy,
            ShopLogoUrl = order.Shop.LogoUrl,
            ShopAddress = string.Join(", ", new[] { order.Shop.AddressLine, order.Shop.Ward, order.Shop.District, order.Shop.City }
                .Where(value => !string.IsNullOrWhiteSpace(value))),
            CreatedAt = order.CreatedAt,
            CompletedAt = order.CompletedAt,
            PaidAmount = paidAmount,
            OutstandingAmount = outstandingAmount
        };
    }
}
