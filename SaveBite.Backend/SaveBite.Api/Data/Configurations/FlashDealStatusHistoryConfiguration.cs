using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class FlashDealStatusHistoryConfiguration : IEntityTypeConfiguration<FlashDealStatusHistory>
{
    public void Configure(EntityTypeBuilder<FlashDealStatusHistory> builder)
    {
        builder.ToTable("flash_deal_status_history");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.FlashDealId).HasColumnName("flash_deal_id").IsRequired();
        builder.Property(x => x.FromStatus)
            .HasColumnName("from_status")
            .HasConversion<string>()
            .HasMaxLength(30);
        builder.Property(x => x.ToStatus)
            .HasColumnName("to_status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        builder.Property(x => x.ChangedByType).HasColumnName("changed_by_type").HasMaxLength(30).IsRequired();
        builder.Property(x => x.ChangedByUserId).HasColumnName("changed_by_user_id");
        builder.Property(x => x.Note).HasColumnName("note").HasColumnType("text");
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.FlashDeal)
            .WithMany(x => x.StatusHistory)
            .HasForeignKey(x => x.FlashDealId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(x => new { x.FlashDealId, x.CreatedAt });
    }
}
