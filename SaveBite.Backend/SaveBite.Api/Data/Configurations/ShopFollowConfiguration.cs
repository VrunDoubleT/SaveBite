using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public sealed class ShopFollowConfiguration : IEntityTypeConfiguration<ShopFollow>
{
    public void Configure(EntityTypeBuilder<ShopFollow> b)
    {
        b.ToTable("shop_follows", t => t.HasCheckConstraint("ck_shop_follows_notification_status", "notification_status IN ('Disabled', 'Enabled')"));
        b.HasKey(f => new { f.CustomerId, f.ShopId });
        b.Property(f => f.CustomerId).HasColumnName("customer_id");
        b.Property(f => f.ShopId).HasColumnName("shop_id");
        b.Property(f => f.NotificationStatus).HasColumnName("notification_status").HasConversion<string>().HasMaxLength(20);
        b.Property(f => f.FollowedAt).HasColumnName("followed_at");
        b.Property(f => f.UpdatedAt).HasColumnName("updated_at");
        b.HasOne(f => f.Customer).WithMany().HasForeignKey(f => f.CustomerId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(f => f.Shop).WithMany().HasForeignKey(f => f.ShopId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(f => new { f.ShopId, f.NotificationStatus, f.FollowedAt });
        b.HasIndex(f => new { f.CustomerId, f.FollowedAt });
    }
}
