using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class ProductFeedbackImageConfiguration : IEntityTypeConfiguration<ProductFeedbackImage>
{
    public void Configure(EntityTypeBuilder<ProductFeedbackImage> builder)
    {
        builder.ToTable("product_feedback_images");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ProductFeedbackId)
            .HasColumnName("product_feedback_id")
            .IsRequired();

        builder.Property(x => x.ImageUrl)
            .HasColumnName("image_url")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(x => x.PublicId)
            .HasColumnName("public_id")
            .HasMaxLength(255);

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.ProductFeedbackId);

        builder.HasOne(x => x.ProductFeedback)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.ProductFeedbackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
