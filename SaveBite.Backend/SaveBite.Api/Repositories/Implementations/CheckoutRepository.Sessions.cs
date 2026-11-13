using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Helpers;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

namespace SaveBite.Backend.Repositories.Implementations;

public sealed partial class CheckoutRepository
{
    private readonly Dictionary<Guid, (CheckoutSession Session, Order Order)> _sessions = new();

    private void AddSession(Order order)
    {
        var session = new CheckoutSession
        {
            Id = order.Id, CustomerId = order.CustomerId, ShopId = order.ShopId,
            QuoteId = order.CheckoutQuoteId!.Value, RequestId = order.CheckoutRequestId!.Value,
            Status = order.Status, PaymentExpiresAt = order.PaymentExpiresAt!.Value, CreatedAt = order.CreatedAt,
            SnapshotJson = CheckoutSessionSnapshot.Serialize(order)
        };
        _dbContext.CheckoutSessions.Add(session);
        foreach (var payment in order.Payments)
        {
            payment.OrderId = null;
            payment.CheckoutSessionId = session.Id;
            _dbContext.Payments.Add(payment);
        }
        _sessions[session.Id] = (session, order);
    }

    private async Task<Order?> FindSessionRequestAsync(Guid userId, Guid requestId, Guid quoteId, CancellationToken ct)
    {
        var sessions = await _dbContext.CheckoutSessions.Where(s => s.CustomerId == userId
            && (s.RequestId == requestId || s.QuoteId == quoteId)).Take(2).ToListAsync(ct);
        if (sessions.Count > 1) throw AppException.Conflict("This request ID belongs to a different checkout.");
        return sessions.Count == 0 ? null : await LoadSessionAsync(sessions[0].Id, userId, ct);
    }

    private async Task<Order?> LoadSessionAsync(Guid id, Guid? userId, CancellationToken ct)
    {
        if (_sessions.TryGetValue(id, out var cached))
            return !userId.HasValue || cached.Order.CustomerId == userId ? cached.Order : null;
        var session = await _dbContext.CheckoutSessions.SingleOrDefaultAsync(s => s.Id == id
            && (!userId.HasValue || s.CustomerId == userId), ct);
        if (session == null) return null;
        var order = CheckoutSessionSnapshot.Read(session.SnapshotJson);
        order.Shop = await _dbContext.Shops.Include(s => s.PaymentConfig).SingleAsync(s => s.Id == session.ShopId, ct);
        order.Customer = await _dbContext.Users.SingleAsync(u => u.Id == session.CustomerId, ct);
        order.Payments = await _dbContext.Payments.Where(p => p.CheckoutSessionId == id).ToListAsync(ct);
        _sessions[id] = (session, order);
        return order;
    }

    private void PersistSessions()
    {
        foreach (var (session, order) in _sessions.Values)
        {
            // Full/deposit verification creates the order exactly once. Failed attempts stay sessions.
            if (order.Status == OrderStatus.Pending && order.Payments.Any(p => p.Status == PaymentStatus.Succeeded)
                && order.Payments.All(p => p.OrderId == null))
            {
                foreach (var payment in order.Payments) payment.OrderId = order.Id;
                _dbContext.Orders.Add(order);
            }
            session.Status = order.Status;
            session.PaymentExpiresAt = order.PaymentExpiresAt ?? session.PaymentExpiresAt;
            session.SnapshotJson = CheckoutSessionSnapshot.Serialize(order);
        }
    }

    public async Task RestoreCartAsync(Order order, CancellationToken ct)
    {
        foreach (var item in order.Items.OrderBy(i => i.FlashDealVariantId))
        {
            // Customer row is locked by WithOrderLockAsync, serializing this with cart edits/checkout.
            var existing = await _dbContext.CartItems.SingleOrDefaultAsync(c => c.UserId == order.CustomerId
                && c.FlashDealVariantId == item.FlashDealVariantId, ct);
            if (existing == null)
                _dbContext.CartItems.Add(new CartItem { Id = Guid.NewGuid(), UserId = order.CustomerId,
                    FlashDealVariantId = item.FlashDealVariantId, Quantity = item.Quantity, AddedAt = DateTime.UtcNow });
            else existing.Quantity = checked(existing.Quantity + item.Quantity);
        }
        cartChanged = true;
    }
}
