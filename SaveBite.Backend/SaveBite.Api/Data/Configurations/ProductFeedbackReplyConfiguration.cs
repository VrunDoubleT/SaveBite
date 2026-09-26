using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class ProductFeedbackReplyConfiguration
    : IEntityTypeConfiguration<ProductFeedbackReply>
{
    public void Configure(
        EntityTypeBuilder<ProductFeedbackReply> builder)
    {
        builder.ToTable("product_feedback_replies");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ProductFeedbackId)
            .HasColumnName("product_feedback_id")
            .IsRequired();

        builder.Property(x => x.RepliedByUserId)
            .HasColumnName("replied_by_user_id")
            .IsRequired();

        builder.Property(x => x.Content)
            .HasColumnName("content")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();
        
        builder.HasIndex(x => x.ProductFeedbackId)
            .IsUnique();

        builder.HasOne(x => x.ProductFeedback)
            .WithOne(x => x.Reply)
            .HasForeignKey<ProductFeedbackReply>(
                x => x.ProductFeedbackId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.RepliedByUser)
            .WithMany(x => x.ProductFeedbackReplies)
            .HasForeignKey(x => x.RepliedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
