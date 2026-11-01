using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Data.Configurations;

public class OrderRefundRequestConfiguration : IEntityTypeConfiguration<OrderRefundRequest>
{
    public void Configure(EntityTypeBuilder<OrderRefundRequest> b)
    {
        b.ToTable("order_refund_requests");
        b.HasKey(x => x.OrderId);
        b.Property(x => x.OrderId).HasColumnName("order_id");
        b.Property(x => x.RefundId).HasColumnName("refund_id");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.HasIndex(x => x.RefundId).IsUnique();
        b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Refund).WithMany().HasForeignKey(x => x.RefundId).OnDelete(DeleteBehavior.Restrict);
    }
}
