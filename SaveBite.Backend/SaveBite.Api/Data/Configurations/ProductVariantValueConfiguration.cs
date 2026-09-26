using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class ProductVariantValueConfiguration
    : IEntityTypeConfiguration<ProductVariantValue>
{
    public void Configure(
        EntityTypeBuilder<ProductVariantValue> builder)
    {
        builder.ToTable("product_variant_values");

        builder.HasKey(x => new
        {
            x.VariantId,
            x.AttributeValueId
        });

        builder.Property(x => x.VariantId)
            .HasColumnName("variant_id")
            .IsRequired();

        builder.Property(x => x.AttributeValueId)
            .HasColumnName("attribute_value_id")
            .IsRequired();

        builder.HasOne(x => x.Variant)
            .WithMany(x => x.VariantValues)
            .HasForeignKey(x => x.VariantId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.AttributeValue)
            .WithMany(x => x.VariantValues)
            .HasForeignKey(x => x.AttributeValueId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
