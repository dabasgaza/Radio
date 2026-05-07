using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Models.Configurations;

public class EpisodeConfiguration : IEntityTypeConfiguration<Episode>
{
    public void Configure(EntityTypeBuilder<Episode> builder)
    {
        // 1. المفتاح الأساسي
        builder.HasKey(e => e.EpisodeId);

        // 2. إعدادات الخصائص (Properties)
        builder.Property(e => e.EpisodeName)
               .IsRequired()
               .HasMaxLength(300);

        builder.Property(e => e.EpisodeDescription)
               .HasMaxLength(2000);

        builder.Property(e => e.SpecialNotes)
               .HasMaxLength(1000);

        builder.Property(e => e.CancellationReason)
               .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.UpdatedAt)
               .HasDefaultValueSql("GETUTCDATE()");



        builder.Property(e => e.IsActive)
               .HasDefaultValue(true);

        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // 3. العلاقات (Relationships)

        // علاقة حالة الحلقة (EpisodeStatus - Lookup Table)
        // ✨ الحالة مطلوبة (Required)، وبرمجياً يجب ألا تُحذف الحالة إذا كانت هناك حلقات تستخدمها
        builder.HasOne(e => e.EpisodeStatus)
              .WithMany()
              .HasForeignKey(e => e.StatusId)
              .OnDelete(DeleteBehavior.Restrict);

        // علاقة البرنامج (Program)
        // ✨ الحلقة هي طفل للبرنامج. إذا حُذف البرنامج فعلياً، يجب حذف الحلقات التابعة له (Cascade)
        builder.HasOne(e => e.Program)
              .WithMany(p => p.Episodes)
              .HasForeignKey(e => e.ProgramId)
              .OnDelete(DeleteBehavior.Cascade);

        // 4. فهارس الأداء (Performance Indexes)
        // ── فهرس على StatusId لتسريع فلترة الحلقات حسب الحالة ──
        builder.HasIndex(e => e.StatusId)
              .HasDatabaseName("IX_Episodes_StatusId");

        // ── فهرس على ProgramId لتسريع JOIN مع البرامج ──
        builder.HasIndex(e => e.ProgramId)
              .HasDatabaseName("IX_Episodes_ProgramId");

        // ── فهرس على ScheduledExecutionTime لتسريع فلترة التاريخ ──
        builder.HasIndex(e => e.ScheduledExecutionTime)
              .HasDatabaseName("IX_Episodes_ScheduledExecutionTime");

    }
}