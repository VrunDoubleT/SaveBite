using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public sealed class PayOsWebhookSyncStateConfiguration : IEntityTypeConfiguration<PayOsWebhookSyncState>
{
    public void Configure(EntityTypeBuilder<PayOsWebhookSyncState> builder)
    {
        builder.ToTable("payos_webhook_sync_states");
        builder.HasKey(x => x.AccountId);
        builder.Property(x => x.AccountId).HasMaxLength(64);
        builder.Property(x => x.CredentialVersion).HasMaxLength(100);
        builder.Property(x => x.WebhookUrl).HasMaxLength(2048);
        builder.Property(x => x.EndpointSession).HasMaxLength(256);
        builder.Property(x => x.Status).HasMaxLength(32);
        builder.Property(x => x.LastErrorCode).HasMaxLength(128);
        builder.HasIndex(x => new { x.Status, x.NextAttemptAt });
    }
}
