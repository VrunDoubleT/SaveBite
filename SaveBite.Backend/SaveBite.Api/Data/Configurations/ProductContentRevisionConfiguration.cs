using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class ProductContentRevisionConfiguration
    : IEntityTypeConfiguration<ProductContentRevision>
{
    public void Configure(
        EntityTypeBuilder<ProductContentRevision> builder)
    {
        builder.ToTable("product_content_revisions", table =>
        {
            table.HasCheckConstraint(
                "product_content_revisions_number_check",
                "\"revision_number\" >= 1");

            table.HasCheckConstraint(
                "product_content_revisions_review_check",
                "(\"status\" = 'Pending' AND " +
                    "\"reviewed_by_user_id\" IS NULL AND " +
                    "\"reviewed_at\" IS NULL) OR " +
                "(\"status\" IN ('Approved', 'NeedsRevision', 'Suspended') " +
                    "AND \"reviewed_by_user_id\" IS NOT NULL " +
                    "AND \"reviewed_at\" IS NOT NULL)");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(x => x.RevisionNumber)
            .HasColumnName("revision_number")
            .IsRequired();

        builder.Property(x => x.SubmittedByUserId)
            .HasColumnName("submitted_by_user_id")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasColumnName("description");

        builder.Property(x => x.CategoryId)
            .HasColumnName("category_id")
            .IsRequired();

        builder.Property(x => x.ImagesJson)
            .HasColumnName("images_json")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.ReviewedByUserId)
            .HasColumnName("reviewed_by_user_id");

        builder.Property(x => x.ReviewNote)
            .HasColumnName("review_note")
            .HasMaxLength(2000);

        builder.Property(x => x.SubmittedAt)
            .HasColumnName("submitted_at")
            .IsRequired();

        builder.Property(x => x.ReviewedAt)
            .HasColumnName("reviewed_at");
        
        builder.HasIndex(x => new
        {
            x.ProductId,
            x.RevisionNumber
        }).IsUnique();
        
        builder.HasIndex(x => x.ProductId)
            .IsUnique()
            .HasFilter("\"status\" = 'Pending'")
            .HasDatabaseName(
                "ux_product_content_revisions_pending_product");

        builder.HasOne(x => x.Product)
            .WithMany(x => x.ContentRevisions)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SubmittedByUser)
            .WithMany(x => x.SubmittedProductContentRevisions)
            .HasForeignKey(x => x.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ReviewedByUser)
            .WithMany(x => x.ReviewedProductContentRevisions)
            .HasForeignKey(x => x.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Category)
            .WithMany(x => x.ProductContentRevisions)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
