using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;

internal static class CustomerUseCaseData
{
    private static Guid Id(int kind, int index) => Guid.Parse($"c0500000-0000-0000-{kind:0000}-{index:000000000000}");
    public static async Task ApplyAsync(AppDbContext db)
    {
        var now = DateTime.UtcNow;
        var customer = await db.Users.SingleAsync(x => x.Email == "customer@gmail.com");
        var templateId = Guid.Parse("00000000-0000-0000-0000-000000000202");
        var template = await db.Orders.AsNoTracking().SingleAsync(x => x.Id == templateId);
        var items = await db.Set<OrderItem>().AsNoTracking().Where(x => x.OrderId == templateId).ToListAsync();
        var payment = await db.Set<Payment>().AsNoTracking().FirstAsync(x => x.OrderId == templateId);
        for (var i = 1; i <= 18; i++)
        {
            var orderId = Id(1, i);
            if (await db.Orders.AnyAsync(x => x.Id == orderId)) continue;
            var order = new Order(); db.Entry(order).CurrentValues.SetValues(template);
            order.Id = orderId; order.OrderCode = $"SB-DEMO-{i:0000}";
            order.CreatedAt = now.AddMinutes(-90 - i); order.CompletedAt = now.AddMinutes(-15 - i);
            order.ConfirmedAt = now.AddMinutes(-70 - i); order.ReadyAt = now.AddMinutes(-50 - i);
            order.SoftDeadlineAt = now.AddMinutes(30); order.HardDeadlineAt = now.AddMinutes(60);
            order.UpdatedAt = now; order.IsPlatformFeeExempt = true;
            db.Add(order);
            for (var j = 0; j < items.Count; j++)
            {
                var item = new OrderItem(); db.Entry(item).CurrentValues.SetValues(items[j]);
                item.Id = Id(2, i * 10 + j); item.OrderId = orderId; db.Add(item);
            }
            var paid = new Payment(); db.Entry(paid).CurrentValues.SetValues(payment);
            paid.Id = Id(3, i); paid.OrderId = orderId; paid.CollectedAmount = paid.Amount;
            paid.GatewayTransactionId = $"DEMO-PAID-{i:0000}";
            paid.PaidAt = order.CreatedAt.AddMinutes(2); paid.CreatedAt = order.CreatedAt; paid.UpdatedAt = order.CreatedAt.AddMinutes(2);
            db.Add(paid);
            if (i <= 8)
            {
                var refund = new Refund {
                    Id = Id(4,i), OrderId = orderId, InitiatedBy = customer.Id,
                    RefundType = RefundType.CustomerRequest,
                    Status = i == 1 ? RefundStatus.AwaitingCustomerDetails : i == 2 ? RefundStatus.AwaitingTransfer : i % 2 == 0 ? RefundStatus.Completed : RefundStatus.Rejected,
                    Reason = "One item arrived with damaged packaging.", RequestedAmount = 20000,
                    CustomerDetailsDeadlineAt = now.AddHours(2), ReviewDeadlineAt = now.AddHours(3),
                    CreatedAt = now.AddMinutes(-i), UpdatedAt = now,
                    ReceiverMethod = i == 1 ? null : RefundReceiverMethod.BankAccount,
                    ReceiverBankBin = i == 1 ? null : "970407", ReceiverBankName = i == 1 ? null : "Techcombank",
                    ReceiverAccountNumber = i == 1 ? null : "1900000123456", ReceiverAccountHolder = i == 1 ? null : "ALEX MORGAN",
                    DestinationConfirmedAt = i == 1 ? null : now.AddMinutes(-5),
                    TransferredAmount = i > 2 && i % 2 == 0 ? 20000 : 0,
                    TransferredAt = i > 2 && i % 2 == 0 ? now.AddMinutes(-2) : null,
                    RejectReason = i > 2 && i % 2 != 0 ? "The evidence does not support the requested refund." : null
                };
                db.Add(refund);
                db.Add(new RefundItem { Id = Id(5,i), RefundId = refund.Id, OrderItemId = Id(2,i*10+1), ProductName = items[1].ProductNameSnapshot, VariantName = "Regular portion", PurchasedQuantity = 2, Quantity = 1, UnitPrice = 20000, Amount = 20000, Reason = refund.Reason });
            }
        }
        var titles = new[] { "Your order is ready for pickup", "Payment received", "New flash deals near you", "Refund update", "Your review received a reply", "Trust score updated", "A store you follow has new deals", "Order completed", "Pickup reminder", "New seasonal menu", "Your receipt is available", "Membership progress updated" };
        for (var i = 0; i < titles.Length; i++)
            if (!await db.Set<Notification>().AnyAsync(x => x.Id == Id(6,i)))
                db.Add(new Notification { Id = Id(6,i), UserId = customer.Id, Type = NotificationType.FlashDealAvailable, Title = titles[i], Body = "Check your account for the latest details and available offers.", ActionUrl = "/account/orders", CreatedAt = now.AddMinutes(-i-1) });
        foreach (var feedback in await db.Set<ProductFeedback>().ToListAsync())
            feedback.Comment = "The food was fresh, well packaged, and ready on time. Great value for money.";
        foreach (var reply in await db.Set<ProductFeedbackReply>().ToListAsync())
            reply.Content = "Thank you for your feedback. We look forward to serving you again.";
        foreach (var value in await db.Set<ProductAttributeValue>().ToListAsync()) value.Value = value.SortOrder == 0 ? "Regular" : "Large";
        foreach (var notification in await db.Set<Notification>().Where(x => x.UserId == customer.Id).ToListAsync())
        {
            notification.Title = titles[Math.Abs(notification.Id.GetHashCode() % titles.Length)];
            notification.Body = "Check your account for the latest details and available offers.";
        }
        var history = await db.Set<TrustProgressHistory>().AsNoTracking().FirstOrDefaultAsync();
        var level = await db.TrustLevels.OrderBy(x => x.Rank).Skip(2).FirstAsync();
        if (!await db.UserTrustScores.AnyAsync(x => x.UserId == customer.Id))
            db.Add(new UserTrustScore { Id = Id(8,1), UserId = customer.Id, TrustLevelId = level.Id, CurrentScore = 93, CreatedAt = now.AddMonths(-3), UpdatedAt = now });
        if (!await db.Set<UserLevelProgress>().AnyAsync(x => x.UserId == customer.Id))
            db.Add(new UserLevelProgress { Id = Id(8,2), UserId = customer.Id, CompletedOrdersCount = 18, AccumulatedOrderValue = 1350000, CreatedAt = now.AddMonths(-3), UpdatedAt = now });
        foreach (var trustLevel in await db.TrustLevels.ToListAsync()) trustLevel.Description = $"Earn {trustLevel.Name} membership by completing orders and maintaining a good trust score.";
        if (history != null)
            for (var i = 1; i <= 12; i++)
                if (!await db.Set<TrustProgressHistory>().AnyAsync(x => x.Id == Id(7,i)))
                {
                    var entry = new TrustProgressHistory(); db.Entry(entry).CurrentValues.SetValues(history);
                    entry.Id = Id(7,i); entry.UserId = customer.Id; entry.OrderId = Id(1,i); entry.RuleId = null; entry.NoShowPenaltyId = null;
                    entry.ScoreBefore = 80+i; entry.ScoreDelta = 1; entry.ScoreAfter = 81+i;
                    entry.ValueBefore = (i-1)*75000; entry.ValueDelta = 75000; entry.ValueAfter = i*75000;
                    entry.Reason = "Completed an order and collected it on time."; entry.CreatedAt = now.AddDays(-13+i); db.Add(entry);
                }
        await db.SaveChangesAsync();
        Console.WriteLine($"Customer use cases ready: {await db.Orders.CountAsync(x => x.CustomerId == customer.Id)} orders, {await db.Set<Refund>().CountAsync(x => x.Order.CustomerId == customer.Id)} refunds, {await db.Set<Notification>().CountAsync(x => x.UserId == customer.Id)} notifications.");
    }
}
