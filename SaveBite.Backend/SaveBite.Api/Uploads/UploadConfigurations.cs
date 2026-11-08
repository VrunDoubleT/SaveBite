using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace SaveBite.Backend.Uploads;
public sealed class UploadSessionConfiguration : IEntityTypeConfiguration<UploadSession>
{
    public void Configure(EntityTypeBuilder<UploadSession> b)
    {
        b.ToTable("upload_sessions"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
        b.Property(x => x.Purpose).HasColumnName("purpose").HasMaxLength(50).IsRequired();
        b.Property(x => x.TargetId).HasColumnName("target_id");
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsConcurrencyToken().IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
        b.Property(x => x.ResultId).HasColumnName("result_id");
        b.HasIndex(x => new { x.UserId, x.Status }).HasDatabaseName("ix_upload_sessions_user_status");
        b.HasIndex(x => x.ExpiresAt).HasDatabaseName("ix_upload_sessions_expires_at");
        b.HasOne<Models.Entities.User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class UploadAssetConfiguration : IEntityTypeConfiguration<UploadAsset>
{
    public void Configure(EntityTypeBuilder<UploadAsset> b)
    {
        b.ToTable("upload_assets"); b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.Property(x => x.SessionId).HasColumnName("session_id").IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsConcurrencyToken().IsRequired();
        b.Property(x => x.Url).HasColumnName("url").HasColumnType("text").IsRequired();
        b.Property(x => x.PublicId).HasColumnName("public_id").HasMaxLength(255).IsRequired();
        b.Property(x => x.ResourceType).HasColumnName("resource_type").HasMaxLength(10).IsRequired();
        b.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(255).IsRequired();
        b.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
        b.Property(x => x.Bytes).HasColumnName("bytes").IsRequired();
        b.Property(x => x.MetadataJson).HasColumnName("metadata_json").HasColumnType("jsonb");
        b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(x => x.ExpiresAt).HasColumnName("expires_at").IsRequired();
        b.HasIndex(x => x.Url).HasDatabaseName("ix_upload_assets_url");
        b.HasIndex(x => new { x.Status, x.ExpiresAt }).HasDatabaseName("ix_upload_assets_status_expires_at");
        b.HasOne(x => x.Session).WithMany(x => x.Assets).HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
