using Domain.Models;
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

        // علاقة "أنشأ بواسطة"
        builder.HasOne(d => d.CreatedByUser)
              .WithMany(p => p.ProgramCreatedByUsers)
              .HasForeignKey(d => d.CreatedByUserId)
              .OnDelete(DeleteBehavior.Restrict); // ✨ حاسم لمنع Multiple Cascade Paths

        // علاقة "عدّل بواسطة"
        builder.HasOne(d => d.UpdatedByUser)
              .WithMany(p => p.ProgramUpdatedByUsers)
              .HasForeignKey(d => d.UpdatedByUserId)
              .OnDelete(DeleteBehavior.Restrict); // ✨ حاسم لمنع Multiple Cascade Paths

        // ملاحظة: علاقة البرنامج مع الحلقات (Episodes) يتم تعريفها في EpisodeConfiguration
    }
}