using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Models.Configurations;

public class ExecutionLogConfiguration : IEntityTypeConfiguration<ExecutionLog>
{
    public void Configure(EntityTypeBuilder<ExecutionLog> builder)
    {
        // 1. المفتاح الأساسي (إزالة الاسم القبيح المولد آلياً)
        builder.HasKey(e => e.ExecutionLogId);

        // 2. إعدادات الخصائص (Properties)
        builder.Property(e => e.ExecutionNotes)
               .HasMaxLength(2000);

        builder.Property(e => e.IssuesEncountered)
               .HasMaxLength(2000);

        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.IsActive)
               .HasDefaultValue(true);

        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // 3. العلاقات (Relationships)

        // علاقة الحلقة (Episode)
        // ✨ سجل التنفيذ هو دليل تاريخي. إذا حُذفت الحلقة، لا نريد حذف سجل التنفيذ.
        // Restrict يمنع حذف الحلقة إذا كان لها سجل تنفيذ (ما لم تحذف السجل أولاً).
        builder.HasOne(d => d.Episode)
              .WithMany(p => p.ExecutionLogs)
              .HasForeignKey(d => d.EpisodeId)
              .OnDelete(DeleteBehavior.Restrict);

        // علاقة "نفّذ بواسطة" (ExecutedByUser)
        // ✨ نفس المنطق: إذا غادر الموظف المؤسسة وحُذف حسابه، يجب أن يظل سجل التنفيذ محتفظاً باسمه.
        // ملاحظة: إذا كان ExecutedByUserId لا يقبل الـ Null، استخدم Restrict.
        // إذا كان يقبل الـ Null، يمكن استخدام SetNull ليتم تعيينه تلقائياً كـ NULL عند حذف المستخدم.
        builder.HasOne(d => d.ExecutedByUser)
              .WithMany(p => p.ExecutionLogs)
              .HasForeignKey(d => d.ExecutedByUserId)
              .OnDelete(DeleteBehavior.Restrict);

        // 4. فهارس الأداء (Performance Indexes)
        builder.HasIndex(e => e.EpisodeId)
              .HasDatabaseName("IX_ExecutionLogs_EpisodeId");

        builder.HasIndex(e => e.ExecutedByUserId)
              .HasDatabaseName("IX_ExecutionLogs_ExecutedByUserId");
    }
}