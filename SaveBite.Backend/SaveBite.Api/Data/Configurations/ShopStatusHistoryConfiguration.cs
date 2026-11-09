using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public sealed class ShopStatusHistoryConfiguration : IEntityTypeConfiguration<ShopStatusHistory>
{
    public void Configure(EntityTypeBuilder<ShopStatusHistory> b)
    {
        b.ToTable("shop_status_histories");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.ShopId).HasColumnName("shop_id");
        b.Property(x => x.FromStatus).HasColumnName("from_status").HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.ToStatus).HasColumnName("to_status").HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.WasSuspendedByAdmin).HasColumnName("was_suspended_by_admin");
        b.Property(x => x.WasSuspendedForPlatformFees).HasColumnName("was_suspended_for_platform_fees");
        b.Property(x => x.WasSuspendedForOverdueRefunds).HasColumnName("was_suspended_for_overdue_refunds");
        b.Property(x => x.SuspendedByAdmin).HasColumnName("suspended_by_admin");
        b.Property(x => x.SuspendedForPlatformFees).HasColumnName("suspended_for_platform_fees");
        b.Property(x => x.SuspendedForOverdueRefunds).HasColumnName("suspended_for_overdue_refunds");
        b.Property(x => x.ChangedByType).HasColumnName("changed_by_type").HasMaxLength(30);
        b.Property(x => x.ChangedByUserId).HasColumnName("changed_by_user_id");
        b.Property(x => x.Reason).HasColumnName("reason").HasColumnType("text");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.HasIndex(x => new { x.ShopId, x.CreatedAt });
        b.HasOne(x => x.Shop).WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ChangedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
