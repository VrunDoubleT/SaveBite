using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Common;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Interfaces;

namespace SaveBite.Backend.Services.Implementations;

// Read customer orders and delegate explicit cancellation commands to the payment workflow.
public sealed class OrderService : IOrderService
{
    private readonly ICheckoutRepository _repository;
    private readonly ICheckoutPaymentService _payments;
    private readonly TimeProvider _clock;

    public OrderService(ICheckoutRepository repository, ICheckoutPaymentService payments, TimeProvider clock)
    {
        _repository = repository;
        _payments = payments;
        _clock = clock;
    }

    private DateTime Now => _clock.GetUtcNow().UtcDateTime;

    private sealed record HistoryCursor(Guid UserId, string? Status, DateTime CreatedAt, Guid Id);
    // Validate a customer-specific cursor and return filtered order history with status counts.
    public async Task<OrderHistoryPageResponse> PageAsync(Guid userId, string? status, string? cursor, int pageSize, CancellationToken ct = default)
    {
        if (pageSize is < 1 or > 30) throw AppException.BadRequest("Page size must be between 1 and 30 orders.");
        OrderStatus? filter = null;
        if (!string.IsNullOrEmpty(status) && status != "All")
        {
            if (!Enum.TryParse<OrderStatus>(status, out var parsed) || !Enum.IsDefined(parsed) || parsed.ToString() != status)
                throw AppException.BadRequest("Invalid order status.");
            filter = parsed;
        }
        HistoryCursor? position = null;
        if (cursor != null)
        {
            try
            {
                if (cursor.Length > 1024) throw new FormatException();
                position = JsonSerializer.Deserialize<HistoryCursor>(WebEncoders.Base64UrlDecode(cursor));
                if (position == null || position.UserId != userId || position.Status != filter?.ToString()
                    || position.CreatedAt.Kind != DateTimeKind.Utc || position.Id == Guid.Empty) throw new FormatException();
            }
            catch (Exception ex) when (ex is FormatException or ArgumentException or JsonException)
            { throw AppException.BadRequest("Invalid cursor for this order filter."); }
        }
        var result = await _repository.GetOrderPageAsync(userId, filter, position?.CreatedAt, position?.Id, pageSize, ct);
        var items = result.Orders.Select(order => CheckoutOrderMapper.Map(order, now: Now)).ToList();
        var last = result.Orders.LastOrDefault();
        var next = result.HasMore && last != null ? WebEncoders.Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(new HistoryCursor(userId, filter?.ToString(), last.CreatedAt, last.Id))) : null;
        return new OrderHistoryPageResponse
        {
            Page = CursorPagedResult<CheckoutOrderResponse>.Create(items, next, result.HasMore, pageSize),
            StatusCounts = result.Counts
        };
    }

    // Map the customer's recent orders using recorded prices and current payment summaries.
    public async Task<IReadOnlyList<CheckoutOrderResponse>> ListAsync(Guid userId, CancellationToken ct = default)
        => (await _repository.GetOrdersAsync(userId, ct)).Select(x => CheckoutOrderMapper.Map(x, now: Now)).ToList();

    // Read the customer's recorded order without locks, provider calls or state changes.
    public async Task<CheckoutOrderResponse> GetAsync(Guid userId, Guid orderId, CancellationToken ct = default)
    {
        var order = await _repository.GetOrderAsync(orderId, userId, ct)
            ?? throw AppException.NotFound("Order not found.");
        return CheckoutOrderMapper.Map(order, now: Now);
    }

    // Apply the shared customer cancellation policy and payment reconciliation workflow.
    public Task<CheckoutOrderResponse> CancelAsync(Guid userId, Guid orderId, CancellationToken ct = default)
        => _payments.CancelAsync(userId, orderId, ct);
}
