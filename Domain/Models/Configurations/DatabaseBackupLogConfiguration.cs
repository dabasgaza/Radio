using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Models.Configurations;

public class DatabaseBackupLogConfiguration : IEntityTypeConfiguration<DatabaseBackupLog>
{
    public void Configure(EntityTypeBuilder<DatabaseBackupLog> builder)
    {
        builder.HasKey(e => e.DatabaseBackupLogId);

        builder.Property(e => e.BackupPath)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(e => e.BackupType)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(e => e.Status)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(e => e.ErrorMessage)
               .HasMaxLength(2000);

        builder.Property(e => e.CloudUrl)
               .HasMaxLength(1000);

        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.IsActive)
               .HasDefaultValue(true);

        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        builder.HasIndex(e => e.CreatedAt)
               .HasDatabaseName("IX_DatabaseBackupLogs_CreatedAt");
    }
}
