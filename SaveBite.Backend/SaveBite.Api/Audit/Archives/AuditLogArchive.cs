using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace SaveBite.Backend.Audit.Archives;
[NoAudit]
public sealed class AuditLogArchive
{
    public Guid Id { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public bool IsTest { get; set; }
    public string ObjectKey { get; set; } = "";
    public string Status { get; set; } = AuditArchiveStatus.Uploading;
    public long RecordCount { get; set; }
    public long Bytes { get; set; }
    public string Sha256 { get; set; } = "";
    public string? Error { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
public sealed class AuditLogArchiveConfiguration : IEntityTypeConfiguration<AuditLogArchive>
{
    public void Configure(EntityTypeBuilder<AuditLogArchive> builder)
    {
        builder.ToTable("audit_log_archives");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.Year, x.Month, x.IsTest });
        builder.Property(x => x.ObjectKey).HasMaxLength(500);
        builder.Property(x => x.Status).HasMaxLength(30);
        builder.Property(x => x.Sha256).HasMaxLength(64);
        builder.Property(x => x.Error).HasMaxLength(1000);
    }
}
