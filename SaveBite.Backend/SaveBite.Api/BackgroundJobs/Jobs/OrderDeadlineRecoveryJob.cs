using SaveBite.Backend.Configurations;
using Hangfire;
using SaveBite.Backend.Models.Enums;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.BackgroundJobs.Jobs;

public sealed class OrderDeadlineRecoveryJob(IServiceScopeFactory scopes, TimeProvider clock, ILogger<OrderDeadlineRecoveryJob> logger)
{
    [DisableConcurrentExecution(TimingConfiguration.Jobs.ConcurrencyLockTimeoutSeconds)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        await Group("remaining-payments", () => RecoveryBatch.RunAsync(scopes, logger, "remaining-payment", db => db.Orders
            .Where(o => o.Payments.Any(p => p.PaymentType == PaymentType.Remaining && p.Method == PaymentMethod.OnlinePayment
                && p.Status == PaymentStatus.Pending && p.GatewayCreationAttempted && p.CreatedAt <= now.AddMinutes(-1))).Select(o => o.Id),
            async (sp, id) => { await sp.GetRequiredService<ICheckoutPaymentService>().ReconcileOrderAsync(id, ct: ct); }, ct), ct);
        await Group("checkout-payments", () => RecoveryBatch.RunAsync(scopes, logger, "checkout-payment", db => db.CheckoutSessions
            .Where(s => s.Status == OrderStatus.AwaitingPayment && s.CreatedAt <= now.AddMinutes(-1)).Select(s => s.Id),
            async (sp, id) => { await sp.GetRequiredService<ICheckoutPaymentService>().ReconcileOrderAsync(id, ct: ct); }, ct), ct);
        await Group("payments", () => RecoveryBatch.RunAsync(scopes, logger, "order-payment", db => db.Orders
            .Where(o => o.Status == OrderStatus.AwaitingPayment && o.CreatedAt <= now.AddMinutes(-1)
                && o.Payments.Any(p => p.Status == PaymentStatus.Pending && p.Amount > 0
                    && (p.PaymentType == PaymentType.Full || p.PaymentType == PaymentType.Deposit)))
            .Select(o => o.Id), async (sp, id) => { await sp.GetRequiredService<ICheckoutPaymentService>().ReconcileOrderAsync(id, ct: ct); }, ct), ct);
        await Group("confirmation", () => RecoveryBatch.RunAsync(scopes, logger, "order-confirmation", db => db.Orders
            .Where(o => o.Status == OrderStatus.Pending && !o.PaymentReviewRequired
                && o.Payments.Any(p => p.Status == PaymentStatus.Succeeded
                    && (p.PaymentType == PaymentType.Full || p.PaymentType == PaymentType.Deposit)
                    && (p.PaidAt ?? o.CreatedAt) <= now.AddMinutes(-TimingConfiguration.Orders.ShopConfirmationMinutes)))
            .Select(o => o.Id), (sp, id) => sp.GetRequiredService<IShopOrderService>().ExpireConfirmationAsync(id, ct), ct), ct);
        await Group("no-show", () => RecoveryBatch.RunAsync(scopes, logger, "order-no-show", db => db.Orders
            .Where(o => o.HardDeadlineAt <= now && (o.Status == OrderStatus.Confirmed
                || o.Status == OrderStatus.Preparing || o.Status == OrderStatus.ReadyForPickup))
            .Select(o => o.Id), (sp, id) => sp.GetRequiredService<IShopOrderService>().MarkNoShowAsync(id, ct), ct), ct);
        await Group("resale-decision", () => RecoveryBatch.RunAsync(scopes, logger, "order-resale-decision", db => db.Orders
            .Where(o => o.ResaleDecisionDeadlineAt <= now && o.Items.Any(i => i.ResaleDecisionStatus == ResaleDecisionStatus.Pending))
            .Select(o => o.Id), (sp, id) => sp.GetRequiredService<SaveBite.Backend.Services.Implementations.OrderResaleService>().ExpireAsync(id, ct), ct), ct);
        await Group("reminder", () => RecoveryBatch.RunAsync(scopes, logger, "order-reminder", db => db.Orders
            .Where(o => o.PickupReminderEmailSentAt == null && o.PickupReminderEmailQueuedAt == null && o.HardDeadlineAt > now && o.HardDeadlineAt <= now.AddMinutes(TimingConfiguration.Orders.PickupReminderLeadMinutes)
                && (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Preparing || o.Status == OrderStatus.ReadyForPickup))
            .Select(o => o.Id), async (sp, id) =>
            {
                var order = await sp.GetRequiredService<SaveBite.Backend.Data.AppDbContext>().Orders.FindAsync(new object[] { id }, ct);
                if (order?.HardDeadlineAt is not DateTime deadline) return;
                if (SaveBite.Backend.Helpers.PickupReminderPolicy.DueAt(order.ConfirmedAt ?? order.CreatedAt, deadline) <= now)
                    await sp.GetRequiredService<PickupDeadlineReminderJob>().ExecuteAsync(id, deadline, ct);
            }, ct), ct);
    }
    private async Task Group(string name, Func<Task> run, CancellationToken ct)
    {
        try { await run(); }
        catch (Exception ex) when (!ct.IsCancellationRequested) { logger.LogError(ex, "Order recovery group {Group} failed.", name); }
    }
}
