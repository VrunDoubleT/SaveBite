using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;
namespace SaveBite.Backend.Data.Configurations;

public sealed class TrustOrderProcessingConfiguration : IEntityTypeConfiguration<TrustOrderProcessing>
{
    // One terminal order can affect trust and accumulated progress only once.
    public void Configure(EntityTypeBuilder<TrustOrderProcessing> builder)
    {
        builder.ToTable("trust_order_processing");
        builder.Property(x => x.CreditedOrderValue).HasColumnName("credited_order_value").HasPrecision(14, 2);
        builder.HasKey(x => x.OrderId);
        builder.Property(x => x.OrderId).HasColumnName("order_id");
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        builder.HasOne<Order>().WithOne().HasForeignKey<TrustOrderProcessing>(x => x.OrderId).OnDelete(DeleteBehavior.NoAction);
    }
}
