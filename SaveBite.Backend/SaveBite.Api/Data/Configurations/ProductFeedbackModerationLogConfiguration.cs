using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class ProductFeedbackModerationLogConfiguration
    : IEntityTypeConfiguration<ProductFeedbackModerationLog>
{
    public void Configure(
        EntityTypeBuilder<ProductFeedbackModerationLog> builder)
    {
        builder.ToTable("product_feedback_moderation_logs", table =>
        {
            table.HasCheckConstraint(
                "product_feedback_moderation_logs_status_change_check",
                "\"previous_status\" <> \"new_status\"");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ProductFeedbackId)
            .HasColumnName("product_feedback_id")
            .IsRequired();

        builder.Property(x => x.AdminUserId)
            .HasColumnName("admin_user_id")
            .IsRequired();

        builder.Property(x => x.Action)
            .HasColumnName("action")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.PreviousStatus)
            .HasColumnName("previous_status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.NewStatus)
            .HasColumnName("new_status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.ProductFeedbackId,
            x.CreatedAt
        });

        builder.HasIndex(x => new
        {
            x.AdminUserId,
            x.CreatedAt
        });

        builder.HasOne(x => x.ProductFeedback)
            .WithMany(x => x.ModerationLogs)
            .HasForeignKey(x => x.ProductFeedbackId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.AdminUser)
            .WithMany(x => x.ModeratedProductFeedbacks)
            .HasForeignKey(x => x.AdminUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
