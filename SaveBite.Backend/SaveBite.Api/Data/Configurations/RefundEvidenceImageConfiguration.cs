using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class RefundEvidenceImageConfiguration
    : IEntityTypeConfiguration<RefundEvidenceImage>
{
    public void Configure(
        EntityTypeBuilder<RefundEvidenceImage> builder)
    {
        builder.ToTable("refund_evidence_images");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.RefundId)
            .HasColumnName("refund_id")
            .IsRequired();

        builder.Property(x => x.ImageUrl)
            .HasColumnName("image_url")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.ImageType)
            .HasColumnName("image_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.UploadedBy)
            .HasColumnName("uploaded_by")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.Refund)
            .WithMany(x => x.EvidenceImages)
            .HasForeignKey(x => x.RefundId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(x => x.UploadedByUser)
            .WithMany(x => x.UploadedRefundEvidenceImages)
            .HasForeignKey(x => x.UploadedBy)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
