using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Data.Configurations;

public class RefundStatusHistoryConfiguration : IEntityTypeConfiguration<RefundStatusHistory>
{
    public void Configure(EntityTypeBuilder<RefundStatusHistory> builder)
    {
        builder.ToTable("refund_status_history");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.RefundId).HasColumnName("refund_id");
        builder.Property(x => x.FromStatus).HasColumnName("from_status").HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.ToStatus).HasColumnName("to_status").HasConversion<string>().HasMaxLength(30);
        builder.Property(x => x.ActorId).HasColumnName("actor_id");
        builder.Property(x => x.ActorRole).HasColumnName("actor_role").HasMaxLength(30);
        builder.Property(x => x.ActorName).HasColumnName("actor_name").HasMaxLength(200);
        builder.Property(x => x.Decision).HasColumnName("decision").HasMaxLength(50);
        builder.Property(x => x.Note).HasColumnName("note");
        builder.Property(x => x.TransferReference).HasColumnName("transfer_reference").HasMaxLength(150);
        builder.Property(x => x.ProofUrl).HasColumnName("proof_url");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.HasOne(x => x.Refund).WithMany(x => x.History).HasForeignKey(x => x.RefundId).OnDelete(DeleteBehavior.NoAction);
        builder.HasIndex(x => new { x.RefundId, x.CreatedAt });
    }
}
