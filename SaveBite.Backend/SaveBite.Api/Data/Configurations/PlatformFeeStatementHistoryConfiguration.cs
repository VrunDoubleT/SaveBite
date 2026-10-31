using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class PlatformFeeStatementHistoryConfiguration
    : IEntityTypeConfiguration<PlatformFeeStatementHistory>
{
    public void Configure(EntityTypeBuilder<PlatformFeeStatementHistory> builder)
    {
        builder.ToTable("platform_fee_statement_histories");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.StatementId)
            .HasColumnName("statement_id")
            .IsRequired();

        builder.Property(x => x.ChangeType)
            .HasColumnName("change_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PreviousAmount)
            .HasColumnName("previous_amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(x => x.ChangeAmount)
            .HasColumnName("change_amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(x => x.NewAmount)
            .HasColumnName("new_amount")
            .HasPrecision(14, 2)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.RelatedRefundId)
            .HasColumnName("related_refund_id");

        builder.Property(x => x.RelatedOrderId)
            .HasColumnName("related_order_id");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne(x => x.Statement)
            .WithMany(x => x.Histories)
            .HasForeignKey(x => x.StatementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
