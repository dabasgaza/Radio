using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Models.Configurations
{
    internal class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            // 1. المفتاح الأساسي (تم إزالة الاسم القبيح المولد آلياً)
            builder.HasKey(e => e.AuditLogId);

            // 2. إعدادات الخصائص
            builder.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(20); // ✨ تم زيادة الطول من 10 إلى 20 لدعم "SOFT_DELETED"

            builder.Property(e => e.TableName)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.RecordId)
                .IsRequired(false); // قد يكون Null أو 0 للسجلات المضافة حديثاً

            builder.Property(e => e.UserId)
                .IsRequired(false); // قد تكون العملة نظامية بدون مستخدم

            builder.Property(e => e.ChangedAt)
                .HasDefaultValueSql("GETUTCDATE()"); // القيمة الافتراضية في قاعدة البيانات كضمان إضافي

            // تخزين الـ JSON بكفاءة في SQL Server
            builder.Property(e => e.OldValues)
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.NewValues)
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.Reason)
                .HasMaxLength(500)
                .IsRequired(false);

            // 3. الفهارس (Indexes) - ✨ إضافة حاسمة للأداء!
            // جدول التدقيق بدون فهارس سيتسبب في بطء شديد بعد أسابيع قليلة من الاستخدام
            builder.HasIndex(e => e.TableName)
                .HasDatabaseName("IX_AuditLog_TableName");

            builder.HasIndex(e => e.RecordId)
                .HasDatabaseName("IX_AuditLog_RecordId");

            builder.HasIndex(e => e.ChangedAt)
                .HasDatabaseName("IX_AuditLog_ChangedAt");

            // فهرس مركب مهم جداً: البحث عن تاريخ سجل معين في جدول معين
            builder.HasIndex(e => new { e.TableName, e.RecordId })
                .HasDatabaseName("IX_AuditLog_Table_Record");
        }
    }
}
