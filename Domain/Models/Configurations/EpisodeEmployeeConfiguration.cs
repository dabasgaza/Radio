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

        // 4. فلتر الحذف المنطقي
        builder.HasQueryFilter(ee => ee.IsActive);
    }
}