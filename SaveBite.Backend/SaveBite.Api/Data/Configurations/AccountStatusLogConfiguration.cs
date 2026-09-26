using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SaveBite.Backend.Models.Entities;

namespace SaveBite.Backend.Data.Configurations;

public class AccountStatusLogConfiguration
    : IEntityTypeConfiguration<AccountStatusLog>
{
    public void Configure(EntityTypeBuilder<AccountStatusLog> builder)
    {
        builder.ToTable("account_status_logs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(x => x.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(x => x.AdminId)
            .HasColumnName("admin_id")
            .IsRequired();

        builder.Property(x => x.Action)
            .HasColumnName("action")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Reason)
            .HasColumnName("reason")
            .HasColumnType("text");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        
        builder.HasOne(x => x.User)
            .WithMany(x => x.AccountStatusLogs)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.NoAction);
        
        builder.HasOne(x => x.Admin)
            .WithMany(x => x.AdminAccountStatusLogs)
            .HasForeignKey(x => x.AdminId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
