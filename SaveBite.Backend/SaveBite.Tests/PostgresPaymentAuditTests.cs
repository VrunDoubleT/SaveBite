using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.DTOs;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Models.Responses;
using SaveBite.Backend.Repositories.Implementations;
using SaveBite.Backend.Repositories.Interfaces;
using SaveBite.Backend.Services.Implementations;
using SaveBite.Backend.Services.Interfaces;
using Xunit;

namespace SaveBite.Tests;

// Explicit opt-in: each case uses its own disposable database on a local test server.
public sealed class PostgresAuditFactAttribute : FactAttribute
{
    public PostgresAuditFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SAVEBITE_AUDIT_TEST_DB")))
            Skip = "Set SAVEBITE_AUDIT_TEST_DB to a dedicated local PostgreSQL test server.";
    }
}

public class PostgresPaymentAuditTests
{
    [PostgresAuditFact]
    public async Task MissingCollectedAmountCannotSettleFee()
    {
        await using var fixture = await Fixture.Create();
        fixture.Api.Response = JsonSerializer.SerializeToElement(new { status = "PAID", orderCode = 123456, amount = 50000 });
        await fixture.Service.ReconcilePendingPaymentAsync(fixture.Payment.Id);
        await fixture.Db.Entry(fixture.Payment).ReloadAsync();
        await fixture.Db.Entry(fixture.Statement).ReloadAsync();
        Assert.Equal(PaymentStatus.Pending, fixture.Payment.Status);
        Assert.Equal(PlatformFeeStatementStatus.Issued, fixture.Statement.Status);
    }

    [PostgresAuditFact]
    public async Task VerifiedFeeSettlementIsIdempotentWithActualTransactions()
    {
        await using var fixture = await Fixture.Create();
        fixture.Api.Response = JsonSerializer.SerializeToElement(new { status = "PAID", orderCode = 123456, amount = 50000, amountPaid = 50000 });
        await fixture.Service.ReconcilePendingPaymentAsync(fixture.Payment.Id);
        await fixture.Service.ReconcilePendingPaymentAsync(fixture.Payment.Id);
        await fixture.Db.Entry(fixture.Payment).ReloadAsync();
        await fixture.Db.Entry(fixture.Statement).ReloadAsync();
        Assert.Equal(PaymentStatus.Succeeded, fixture.Payment.Status);
        Assert.Equal(PlatformFeeStatementStatus.Paid, fixture.Statement.Status);
        Assert.NotNull(fixture.Statement.PaidAt);
    }

    [PostgresAuditFact]
    public async Task StaleMissingPaymentLookupCannotOverwriteConcurrentWebhook()
    {
        await using var fixture = await Fixture.Create();
        // Simulate the webhook committing while the provider lookup is in flight.
        fixture.Api.BeforeResponse = async () =>
        {
            await using var other = new AppDbContext(fixture.Db.Options());
            await other.PlatformFeePayments.Where(p => p.Id == fixture.Payment.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, PaymentStatus.Succeeded));
        };
        fixture.Payment.CreatedAt = DateTime.UtcNow.AddHours(-1);
        await fixture.Db.SaveChangesAsync();
        fixture.Api.Response = JsonSerializer.SerializeToElement(new { status = "NOT_FOUND" });
        await fixture.Service.ReconcilePendingPaymentAsync(fixture.Payment.Id);
        await fixture.Db.Entry(fixture.Payment).ReloadAsync();
        Assert.Equal(PaymentStatus.Succeeded, fixture.Payment.Status);
    }

    [PostgresAuditFact]
    public async Task FractionalBillCreatesMatchingWholeVndPaymentAndAuditHistory()
    {
        await using var fixture = await Fixture.Create();
        fixture.Db.Remove(fixture.Payment);
        fixture.Statement.TotalFeeAmount = 2500.50m;
        await fixture.Db.SaveChangesAsync();
        fixture.Api.Create = true;
        var response = await fixture.Service.CreatePaymentLinkAsync(fixture.Statement.Id, fixture.Statement.ShopId,
            fixture.Statement.Shop.OwnerUserId);
        Assert.Equal(2501, response.Amount);
        Assert.Equal(2501, fixture.Api.RequestedAmount);
        var payment = await fixture.Db.PlatformFeePayments.SingleAsync();
        Assert.Equal(2501, payment.Amount);
        var history = Assert.Single(await fixture.Db.PlatformFeeStatementHistories.ToListAsync());
        Assert.Equal(StatementChangeType.VndRounding, history.ChangeType);
        Assert.Equal(0.50m, history.ChangeAmount);
    }

    [PostgresAuditFact]
    public async Task ChangedFeeClosesOldQrBeforeCreatingReplacement()
    {
        await using var fixture = await Fixture.Create();
        fixture.Statement.TotalFeeAmount = 40000;
        await fixture.Db.SaveChangesAsync();
        fixture.Api.Create = true;
        fixture.Api.Response = JsonSerializer.SerializeToElement(new { status = "PENDING", orderCode = 123456, amount = 50000, amountPaid = 0, id = "old" });
        var response = await fixture.Service.CreatePaymentLinkAsync(fixture.Statement.Id, fixture.Statement.ShopId,
            fixture.Statement.Shop.OwnerUserId);
        Assert.Equal(40000, response.Amount);
        Assert.Equal(1, fixture.Api.Cancellations);
        await fixture.Db.Entry(fixture.Payment).ReloadAsync();
        Assert.Equal(PaymentStatus.Cancelled, fixture.Payment.Status);
        Assert.Equal(2, await fixture.Db.PlatformFeePayments.CountAsync());
    }

    [PostgresAuditFact]
    public async Task ClosedQrWithPartialFundsCannotBeReplacedAndChargedAgain()
    {
        await using var fixture = await Fixture.Create();
        fixture.Statement.TotalFeeAmount = 40000;
        await fixture.Db.SaveChangesAsync();
        fixture.Api.Create = true;
        fixture.Api.Response = JsonSerializer.SerializeToElement(new { status = "CANCELLED", orderCode = 123456,
            amount = 50000, amountPaid = 1000 });
        await Assert.ThrowsAsync<AppException>(() => fixture.Service.CreatePaymentLinkAsync(fixture.Statement.Id,
            fixture.Statement.ShopId, fixture.Statement.Shop.OwnerUserId));
        Assert.Equal(1, await fixture.Db.PlatformFeePayments.CountAsync());
        Assert.Equal(0, fixture.Api.RequestedAmount);
    }

    [PostgresAuditFact]
    public async Task IncreasedBillRetainsOldReceiptAndChargesOnlyTheDifference()
    {
        await using var fixture = await Fixture.Create();
        fixture.Statement.TotalFeeAmount = 60000;
        await fixture.Db.SaveChangesAsync();
        fixture.Api.Response = JsonSerializer.SerializeToElement(new { status = "PAID", orderCode = 123456, amount = 50000, amountPaid = 50000 });
        await fixture.Service.ReconcilePendingPaymentAsync(fixture.Payment.Id);
        Assert.Equal(PaymentStatus.Succeeded, fixture.Payment.Status);
        Assert.Equal(PlatformFeeStatementStatus.Issued, fixture.Statement.Status);
        fixture.Api.Create = true;
        var response = await fixture.Service.CreatePaymentLinkAsync(fixture.Statement.Id, fixture.Statement.ShopId,
            fixture.Statement.Shop.OwnerUserId);
        Assert.Equal(10000, response.Amount);
        fixture.Api.Response = JsonSerializer.SerializeToElement(new { status = "PAID", orderCode = response.OrderCode, amount = 10000, amountPaid = 10000 });
        fixture.Api.Create = false;
        await fixture.Service.ReconcilePendingPaymentAsync(response.PaymentId);
        await fixture.Db.Entry(fixture.Statement).ReloadAsync();
        Assert.Equal(PlatformFeeStatementStatus.Paid, fixture.Statement.Status);
        Assert.Equal(60000, await fixture.Db.PlatformFeePayments.Where(p => p.Status == PaymentStatus.Succeeded).SumAsync(p => p.Amount));
    }

    [PostgresAuditFact]
    public async Task RoundingBillToZeroReleasesFeeOnlySuspension()
    {
        await using var fixture = await Fixture.Create();
        fixture.Db.Remove(fixture.Payment);
        fixture.Statement.TotalFeeAmount = 0.49m;
        fixture.Statement.Shop.Status = ShopStatus.Suspended;
        fixture.Statement.Shop.SuspendedForPlatformFees = true;
        await fixture.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<AppException>(() => fixture.Service.CreatePaymentLinkAsync(fixture.Statement.Id,
            fixture.Statement.ShopId, fixture.Statement.Shop.OwnerUserId));
        await fixture.Db.Entry(fixture.Statement).ReloadAsync();
        await fixture.Db.Entry(fixture.Statement.Shop).ReloadAsync();
        Assert.Equal(PlatformFeeStatementStatus.Paid, fixture.Statement.Status);
        Assert.False(fixture.Statement.Shop.SuspendedForPlatformFees);
        Assert.Equal(ShopStatus.Active, fixture.Statement.Shop.Status);
        Assert.Empty(await fixture.Db.PlatformFeePayments.ToListAsync());
    }

    [PostgresAuditFact]
    public async Task RefundReducedBillCoveredByOldReceiptSettlesWithoutChargingAgain()
    {
        await using var fixture = await Fixture.Create();
        fixture.Payment.Status = PaymentStatus.Succeeded;
        fixture.Statement.TotalFeeAmount = 40000;
        await fixture.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<AppException>(() => fixture.Service.CreatePaymentLinkAsync(fixture.Statement.Id,
            fixture.Statement.ShopId, fixture.Statement.Shop.OwnerUserId));
        await fixture.Db.Entry(fixture.Statement).ReloadAsync();
        Assert.Equal(PlatformFeeStatementStatus.Paid, fixture.Statement.Status);
        Assert.Equal(1, await fixture.Db.PlatformFeePayments.CountAsync());
        Assert.Equal(0, fixture.Api.RequestedAmount);
    }

    private static PlatformFeeStatementService ServiceFor(AppDbContext db, Api api)
    {
        var config = new ConfigurationBuilder().Build();
        var notifications = new Notifications();
        var availability = new PlatformFeePaymentAvailability(db, config, notifications, TimeProvider.System, null!);
        var credentials = DispatchProxy.Create<IPlatformPayOsConfigService, Credentials>();
        return new PlatformFeeStatementService(new PlatformFeeStatementRepository(db), TimeProvider.System,
            NullLogger<PlatformFeeStatementService>.Instance, config, null!, notifications, db, null!, credentials,
            null!, null!, availability, api, new Protector());
    }

    [PostgresAuditFact]
    public async Task UnchangedAmountLeavesExistingQrUntouched()
    {
        await using var f = await Fixture.Create();
        f.Payment.GatewayCheckoutUrl = "https://pay.payos.vn/web/old";
        f.Payment.GatewayQrCode = "original-qr";
        f.Statement.UpdatedAt = DateTime.UtcNow;
        await f.Db.SaveChangesAsync();
        await f.Service.InvalidateChangedPaymentLinksAsync();
        Assert.Null(f.Payment.GatewayInvalidatedAt);
        Assert.Equal("original-qr", f.Payment.GatewayQrCode);
        Assert.Equal(0, f.Api.Reads);
        Assert.Equal(0, f.Api.Cancellations);
    }

    [PostgresAuditFact]
    public async Task AmountChangePersistsInvalidationAndAutomaticallyClosesOldQr()
    {
        await using var f = await Fixture.Create();
        f.Payment.GatewayQrCode = "original-qr";
        f.Statement.TotalFeeAmount = 40000;
        await f.Db.SaveChangesAsync();
        Assert.NotNull(f.Payment.GatewayInvalidatedAt);
        Assert.Null(f.Payment.GatewayQrCode);
        Assert.Null(f.Payment.GatewayClosedAt); // Database invalidation is not provider closure.
        f.Api.Response = JsonSerializer.SerializeToElement(new { status = "PENDING", orderCode = 123456, amount = 50000, amountPaid = 0 });
        await f.Service.InvalidateChangedPaymentLinksAsync();
        Assert.Equal(1, f.Api.Cancellations);
        Assert.Equal(PaymentStatus.Cancelled, f.Payment.Status);
        Assert.NotNull(f.Payment.GatewayClosedAt);
        await f.Service.InvalidateChangedPaymentLinksAsync();
        Assert.Equal(1, f.Api.Cancellations);
        Assert.Single(await f.Db.PlatformFeePayments.ToListAsync());
    }

    [PostgresAuditFact]
    public async Task RollbackOfAmountChangeAlsoRollsBackQrInvalidation()
    {
        await using var f = await Fixture.Create();
        f.Payment.GatewayQrCode = "original-qr";
        await f.Db.SaveChangesAsync();
        await using (var tx = await f.Db.Database.BeginTransactionAsync())
        {
            f.Statement.TotalFeeAmount = 40000;
            await f.Db.SaveChangesAsync();
            Assert.NotNull(f.Payment.GatewayInvalidatedAt);
            await tx.RollbackAsync();
        }
        await using var check = new AppDbContext(f.Db.Options());
        var payment = await check.PlatformFeePayments.SingleAsync();
        Assert.Null(payment.GatewayInvalidatedAt);
        Assert.Equal("original-qr", payment.GatewayQrCode);
        Assert.Equal(50000, await check.PlatformFeeStatements.Select(s => s.TotalFeeAmount).SingleAsync());
        Assert.Equal(0, f.Api.Cancellations);
    }

    [PostgresAuditFact]
    public async Task ProviderOutageKeepsDurableInvalidationAndBlocksReplacementUntilRetry()
    {
        await using var f = await Fixture.Create();
        f.Statement.TotalFeeAmount = 40000;
        await f.Db.SaveChangesAsync();
        f.Api.Response = JsonSerializer.SerializeToElement(new { status = "PENDING", orderCode = 123456, amount = 50000, amountPaid = 0 });
        f.Api.FailCancellation = true;
        await f.Service.InvalidateChangedPaymentLinksAsync();
        Assert.Null(f.Payment.GatewayClosedAt);
        Assert.Equal(PaymentStatus.Pending, f.Payment.Status);
        await Assert.ThrowsAsync<AppException>(() => f.Service.CreatePaymentLinkAsync(f.Statement.Id, f.Statement.ShopId, f.Statement.Shop.OwnerUserId));
        Assert.Equal(1, await f.Db.PlatformFeePayments.CountAsync());
        f.Api.FailCancellation = false;
        await f.Service.InvalidateChangedPaymentLinksAsync();
        Assert.NotNull(f.Payment.GatewayClosedAt);
        f.Api.Create = true;
        var replacement = await f.Service.CreatePaymentLinkAsync(f.Statement.Id, f.Statement.ShopId, f.Statement.Shop.OwnerUserId);
        Assert.Equal(40000, replacement.Amount);
    }

    [PostgresAuditFact]
    public async Task TransferWinsCancellationAndOnlyRemainingDebtCanBeCharged()
    {
        await using var f = await Fixture.Create();
        f.Statement.TotalFeeAmount = 60000;
        await f.Db.SaveChangesAsync();
        f.Api.Response = JsonSerializer.SerializeToElement(new { status = "PENDING", orderCode = 123456, amount = 50000, amountPaid = 0 });
        f.Api.CancellationResponse = JsonSerializer.SerializeToElement(new { status = "PAID", orderCode = 123456, amount = 50000, amountPaid = 50000 });
        await f.Service.InvalidateChangedPaymentLinksAsync();
        Assert.Equal(PaymentStatus.Succeeded, f.Payment.Status);
        Assert.Equal(50000, f.Payment.CollectedAmount);
        Assert.Equal(PlatformFeeStatementStatus.Issued, f.Statement.Status);
        f.Api.Create = true;
        var replacement = await f.Service.CreatePaymentLinkAsync(f.Statement.Id, f.Statement.ShopId, f.Statement.Shop.OwnerUserId);
        Assert.Equal(10000, replacement.Amount);
    }

    [PostgresAuditFact]
    public async Task ActualFundsCoveringUpdatedBillCannotBeChargedAgainUsingOldQrAmount()
    {
        await using var f = await Fixture.Create();
        f.Statement.TotalFeeAmount = 60000;
        await f.Db.SaveChangesAsync();
        f.Api.Response = JsonSerializer.SerializeToElement(new { status = "PENDING", orderCode = 123456, amount = 50000, amountPaid = 0 });
        f.Api.CancellationResponse = JsonSerializer.SerializeToElement(new { status = "PAID", orderCode = 123456, amount = 50000, amountPaid = 60000 });
        await f.Service.InvalidateChangedPaymentLinksAsync();
        Assert.Equal(60000, f.Payment.CollectedAmount);
        Assert.Equal(PlatformFeeStatementStatus.Paid, f.Statement.Status);
        await Assert.ThrowsAsync<AppException>(() => f.Service.CreatePaymentLinkAsync(f.Statement.Id, f.Statement.ShopId, f.Statement.Shop.OwnerUserId));
        Assert.Single(await f.Db.PlatformFeePayments.ToListAsync());
    }

    [PostgresAuditFact]
    public async Task PartialFundsReceivedDuringCancellationArePreservedAndBlockDoubleCollection()
    {
        await using var f = await Fixture.Create();
        f.Statement.TotalFeeAmount = 40000;
        await f.Db.SaveChangesAsync();
        f.Api.Response = JsonSerializer.SerializeToElement(new { status = "PENDING", orderCode = 123456, amount = 50000, amountPaid = 0 });
        f.Api.CancellationResponse = JsonSerializer.SerializeToElement(new { status = "CANCELLED", orderCode = 123456, amount = 50000, amountPaid = 1000 });
        await f.Service.InvalidateChangedPaymentLinksAsync();
        Assert.Equal(PaymentStatus.Cancelled, f.Payment.Status);
        Assert.Equal(1000, f.Payment.CollectedAmount);
        Assert.NotNull(f.Payment.GatewayClosedAt);
        await Assert.ThrowsAsync<AppException>(() => f.Service.CreatePaymentLinkAsync(f.Statement.Id, f.Statement.ShopId, f.Statement.Shop.OwnerUserId));
        Assert.Single(await f.Db.PlatformFeePayments.ToListAsync());
    }

    [PostgresAuditFact]
    public async Task RegeneratingChangedBillClosesQrAfterCommitWithoutOwnerOpeningPayment()
    {
        await using var f = await Fixture.Create();
        f.Api.Response = JsonSerializer.SerializeToElement(new { status = "PENDING", orderCode = 123456, amount = 50000, amountPaid = 0 });
        // This fixture has no order-fee ledger rows, so regeneration correctly reduces its bill to zero.
        await f.Service.GenerateMonthlyStatementsAsync(f.Statement.BillingYear, f.Statement.BillingMonth);
        await f.Db.Entry(f.Payment).ReloadAsync();
        Assert.Equal(0, f.Statement.TotalFeeAmount);
        Assert.Equal(PlatformFeeStatementStatus.Paid, f.Statement.Status);
        Assert.NotNull(f.Payment.GatewayInvalidatedAt);
        Assert.NotNull(f.Payment.GatewayClosedAt);
        Assert.Equal(PaymentStatus.Cancelled, f.Payment.Status);
        Assert.Equal(1, f.Api.Cancellations);
        Assert.Single(await f.Db.PlatformFeePayments.ToListAsync());
    }

    [PostgresAuditFact]
    public async Task ClosedResponseWithoutFinalCollectedTotalCannotPermitReplacement()
    {
        await using var f = await Fixture.Create();
        f.Statement.TotalFeeAmount = 40000;
        await f.Db.SaveChangesAsync();
        f.Api.Response = JsonSerializer.SerializeToElement(new { status = "PENDING", orderCode = 123456, amount = 50000, amountPaid = 0 });
        f.Api.CancellationResponse = JsonSerializer.SerializeToElement(new { status = "CANCELLED", orderCode = 123456, amount = 50000 });
        await f.Service.InvalidateChangedPaymentLinksAsync();
        Assert.Null(f.Payment.GatewayClosedAt);
        await Assert.ThrowsAsync<AppException>(() => f.Service.CreatePaymentLinkAsync(f.Statement.Id, f.Statement.ShopId, f.Statement.Shop.OwnerUserId));
        Assert.Single(await f.Db.PlatformFeePayments.ToListAsync());
    }

    [PostgresAuditFact]
    public async Task MissingRecentLinkRemainsBlockedInCaseCreationIsStillInFlight()
    {
        await using var f = await Fixture.Create();
        f.Statement.TotalFeeAmount = 40000;
        await f.Db.SaveChangesAsync();
        f.Api.Response = JsonSerializer.SerializeToElement(new { status = "NOT_FOUND" });
        await f.Service.InvalidateChangedPaymentLinksAsync();
        Assert.Null(f.Payment.GatewayClosedAt);
        await Assert.ThrowsAsync<AppException>(() => f.Service.CreatePaymentLinkAsync(f.Statement.Id, f.Statement.ShopId, f.Statement.Shop.OwnerUserId));
        Assert.Single(await f.Db.PlatformFeePayments.ToListAsync());
    }

    [PostgresAuditFact]
    public async Task SignedWebhookArrivingWhileCancellationIsRunningStillRecordsMoneyOnce()
    {
        await using var f = await Fixture.Create();
        f.Statement.TotalFeeAmount = 40000;
        await f.Db.SaveChangesAsync();
        f.Api.Response = JsonSerializer.SerializeToElement(new { status = "PENDING", orderCode = 123456, amount = 50000, amountPaid = 0 });
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        f.Api.BeforeCancellation = async () => { entered.SetResult(); await release.Task.WaitAsync(TimeSpan.FromSeconds(10)); };
        var closing = f.Service.InvalidateChangedPaymentLinksAsync();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await using var webhookDb = new AppDbContext(f.Db.Options());
        var webhookService = ServiceFor(webhookDb, new Api());
        var data = JsonSerializer.SerializeToElement(new { amount = 50000, code = "00", orderCode = 123456 });
        var body = JsonSerializer.SerializeToElement(new { code = "00", data,
            signature = PayOsCheckoutGateway.Sign("amount=50000&code=00&orderCode=123456", "checksum") });
        var webhook = webhookService.ProcessFeeWebhookAsync(body);
        release.SetResult();
        await Task.WhenAll(closing, webhook).WaitAsync(TimeSpan.FromSeconds(20));
        await webhookService.ProcessFeeWebhookAsync(body);
        await f.Db.Entry(f.Payment).ReloadAsync();
        await f.Db.Entry(f.Statement).ReloadAsync();
        Assert.Equal(PaymentStatus.Succeeded, f.Payment.Status);
        Assert.Equal(50000, f.Payment.CollectedAmount);
        Assert.Equal(PlatformFeeStatementStatus.Paid, f.Statement.Status);
        Assert.Single(await f.Db.PlatformFeePayments.ToListAsync());
    }

    [PostgresAuditFact]
    public async Task FeedbackConcurrentEditsRespectThreeEditLimitAndPublicReadHidesPurchaseIds()
    {
        await using var f = await Fixture.Create();
        var now = DateTime.UtcNow;
        var category = new Category { Id = Guid.NewGuid(), Name = "Audit food", IsActive = true, CreatedAt = now, UpdatedAt = now };
        var product = new Product { Id = Guid.NewGuid(), Shop = f.Statement.Shop, Category = category, Name = "Audit meal", CreatedAt = now, UpdatedAt = now };
        var variant = new ProductVariant { Id = Guid.NewGuid(), Product = product, OriginalPrice = 20000, IsActive = true, CreatedAt = now };
        var deal = new FlashDeal { Id = Guid.NewGuid(), Product = product, Shop = f.Statement.Shop,
            Status = FlashDealStatus.Closed, SaleStartTime = now.AddHours(-2), OrderEndTime = now.AddHours(-1),
            ShopClosingTime = now, FeedPublishedAt = now.AddHours(-2), PublicationId = Guid.NewGuid(), CreatedAt = now, UpdatedAt = now };
        var dealVariant = new FlashDealVariant { Id = Guid.NewGuid(), FlashDeal = deal, Variant = variant,
            OriginalPrice = 20000, DealPrice = 10000, TotalQuantity = 10, SoldQuantity = 1, CreatedAt = now, UpdatedAt = now };
        var order = new Order { Id = Guid.NewGuid(), Customer = f.Statement.Shop.Owner, Shop = f.Statement.Shop,
            OrderCode = "AUDIT01", Status = OrderStatus.Completed, TotalAmount = 10000, CreatedAt = now, UpdatedAt = now };
        var item = new OrderItem { Id = Guid.NewGuid(), Order = order, FlashDealVariant = dealVariant,
            ProductNameSnapshot = "Audit meal", Quantity = 1, UnitPrice = 10000 };
        var feedback = new ProductFeedback { Id = Guid.NewGuid(), OrderItem = item, Product = product,
            User = f.Statement.Shop.Owner, Rating = 4, EditCount = 2, CreatedAt = now, UpdatedAt = now };
        f.Db.Add(feedback);
        await f.Db.SaveChangesAsync();
        async Task<bool> Edit()
        {
            await using var db = new AppDbContext(f.Db.Options());
            var service = new FeedbackService(new FeedbackRepository(db), new Notifications(), db, null!);
            try { await service.EditFeedbackAsync(feedback.UserId, feedback.Id, new() { Rating = 5, Comment = "Updated" }); return true; }
            catch (AppException) { return false; }
        }
        var results = await Task.WhenAll(Edit(), Edit());
        Assert.Single(results.Where(x => x));
        await f.Db.Entry(feedback).ReloadAsync();
        Assert.Equal(3, feedback.EditCount);
        var reader = new FeedbackService(new FeedbackRepository(f.Db), new Notifications(), f.Db, null!);
        var publicView = await reader.GetFeedbackByIdAsync(feedback.Id, viewerUserId: Guid.NewGuid());
        Assert.Equal(Guid.Empty, publicView.OrderItemId);
        Assert.Equal(Guid.Empty, publicView.OrderId);
        Assert.Equal(string.Empty, publicView.OrderCode);
        Assert.False(publicView.CanEdit);
        Assert.Equal(3, publicView.EditCount);
        var ownView = await reader.GetFeedbackByIdAsync(feedback.Id, viewerUserId: feedback.UserId);
        Assert.Equal(item.Id, ownView.OrderItemId);
        Assert.Equal(order.Id, ownView.OrderId);
        Assert.False(ownView.CanEdit);
        Assert.Equal(3, ownView.EditCount);

        var shops = new ShopRepository(f.Db);
        async Task AssertStars(int count, double average)
        {
            var stats = await shops.GetShopReviewStatsAsync(order.ShopId);
            Assert.Equal(count, stats.TotalReviews);
            Assert.Equal(average, stats.AvgRating);
            Assert.Equal(count, stats.RatingBreakdown.Values.Sum());
            var batch = await shops.GetReviewsStatsForShopsAsync([order.ShopId]);
            if (count == 0) Assert.False(batch.ContainsKey(order.ShopId));
            else { Assert.Equal(count, batch[order.ShopId].TotalCount); Assert.Equal(average, batch[order.ShopId].AvgRating); }
        }
        await AssertStars(1, 5);
        feedback.Status = FeedbackStatus.HiddenByAdmin;
        feedback.HiddenAt = DateTime.UtcNow;
        await f.Db.SaveChangesAsync();
        await AssertStars(0, 0);
        feedback.Status = FeedbackStatus.Visible;
        feedback.HiddenAt = null;
        await f.Db.SaveChangesAsync();
        await AssertStars(1, 5);
        await reader.DeleteFeedbackAsync(feedback.UserId, feedback.Id);
        await AssertStars(0, 0);
        var refreshEvents = await f.Db.OrderEventOutboxes.Where(e => e.RoutingKey == FeedbackRealtimeOutbox.Route).ToListAsync();
        Assert.Contains(refreshEvents, e => e.PayloadJson.Contains("hide"));
        Assert.Contains(refreshEvents, e => e.PayloadJson.Contains("refresh"));
    }

    [PostgresAuditFact]
    public async Task SoldOutDealStillBlocksOverlappingSchedule()
    {
        await using var f = await Fixture.Create();
        var now = DateTime.UtcNow;
        var product = new Product { Id = Guid.NewGuid(), Shop = f.Statement.Shop, Name = "Audit meal",
            Category = new Category { Id = Guid.NewGuid(), Name = "Food", IsActive = true, CreatedAt = now, UpdatedAt = now }, CreatedAt = now, UpdatedAt = now };
        var deal = new FlashDeal { Id = Guid.NewGuid(), Product = product, Shop = f.Statement.Shop,
            Status = FlashDealStatus.SoldOut, SaleStartTime = now, OrderEndTime = now.AddHours(1),
            ShopClosingTime = now.AddHours(1), FeedPublishedAt = now, PublicationId = Guid.NewGuid(), CreatedAt = now, UpdatedAt = now };
        f.Db.Add(deal);
        await f.Db.SaveChangesAsync();
        var repository = new FlashDealRepository(f.Db, null!);
        Assert.True(await repository.HasOverlappingDealAsync(deal.ShopId, product.Id, now.AddMinutes(5), now.AddMinutes(35)));
        Assert.False(await repository.HasOverlappingDealAsync(deal.ShopId, product.Id, now.AddHours(1), now.AddHours(2)));
        deal.Status = FlashDealStatus.Cancelled;
        await f.Db.SaveChangesAsync();
        Assert.False(await repository.HasOverlappingDealAsync(deal.ShopId, product.Id, now.AddMinutes(5), now.AddMinutes(35)));
    }

    [PostgresAuditFact]
    public async Task UpdatingExistingDealVariantCannotRaisePriceToOriginalAndCancelledDealCannotReopen()
    {
        await using var f = await Fixture.Create();
        var now = DateTime.UtcNow;
        var product = new Product { Id = Guid.NewGuid(), Shop = f.Statement.Shop, Name = "Meal",
            Category = new Category { Id = Guid.NewGuid(), Name = "Food", IsActive = true, CreatedAt = now, UpdatedAt = now }, CreatedAt = now, UpdatedAt = now };
        var variant = new ProductVariant { Id = Guid.NewGuid(), Product = product, OriginalPrice = 20000, IsActive = true, CreatedAt = now };
        var deal = new FlashDeal { Id = Guid.NewGuid(), Product = product, Shop = f.Statement.Shop,
            Status = FlashDealStatus.OnSale, SaleStartTime = now.AddMinutes(-5), OrderEndTime = now.AddMinutes(40),
            ShopClosingTime = now.AddMinutes(50), FeedPublishedAt = now, PublicationId = Guid.NewGuid(), CreatedAt = now, UpdatedAt = now };
        var dealVariant = new FlashDealVariant { Id = Guid.NewGuid(), FlashDeal = deal, Variant = variant,
            OriginalPrice = 20000, DealPrice = 10000, TotalQuantity = 10, CreatedAt = now, UpdatedAt = now };
        f.Db.Add(dealVariant);
        await f.Db.SaveChangesAsync();
        var shops = DispatchProxy.Create<IShopRepository, AuditShopRepository>();
        ((AuditShopRepository)(object)shops).Shop = f.Statement.Shop;
        var service = new FlashDealService(new FlashDealRepository(f.Db, null!), null!, shops, null!, null!, null!,
            NullLogger<FlashDealService>.Instance, f.Db);
        await Assert.ThrowsAsync<AppException>(() => service.UpdateDealAsync(f.Statement.Shop.OwnerUserId, deal.Id,
            new SaveBite.Backend.Models.Requests.UpdateFlashDealRequest {
                SaleStartTime = deal.SaleStartTime, OrderEndTime = deal.OrderEndTime, ShopClosingTime = deal.ShopClosingTime,
                Variants = [new() { VariantId = variant.Id, DealPrice = 20000, TotalQuantity = 10 }] }));
        await f.Db.Entry(dealVariant).ReloadAsync();
        Assert.Equal(10000, dealVariant.DealPrice);
        deal.Status = FlashDealStatus.Cancelled;
        await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<AppException>(() => service.UnhideDealAsync(f.Statement.Shop.OwnerUserId, deal.Id));
        await f.Db.Entry(deal).ReloadAsync();
        Assert.Equal(FlashDealStatus.Cancelled, deal.Status);
    }

    public class AuditShopRepository : DispatchProxy
    {
        public Shop Shop = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name == "GetShopByOwnerUserIdAsync"
            ? Task.FromResult<Shop?>(Shop) : throw new NotSupportedException(method.Name);
    }

    [PostgresAuditFact]
    public async Task OutboxFailureRemainsPendingAndRetryPublishesOnceEvenWithStaleTracking()
    {
        await using var f = await Fixture.Create();
        var message = new OrderEventOutbox { Id = Guid.NewGuid(), EventKey = "audit-outbox", RoutingKey = "audit", PayloadJson = "{}", CreatedAt = DateTime.UtcNow };
        f.Db.Add(message);
        await f.Db.SaveChangesAsync();
        var repository = new OrderEventRepository(f.Db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.DispatchAsync(message.Id,
            _ => throw new InvalidOperationException("broker unavailable"), CancellationToken.None));
        await using var other = new AppDbContext(f.Db.Options());
        Assert.Null((await other.OrderEventOutboxes.SingleAsync()).PublishedAt);
        var deliveries = 0;
        await new OrderEventRepository(other).DispatchAsync(message.Id, row =>
        {
            deliveries++;
            row.PublishedAt = DateTime.UtcNow;
            return Task.CompletedTask;
        }, CancellationToken.None);
        // f.Db still tracked the Pending row, but must reread the committed receipt under lock.
        await repository.DispatchAsync(message.Id, _ => { deliveries++; return Task.CompletedTask; }, CancellationToken.None);
        Assert.Equal(1, deliveries);
    }

    [PostgresAuditFact]
    public async Task OutboxAndBusinessWriteRollBackTogether()
    {
        await using var f = await Fixture.Create();
        await using (var tx = await f.Db.Database.BeginTransactionAsync())
        {
            f.Statement.TotalFeeAmount = 42000;
            f.Db.Add(new OrderEventOutbox { Id = Guid.NewGuid(), EventKey = "rolled-back-event", RoutingKey = "audit", PayloadJson = "{}", CreatedAt = DateTime.UtcNow });
            await f.Db.SaveChangesAsync();
            await tx.RollbackAsync();
        }
        await using var verify = new AppDbContext(f.Db.Options());
        Assert.False(await verify.OrderEventOutboxes.AnyAsync(e => e.EventKey == "rolled-back-event"));
        Assert.Equal(50000, await verify.PlatformFeeStatements.Select(s => s.TotalFeeAmount).SingleAsync());
    }

    [PostgresAuditFact]
    public async Task StaleAddressEventProjectsLatestDefaultAndDeduplicatesDelivery()
    {
        await using var f = await Fixture.Create();
        var current = new UserAddress { Id = Guid.NewGuid(), UserId = f.Statement.Shop.OwnerUserId,
            AddressLine = "Latest", Latitude = 10, Longitude = 106, IsDefault = true, AddressVersion = 2,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        f.Db.Add(current);
        UserDefaultAddressChangedOutbox.Stage(f.Db, current.UserId, Guid.NewGuid(), 0, 0, 99, false, true);
        await f.Db.SaveChangesAsync();
        var message = await f.Db.OrderEventOutboxes.SingleAsync();
        var location = DispatchProxy.Create<ILocationService, AuditLocation>();
        var recorded = (AuditLocation)(object)location;
        var dispatcher = new SaveBite.Backend.BackgroundJobs.Jobs.OrderEventDispatchJob(new OrderEventRepository(f.Db), null!,
            TimeProvider.System, new SaveBite.Backend.Messaging.Configuration.RabbitMqTopologyState(), locationService: location, db: f.Db);
        await dispatcher.ExecuteAsync(message.Id, CancellationToken.None);
        await dispatcher.ExecuteAsync(message.Id, CancellationToken.None);
        Assert.Equal(1, recorded.Synced);
        Assert.Equal(0, recorded.Removed);
        Assert.Equal(10, recorded.Latitude);
        Assert.Equal(106, recorded.Longitude);
    }

    [PostgresAuditFact]
    public async Task PenaltySchedulingFailureDoesNotRollBackRuleOrBlockFutureRecovery()
    {
        await using var f = await Fixture.Create();
        var rule = new RecoveryRequirementRule { Id = Guid.NewGuid(), MinimumOccurrence = 1, ScoreDeduction = 1,
            EffectiveFrom = DateTime.UtcNow.AddHours(1), EffectiveTo = DateTime.UtcNow.AddHours(2), CreatedAt = DateTime.UtcNow,
            StartJobId = "obsolete-start", EndJobId = "obsolete-end" };
        f.Db.Add(rule);
        await f.Db.SaveChangesAsync();
        var jobs = DispatchProxy.Create<Hangfire.IBackgroundJobClient, FailingHangfire>();
        var scheduler = new SaveBite.Backend.BackgroundJobs.Scheduling.PenaltyRuleScheduler(f.Db, jobs, TimeProvider.System);
        await scheduler.SyncAsync(rule.Id, CancellationToken.None);
        await f.Db.Entry(rule).ReloadAsync();
        Assert.Null(rule.StartJobId);
        Assert.Null(rule.EndJobId);
        Assert.True(rule.IsActive);
        Assert.Null(rule.StartProcessedAt);
    }

    public class FailingHangfire : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => throw new InvalidOperationException("Hangfire unavailable");
    }
    public class AuditLocation : DispatchProxy
    {
        public int Synced, Removed;
        public double Latitude, Longitude;
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == "SyncDefaultAddressAsync") { Synced++; Latitude = (double)args![1]!; Longitude = (double)args[2]!; return Task.CompletedTask; }
            if (method.Name == "RemoveDefaultAddressAsync") { Removed++; return Task.CompletedTask; }
            throw new NotSupportedException(method.Name);
        }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        public AppDbContext Db = null!;
        public PlatformFeePayment Payment = null!;
        public PlatformFeeStatement Statement = null!;
        public PlatformFeeStatementService Service = null!;
        public Api Api = new();

        public static async Task<Fixture> Create()
        {
            var connection = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("SAVEBITE_AUDIT_TEST_DB"));
            if (connection.Host is not ("localhost" or "127.0.0.1") || !connection.Database!.StartsWith("savebite_audit", StringComparison.Ordinal))
                throw new InvalidOperationException("Audit tests require a dedicated local savebite_audit database.");
            connection.Database = "savebite_audit_" + Guid.NewGuid().ToString("N");
            var f = new Fixture { Db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection.ConnectionString).Options) };
            try
            {
                await f.Db.Database.MigrateAsync();
                var now = DateTime.UtcNow;
                var owner = new User { Id = Guid.NewGuid(), Email = "audit@example.test", FullName = "Audit owner", Role = UserRole.StoreOwner, CreatedAt = now, UpdatedAt = now };
                var application = new ShopApplication { Id = Guid.NewGuid(), ApplicantUser = owner, Status = ShopApplicationStatus.Approved, CreatedAt = now, UpdatedAt = now };
                var shop = new Shop { Id = Guid.NewGuid(), Owner = owner, Application = application, Name = "Audit shop", CreatedAt = now, UpdatedAt = now };
                f.Statement = new PlatformFeeStatement { Id = Guid.NewGuid(), Shop = shop, BillingYear = 2026, BillingMonth = 10,
                    Status = PlatformFeeStatementStatus.Issued, TotalFeeAmount = 50000, DueAt = now.AddDays(7), CreatedAt = now, UpdatedAt = now };
                f.Payment = new PlatformFeePayment { Id = Guid.NewGuid(), Shop = shop, Statement = f.Statement, Amount = 50000,
                    GatewayTransactionId = "123456", Status = PaymentStatus.Pending, PaymentMethod = PaymentMethod.OnlinePayment, CreatedAt = now, UpdatedAt = now };
                f.Db.Add(f.Payment);
                f.Db.Add(new PlatformPayOsConfig { Id = Guid.NewGuid(), UpdatedByUser = owner, VerifiedAt = now, UpdatedAt = now });
                await f.Db.SaveChangesAsync();
                f.Service = ServiceFor(f.Db, f.Api);
                return f;
            }
            catch { await f.DisposeAsync(); throw; }
        }

        public async ValueTask DisposeAsync()
        {
            await Db.Database.EnsureDeletedAsync();
            await Db.DisposeAsync();
        }
    }

    private sealed class Api : IPayOsApiClient
    {
        public JsonElement Response;
        public Func<Task>? BeforeResponse;
        public bool Create;
        public decimal RequestedAmount;
        public int Cancellations;
        public int Reads;
        public bool FailCancellation;
        public JsonElement? CancellationResponse;
        public Func<Task>? BeforeCancellation;
        public async Task<JsonElement> SendAsync(HttpMethod method, string path, PlatformPayOsCredentials credentials, object? body, CancellationToken ct)
        {
            if (BeforeResponse != null) await BeforeResponse();
            if (path.EndsWith("/cancel"))
            {
                Cancellations++;
                if (BeforeCancellation != null) await BeforeCancellation();
                if (FailCancellation) throw AppException.ServiceUnavailable("Simulated provider outage");
                if (CancellationResponse.HasValue) return CancellationResponse.Value;
                return JsonSerializer.SerializeToElement(new { orderCode = 123456, amount = 50000, amountPaid = 0, status = "CANCELLED" });
            }
            if (Create && method == HttpMethod.Post)
            {
                var payload = JsonSerializer.SerializeToElement(body);
                RequestedAmount = payload.GetProperty("amount").GetDecimal();
                return JsonSerializer.SerializeToElement(new { orderCode = payload.GetProperty("orderCode").GetInt64(),
                    amount = RequestedAmount, status = "PENDING", checkoutUrl = "https://pay.payos.vn/web/test", qrCode = "audit-qr" });
            }
            Reads++;
            return Response;
        }
    }
    public class Credentials : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method!.Name == "GetCredentialsAsync"
            ? Task.FromResult(new PlatformPayOsCredentials("client", "key", "checksum")) : throw new NotSupportedException();
    }
    private sealed class Protector : IPayOsCredentialProtector
    {
        public string Protect(PlatformPayOsCredentials credentials) => "test-only";
        public PlatformPayOsCredentials Unprotect(string value) => new("client", "key", "checksum");
    }
    private sealed class Notifications : INotificationQueue
    {
        public Task EnqueueAsync(NotificationTarget target, CreateNotificationCommand command, CancellationToken ct = default) => Task.CompletedTask;
    }
}

internal static class AuditDbOptions
{
    internal static DbContextOptions<AppDbContext> Options(this AppDbContext db)
        => new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(db.Database.GetConnectionString()).Options;
}
