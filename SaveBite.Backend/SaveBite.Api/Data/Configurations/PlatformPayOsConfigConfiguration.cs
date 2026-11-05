using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public sealed class PlatformPayOsConfigConfiguration : IEntityTypeConfiguration<PlatformPayOsConfig>
{
    public void Configure(EntityTypeBuilder<PlatformPayOsConfig> builder)
    {
        builder.ToTable("platform_payos_configs");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.ClientId).HasColumnName("client_id").HasMaxLength(255).IsRequired();
        builder.Property(x => x.ProtectedApiKey).HasColumnName("protected_api_key").HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ProtectedChecksumKey).HasColumnName("protected_checksum_key").HasMaxLength(2000).IsRequired();
        builder.Property(x => x.UpdatedByUserId).HasColumnName("updated_by_user_id").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp with time zone").IsRequired();

        builder.HasOne(x => x.UpdatedByUser)
            .WithMany()
            .HasForeignKey(x => x.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
