using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

// Configure storage for durable order messages awaiting broker delivery.
public sealed class OrderEventOutboxConfiguration : IEntityTypeConfiguration<OrderEventOutbox>
{
    // Map durable outbox messages and the indexes used to dispatch pending events.
    public void Configure(EntityTypeBuilder<OrderEventOutbox> builder)
    {
        builder.ToTable("order_event_outbox");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OrderId).HasColumnName("order_id");
        builder.Property(x => x.RoutingKey).HasColumnName("routing_key").HasMaxLength(150);
        builder.Property(x => x.PayloadJson).HasColumnName("payload_json").HasColumnType("jsonb");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.PublishedAt).HasColumnName("published_at");
        builder.HasIndex(x => new { x.OrderId, x.RoutingKey }).IsUnique();
    }
}
