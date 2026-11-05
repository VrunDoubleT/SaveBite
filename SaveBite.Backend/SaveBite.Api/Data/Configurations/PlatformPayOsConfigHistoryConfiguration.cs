using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public sealed class PlatformPayOsConfigHistoryConfiguration : IEntityTypeConfiguration<PlatformPayOsConfigHistory>
{
    public void Configure(EntityTypeBuilder<PlatformPayOsConfigHistory> builder)
    {
        builder.ToTable("platform_payos_config_history");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(x => x.ChangedByUserId).HasColumnName("changed_by_user_id").IsRequired();
        builder.Property(x => x.ChangedFieldsJson).HasColumnName("changed_fields_json").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.PreviousMaskedClientId).HasColumnName("previous_masked_client_id").HasMaxLength(255);
        builder.Property(x => x.MaskedClientId).HasColumnName("masked_client_id").HasMaxLength(255).IsRequired();
        builder.Property(x => x.ChangedAt).HasColumnName("changed_at").HasColumnType("timestamp with time zone").IsRequired();

        builder.HasOne(x => x.ChangedByUser)
            .WithMany()
            .HasForeignKey(x => x.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ChangedAt);
    }
}
