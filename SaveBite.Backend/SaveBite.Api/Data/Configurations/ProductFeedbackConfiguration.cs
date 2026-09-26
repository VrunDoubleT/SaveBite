using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class ProductFeedbackConfiguration
    : IEntityTypeConfiguration<ProductFeedback>
{
    public void Configure(EntityTypeBuilder<ProductFeedback> builder)
    {
        builder.ToTable("product_feedbacks", table =>
        {
            table.HasCheckConstraint(
                "product_feedbacks_rating_check",
                "\"rating\" BETWEEN 1 AND 5");

            table.HasCheckConstraint(
                "product_feedbacks_hidden_at_check",
                "(\"status\" = 'Visible' AND \"hidden_at\" IS NULL) OR " +
                "(\"status\" <> 'Visible' AND \"hidden_at\" IS NOT NULL)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.OrderItemId)
            .HasColumnName("order_item_id")
            .IsRequired();

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.Rating)
            .HasColumnName("rating")
            .IsRequired();

        builder.Property(x => x.Comment)
            .HasColumnName("comment")
            .HasMaxLength(2000);

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(x => x.HiddenAt)
            .HasColumnName("hidden_at");
        
        builder.HasIndex(x => x.OrderItemId)
            .IsUnique();
        
        builder.HasIndex(x => new
        {
            x.ProductId,
            x.CreatedAt
        })
        .HasFilter("\"status\" = 'Visible'")
        .HasDatabaseName("ix_product_feedbacks_visible_product_created_at");
        
        builder.HasIndex(x => new
        {
            x.UserId,
            x.CreatedAt
        });

        builder.HasOne(x => x.OrderItem)
            .WithOne(x => x.Feedback)
            .HasForeignKey<ProductFeedback>(x => x.OrderItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Product)
            .WithMany(x => x.Feedbacks)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.User)
            .WithMany(x => x.ProductFeedbacks)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
