using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public sealed class EmailDeliveryConfiguration : IEntityTypeConfiguration<EmailDelivery>
{
    public void Configure(EntityTypeBuilder<EmailDelivery> b)
    {
        b.ToTable("email_deliveries");
        b.HasKey(e => e.Id);
        b.Property(e => e.Id).HasColumnName("id").HasDefaultValueSql("gen_random_uuid()");
        b.HasIndex(e => e.DeliveryKey).IsUnique();
        b.Property(e => e.DeliveryKey).HasColumnName("delivery_key").HasMaxLength(200);
        b.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);
        b.Property(e => e.UpdatedAt).HasColumnName("updated_at");
        b.Property(e => e.SentAt).HasColumnName("sent_at");
        b.HasIndex(e => new { e.Status, e.UpdatedAt });
    }
}
