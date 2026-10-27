using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Models.Entities;
using SaveBite.Backend.Services.Interfaces;
namespace SaveBite.Backend.BackgroundJobs.Jobs;

public class RefundDeadlineRecoveryJob
{
    private readonly AppDbContext _db;
    private readonly IRefundService _refunds;
    public RefundDeadlineRecoveryJob(AppDbContext db, IRefundService refunds) { _db = db; _refunds = refunds; }
    // Recover pre-existing confirmed requests in batches; each schedule is rechecked under the order lock.
    public async Task ExecuteAsync(CancellationToken ct)
    {
        while (true)
        {
            var ids = await _db.Refunds.AsNoTracking().Where(f => f.DeadlineJobId == null
                && ((f.RefundType == RefundType.ShopCancellation && f.Status == RefundStatus.AwaitingCustomerDetails)
                    || (f.DestinationConfirmedAt != null && (f.Status == RefundStatus.PendingVerification || f.Status == RefundStatus.AwaitingTransfer))))
                .OrderBy(f => f.Id).Select(f => f.Id).Take(100).ToListAsync(ct);
            if (ids.Count == 0) return;
            foreach (var id in ids) await _refunds.EnsureDeadlineScheduledAsync(id, ct);
        }
    }
}
