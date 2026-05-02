using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Models.Configurations;

public class ProgramConfiguration : IEntityTypeConfiguration<Program>
{
    public void Configure(EntityTypeBuilder<Program> builder)
    {
        // 1. المفتاح الأساسي (إزالة الاسم القبيح المولد آلياً)
        builder.HasKey(e => e.ProgramId);

        // 2. الفهارس (Indexes)
        // ✨ فهرس فريد لضمان عدم تكرار أسماء البرامج (تم إعطاء اسم نظيف بدل الاسم المولد آلياً)
        builder.HasIndex(e => e.ProgramName, "UQ_Programs_ProgramName")
              .IsUnique();

        // 3. إعدادات الخصائص (Properties)
        builder.Property(e => e.ProgramName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(e => e.ProgramDescription)
               .HasMaxLength(1000);

        builder.Property(e => e.Category)
               .HasMaxLength(100);

        // إعدادات BaseEntity
        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.UpdatedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.IsActive)
               .HasDefaultValue(true);

        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // 4. العلاقات (Relationships)



        // ملاحظة: علاقة البرنامج مع الحلقات (Episodes) يتم تعريفها في EpisodeConfiguration

        // 5. Seed Data
        builder.HasData(
            new Program { ProgramId = 1, ProgramName = "نشرة الأخبار", ProgramDescription = "النشرة الإخبارية اليومية", Category = "أخبار", CreatedByUserId = 1, CreatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc) },
            new Program { ProgramId = 2, ProgramName = "صباح الخير", ProgramDescription = "برنامج صباحي منوع", Category = "منوعات", CreatedByUserId = 1, CreatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc) },
            new Program { ProgramId = 3, ProgramName = "حديث الرياضة", ProgramDescription = "تحليل ونقاش رياضي", Category = "رياضة", CreatedByUserId = 1, CreatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc) },
            new Program { ProgramId = 4, ProgramName = "نافذة ثقافية", ProgramDescription = "برنامج ثقافي أدبي", Category = "ثقافة", CreatedByUserId = 1, CreatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}