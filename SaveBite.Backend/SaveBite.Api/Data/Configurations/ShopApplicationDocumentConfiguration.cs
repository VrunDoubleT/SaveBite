using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class ShopApplicationDocumentConfiguration
    : IEntityTypeConfiguration<ShopApplicationDocument>
{
    public void Configure(
        EntityTypeBuilder<ShopApplicationDocument> builder)
    {
        builder.ToTable("shop_application_documents", table =>
        {
            table.HasCheckConstraint(
                "ck_shop_application_document_revision",
                "\"revision_number\" > 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.ApplicationId)
            .HasColumnName("application_id")
            .IsRequired();

        builder.Property(x => x.DocumentType)
            .HasColumnName("document_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.FileUrl)
            .HasColumnName("file_url")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RevisionNumber)
            .HasColumnName("revision_number")
            .IsRequired();

        builder.Property(x => x.UploadedBy)
            .HasColumnName("uploaded_by")
            .IsRequired();

        builder.Property(x => x.UploadedAt)
            .HasColumnName("uploaded_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.IsCurrent)
            .HasColumnName("is_current")
            .HasDefaultValue(true)
            .IsRequired();
        
        builder.HasIndex(x => new
            {
                x.ApplicationId,
                x.DocumentType
            })
            .IsUnique()
            .HasDatabaseName(
                "ux_shop_application_current_document")
            .HasFilter("\"is_current\" = TRUE");

        builder.HasOne(x => x.Application)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.ApplicationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.UploadedByUser)
            .WithMany(x => x.UploadedShopApplicationDocuments)
            .HasForeignKey(x => x.UploadedBy)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
