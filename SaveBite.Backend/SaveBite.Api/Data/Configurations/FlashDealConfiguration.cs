using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class FlashDealConfiguration : IEntityTypeConfiguration<FlashDeal>
{
    public void Configure(EntityTypeBuilder<FlashDeal> builder)
    {
        builder.ToTable("flash_deals");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ShopId)
            .HasColumnName("shop_id")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.SaleStartTime)
            .HasColumnName("sale_start_time")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.OrderEndTime)
            .HasColumnName("order_end_time")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ShopClosingTime)
            .HasColumnName("shop_closing_time")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.Product)
            .WithMany(x => x.FlashDeals)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Shop)
            .WithMany(x => x.FlashDeals)
            .HasForeignKey(x => x.ShopId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasIndex(x => new { x.Status, x.CreatedAt })
            .HasDatabaseName("ix_flash_deals_status_created_at");
        
    }
}
