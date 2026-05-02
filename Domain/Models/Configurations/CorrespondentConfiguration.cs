using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Models.Configurations;

public class CorrespondentConfiguration : IEntityTypeConfiguration<Correspondent>
{
    public void Configure(EntityTypeBuilder<Correspondent> builder)
    {
        // 1. المفتاح الأساسي (تم إزالة الاسم القبيح المولد آلياً)
        builder.HasKey(e => e.CorrespondentId);

        // 2. إعدادات الخصائص (Properties)
        builder.Property(e => e.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(e => e.AssignedLocations)
            .HasMaxLength(500);

        // إعدادات BaseEntity (التواريخ يتم إدارتها آلياً عبر Interceptor، لكن نضع القيم الافتراضية لقاعدة البيانات)
        builder.Property(e => e.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.UpdatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.IsActive)
            .HasDefaultValue(true);

        // إعداد الـ RowVersion للـ Optimistic Concurrency
        builder.Property(e => e.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // 3. العلاقات (Relationships)



        // ملاحظة: لا نحتاج لكتابة HasQueryFilter هنا لأننا أضفناه ديناميكياً 
        // في DbContext لكل الكيانات التي ترث من BaseEntity!

        // 4. Seed Data
        builder.HasData(
            new Correspondent { CorrespondentId = 1, FullName = "محمد الحربي", PhoneNumber = "0550000001", AssignedLocations = "الرياض", CreatedByUserId = 1, CreatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc) },
            new Correspondent { CorrespondentId = 2, FullName = "فهد المطيري", PhoneNumber = "0550000002", AssignedLocations = "جدة", CreatedByUserId = 1, CreatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc) },
            new Correspondent { CorrespondentId = 3, FullName = "عبدالله العنزي", PhoneNumber = "0550000003", AssignedLocations = "الدمام", CreatedByUserId = 1, CreatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2026, 4, 28, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}