using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public sealed class CheckoutSessionConfiguration : IEntityTypeConfiguration<CheckoutSession>
{
    public void Configure(EntityTypeBuilder<CheckoutSession> b)
    {
        b.ToTable("checkout_sessions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.CustomerId).HasColumnName("customer_id");
        b.Property(x => x.ShopId).HasColumnName("shop_id");
        b.Property(x => x.QuoteId).HasColumnName("quote_id");
        b.Property(x => x.RequestId).HasColumnName("request_id");
        b.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.SnapshotJson).HasColumnName("snapshot_json").HasColumnType("jsonb");
        b.Property(x => x.PaymentExpiresAt).HasColumnName("payment_expires_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.HasIndex(x => new { x.CustomerId, x.RequestId }).IsUnique();
        b.HasIndex(x => new { x.CustomerId, x.QuoteId }).IsUnique();
        b.HasIndex(x => new { x.Status, x.PaymentExpiresAt });
        b.HasOne<User>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<Shop>().WithMany().HasForeignKey(x => x.ShopId).OnDelete(DeleteBehavior.NoAction);
    }
}
