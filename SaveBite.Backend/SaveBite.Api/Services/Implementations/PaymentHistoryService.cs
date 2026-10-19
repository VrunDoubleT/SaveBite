using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Expose customer payment history and associated orders without changing payment state.
public sealed class PaymentHistoryService : IPaymentHistoryService
{
    private readonly IPaymentHistoryRepository _payments;
    private readonly ICheckoutRepository _orders;
    private readonly TimeProvider _clock;

    public PaymentHistoryService(IPaymentHistoryRepository payments, ICheckoutRepository orders, TimeProvider clock)
    { _payments = payments; _orders = orders; _clock = clock; }

    // Payment history reads recorded data without creating links or changing provider/payment state.
    public async Task<PagedResult<CustomerPaymentResponse>> PageAsync(Guid userId, int page, int pageSize, string? status, CancellationToken ct = default)
    {
        await _payments.RequireCustomerAsync(userId, ct);
        if (pageSize is < 1 or > 30) throw AppException.BadRequest("Page size must be between 1 and 30.");
        if (page < 1 || (long)(page - 1) * pageSize > int.MaxValue) throw AppException.BadRequest("Invalid page number.");
        PaymentStatus? filter = null;
        if (!string.IsNullOrEmpty(status) && status != "All")
        {
            if (!Enum.TryParse<PaymentStatus>(status, out var parsed) || !Enum.IsDefined(parsed) || parsed.ToString() != status)
                throw AppException.BadRequest("Invalid payment status.");
            filter = parsed;
        }
        var result = await _payments.PageAsync(userId, filter, page, pageSize, ct);
        return PagedResult<CustomerPaymentResponse>.Create(result.Items.Select(Map).ToList(), page, pageSize, result.TotalItems);
    }

    // Recheck order ownership and return the purchased item snapshots alongside this individual payment.
    public async Task<CustomerPaymentDetailResponse> GetAsync(Guid userId, Guid paymentId, CancellationToken ct = default)
    {
        await _payments.RequireCustomerAsync(userId, ct);
        var payment = await _payments.GetAsync(userId, paymentId, ct) ?? throw AppException.NotFound("Payment not found.");
        var order = await _orders.GetOrderAsync(payment.OrderId, userId, ct) ?? throw AppException.NotFound("Order not found.");
        return new CustomerPaymentDetailResponse { Payment = Map(payment), Order = CheckoutOrderMapper.Map(order, now: _clock.GetUtcNow().UtcDateTime) };
    }

    // Map a payment with its order and shop identity without changing payment state.
    private static CustomerPaymentResponse Map(Payment payment) => new()
    {
        Id = payment.Id, OrderId = payment.OrderId, OrderCode = payment.Order.OrderCode,
        ShopName = payment.Order.Shop.Name, ShopLogoUrl = payment.Order.Shop.LogoUrl,
        Type = payment.PaymentType.ToString(), Method = payment.Method.ToString(), Status = payment.Status.ToString(),
        Amount = payment.Amount, CollectedAmount = CheckoutOrderMapper.Collected(payment),
        CreatedAt = payment.CreatedAt, PaidAt = payment.PaidAt
    };
}
