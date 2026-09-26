using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class ShopApplicationReviewLogConfiguration
    : IEntityTypeConfiguration<ShopApplicationReviewLog>
{
    public void Configure(
        EntityTypeBuilder<ShopApplicationReviewLog> builder)
    {
        builder.ToTable("shop_application_review_logs", table =>
        {
            table.HasCheckConstraint(
                "ck_shop_application_review_revision",
                "\"revision_number\" > 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ApplicationId)
            .HasColumnName("application_id")
            .IsRequired();

        builder.Property(x => x.AdminId)
            .HasColumnName("admin_id");

        builder.Property(x => x.FromStatus)
            .HasColumnName("from_status")
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(x => x.ToStatus)
            .HasColumnName("to_status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.RevisionNumber)
            .HasColumnName("revision_number")
            .IsRequired();

        builder.Property(x => x.Note)
            .HasColumnName("note")
            .HasColumnType("text");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.Application)
            .WithMany(x => x.ReviewLogs)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.Admin)
            .WithMany(x => x.ReviewedShopApplications)
            .HasForeignKey(x => x.AdminId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
