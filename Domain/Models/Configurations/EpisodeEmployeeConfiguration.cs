using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Models.Configurations;

public class EpisodeEmployeeConfiguration : IEntityTypeConfiguration<EpisodeEmployee>
{
    public void Configure(EntityTypeBuilder<EpisodeEmployee> builder)
    {
        // 1. المفتاح الأساسي
        builder.HasKey(e => e.EpisodeEmployeeId);

        // 2. إعدادات الخصائص
        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.IsActive)
               .HasDefaultValue(true);

        builder.Property(e => e.RowVersion)
               .IsRowVersion()
               .IsConcurrencyToken();

        // 3. العلاقات

        // علاقة الحلقة — Cascade: حذف الربط عند حذف الحلقة
        builder.HasOne(e => e.Episode)
              .WithMany(ep => ep.EpisodeEmployees)
              .HasForeignKey(e => e.EpisodeId)
              .OnDelete(DeleteBehavior.Cascade);

        // علاقة الموظف — Restrict: لا يحذف الموظف إذا كان مربوطاً بحلقة
        builder.HasOne(e => e.Employee)
              .WithMany(emp => emp.EpisodeEmployees)
              .HasForeignKey(e => e.EmployeeId)
              .OnDelete(DeleteBehavior.Restrict);

        // 4. الفهارس (Indexes)
        // فهرس فريد مركب لمنع إضافة نفس الموظف لنفس الحلقة مرتين وتأمين الاستعلامات
        builder.HasIndex(e => new { e.EpisodeId, e.EmployeeId }, "UQ_EpisodeEmployees")
               .IsUnique();

        // فهرس لتسريع الاستعلام والبحث حسب الموظف
        builder.HasIndex(e => e.EmployeeId)
               .HasDatabaseName("IX_EpisodeEmployees_EmployeeId");

        // 5. فلتر الحذف المنطقي
        builder.HasQueryFilter(ee => ee.IsActive);
    }
}