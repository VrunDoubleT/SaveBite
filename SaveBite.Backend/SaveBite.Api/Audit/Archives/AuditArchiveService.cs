using Hangfire;
using Microsoft.EntityFrameworkCore;
using SaveBite.Backend.Data;
using SaveBite.Backend.Exceptions;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Audit.Archives;

public sealed class AuditArchiveService(AppDbContext db, ArchiveStorage storage, IAuditWriter audit, TimeProvider clock)
{
    private const int DeleteBatchSize = 1000;
    private const long ArchiveLockId = 728194610;

    public (int Year, int Month) CurrentMonth()
    {
        var now = clock.GetUtcNow().UtcDateTime.AddHours(7);
        return (now.Year, now.Month);
    }
    public static (DateTime From, DateTime To) Bounds(int year, int month)
    {
        if (year is < 2000 or > 9998 || month is < 1 or > 12) throw AppException.BadRequest("Invalid archive month.");
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc).AddHours(-7);
        return (start, start.AddMonths(1));
    }
    [DisableConcurrentExecution(3600)]
    public async Task ExecuteAsync(CancellationToken ct)
    {
        var current = CurrentMonth();
        var cutoff = Bounds(current.Year, current.Month).From;
        var oldest = await db.AuditLogs.Where(x => x.CreatedAt < cutoff).MinAsync(x => (DateTime?)x.CreatedAt, ct);
        if (oldest == null) return;
        var period = oldest.Value.AddHours(7);
        for (var month = new DateTime(period.Year, period.Month, 1); month < new DateTime(current.Year, current.Month, 1); month = month.AddMonths(1))
            await CreateAsync(month.Year, month.Month, false, ct);
    }
    public async Task<AuditLogArchive?> CreateAsync(int year, int month, bool test, CancellationToken ct)
    {
        var bounds = Bounds(year, month);
        var current = CurrentMonth();
        if (!test && bounds.To > Bounds(current.Year, current.Month).From)
            throw AppException.BadRequest("Only completed months can be archived.");
        // Session lock serializes exports/deletes, including direct test requests.
        await db.Database.OpenConnectionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_lock({ArchiveLockId})", ct);
        var path = Path.GetTempFileName();
        var verified = Path.GetTempFileName();
        AuditLogArchive? archive = null;
        try
        {
            // Check under the export lock so concurrent requests cannot create an empty archive.
            if (!await db.AuditLogs.AnyAsync(x => x.CreatedAt >= bounds.From && x.CreatedAt < bounds.To, ct))
                return null;

            archive = new AuditLogArchive { Id = Guid.NewGuid(), Year = year, Month = month, IsTest = test, CreatedAt = clock.GetUtcNow().UtcDateTime };
            archive.ObjectKey = $"{(test ? "test" : "monthly")}/{year:D4}-{month:D2}/{archive.Id}.zip";
            db.Add(archive);
            await db.SaveChangesAsync(ct);
            archive.RecordCount = await AuditArchiveFile.WriteAsync(path, year, month,
                db.AuditLogs.AsNoTracking()
                    .Where(x => x.CreatedAt >= bounds.From && x.CreatedAt < bounds.To)
                    .OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).AsAsyncEnumerable(), ct);
            archive.Bytes = new FileInfo(path).Length;
            archive.Sha256 = await AuditArchiveFile.HashAsync(path, ct);
            await storage.UploadAsync(archive.ObjectKey, path, ct);
            await storage.DownloadAsync(archive.ObjectKey, verified, ct);
            if (archive.Sha256 != await AuditArchiveFile.HashAsync(verified, ct)) throw new InvalidOperationException("Archive checksum verification failed.");
            var count = 0L;
            await foreach (var _ in AuditArchiveFile.RowsAsync(verified, ct)) count++;
            if (count != archive.RecordCount) throw new InvalidOperationException("Archive row count verification failed.");
            // Exact exported IDs are deleted in the transaction that marks this archive complete.
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            if (!test)
            {
                var ids = new List<Guid>(DeleteBatchSize);
                await foreach (var row in AuditArchiveFile.RowsAsync(verified, ct))
                {
                    ids.Add(row.Id);
                    if (ids.Count == DeleteBatchSize)
                    {
                        await DeleteRowsAsync(ids, bounds, ct);
                        ids.Clear();
                    }
                }
                if (ids.Count > 0) await DeleteRowsAsync(ids, bounds, ct);
            }
            archive.Status = AuditArchiveStatus.Completed;
            archive.CompletedAt = clock.GetUtcNow().UtcDateTime;
            audit.Record(nameof(AuditLogArchive), archive.Id, test ? "TEST_CREATED" : "CREATED", newValues: new { year, month, archive.RecordCount, archive.Bytes, DatabaseRowsDeleted = !test });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return archive;
        }
        catch (Exception error)
        {
            if (archive != null)
            {
                foreach (var pending in db.ChangeTracker.Entries<AuditLog>().Where(x => x.State == EntityState.Added).ToArray())
                    pending.State = EntityState.Detached;
                archive.Status = AuditArchiveStatus.Failed;
                archive.Error = "Archive did not complete. Source audit records were retained. " + error.GetType().Name;
                await db.SaveChangesAsync(CancellationToken.None);
            }
            throw;
        }
        finally
        {
            File.Delete(path);
            File.Delete(verified);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_unlock({ArchiveLockId})", CancellationToken.None);
            await db.Database.CloseConnectionAsync();
        }
    }
    private Task<int> DeleteRowsAsync(List<Guid> ids, (DateTime From, DateTime To) bounds, CancellationToken ct) => db.AuditLogs
        .Where(x => ids.Contains(x.Id) && x.CreatedAt >= bounds.From && x.CreatedAt < bounds.To).ExecuteDeleteAsync(ct);
    public async Task<List<AuditLogArchive>> ListAsync(CancellationToken ct)
    {
        var rows = await db.Set<AuditLogArchive>().AsNoTracking()
            .OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync(ct);
        foreach (var row in rows.Where(x => x.Status == AuditArchiveStatus.Completed))
        {
            if (await storage.ExistsAsync(row.ObjectKey, ct)) continue;
            await MarkMissingAsync(row.Id, ct);
            row.Status = AuditArchiveStatus.Missing;
        }
        return rows;
    }
    private Task<int> MarkMissingAsync(Guid id, CancellationToken ct) => db.Set<AuditLogArchive>()
        .Where(x => x.Id == id && x.Status == AuditArchiveStatus.Completed)
        .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, AuditArchiveStatus.Missing), ct);

    public async Task<AuditLogArchive> FindAsync(Guid id, CancellationToken ct) =>
        await db.Set<AuditLogArchive>().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.Status == AuditArchiveStatus.Completed, ct)
        ?? throw AppException.NotFound("Archive not found.");
    public async Task<string> DownloadAsync(AuditLogArchive archive, CancellationToken ct)
    {
        var path = Path.GetTempFileName();
        try
        {
            await storage.DownloadAsync(archive.ObjectKey, path, ct);
            if (await AuditArchiveFile.HashAsync(path, ct) != archive.Sha256) throw new InvalidOperationException("Archive integrity check failed.");
            return path;
        }
        catch (Amazon.S3.AmazonS3Exception error) when (ArchiveStorage.IsMissing(error))
        {
            File.Delete(path);
            await MarkMissingAsync(archive.Id, ct);
            throw AppException.NotFound("This archive file is no longer available on storage.");
        }
        catch { File.Delete(path); throw; }
    }
    public async Task DeleteCurrentTestAsync(CancellationToken ct)
    {
        var current = CurrentMonth();
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({ArchiveLockId})", ct);
        var rows = await db.Set<AuditLogArchive>().Where(x => x.IsTest && x.Year == current.Year && x.Month == current.Month && x.Status != AuditArchiveStatus.Deleted).ToListAsync(ct);
        foreach (var row in rows)
        {
            await storage.DeleteAsync(row.ObjectKey, ct);
            row.Status = AuditArchiveStatus.Deleted;
        }
        audit.Record(nameof(AuditLogArchive), Guid.NewGuid(), "TEST_DELETED", newValues: new { current.Year, current.Month, Count = rows.Count });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
    }
}
