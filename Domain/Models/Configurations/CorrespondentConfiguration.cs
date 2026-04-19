using Domain.Models;
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

        // علاقة "أنشأ بواسطة"
        builder.HasOne(d => d.CreatedByUser)
            .WithMany(p => p.CorrespondentCreatedByUsers)
            .HasForeignKey(d => d.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict); // ✨ تغيير حاسم لمنع Multiple Cascade Paths

        // علاقة "عدّل بواسطة"
        builder.HasOne(d => d.UpdatedByUser)
            .WithMany(p => p.CorrespondentUpdatedByUsers)
            .HasForeignKey(d => d.UpdatedByUserId)
            .OnDelete(DeleteBehavior.Restrict); // ✨ تغيير حاسم لمنع Multiple Cascade Paths

        // ملاحظة: لا نحتاج لكتابة HasQueryFilter هنا لأننا أضفناه ديناميكياً 
        // في DbContext لكل الكيانات التي ترث من BaseEntity!
    }
}