using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Models.Configurations;

public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        // 1. المفتاح الأساسي المركب (Composite Key)
        builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

        // 2. العلاقات (Relationships)

        // علاقة الدور الوظيفي
        // ✨Cascade هو الخيار الصحيح هنا: إذا حُذف الدور، لا داعي لبقاء سجلات صلاحياته
        builder.HasOne(rp => rp.Role)
              .WithMany(r => r.RolePermissions)
              .HasForeignKey(rp => rp.RoleId)
              .OnDelete(DeleteBehavior.Cascade);

        // علاقة الصلاحية
        // ✨Cascade هنا أيضاً: إذا حُذفت صلاحية من النظام، تُحذف من جميع الأدوار
        builder.HasOne(rp => rp.Permission)
              .WithMany(p => p.RolePermissions)
              .HasForeignKey(rp => rp.PermissionId)
              .OnDelete(DeleteBehavior.Cascade);
    }
}