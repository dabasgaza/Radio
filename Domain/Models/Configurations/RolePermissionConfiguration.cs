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

        // 3. Seed Data
        builder.HasData(
            // Admin - جميع الصلاحيات (1-11)
            new RolePermission { RoleId = 1, PermissionId = 1 },
            new RolePermission { RoleId = 1, PermissionId = 2 },
            new RolePermission { RoleId = 1, PermissionId = 3 },
            new RolePermission { RoleId = 1, PermissionId = 4 },
            new RolePermission { RoleId = 1, PermissionId = 5 },
            new RolePermission { RoleId = 1, PermissionId = 6 },
            new RolePermission { RoleId = 1, PermissionId = 7 },
            new RolePermission { RoleId = 1, PermissionId = 8 },
            new RolePermission { RoleId = 1, PermissionId = 9 },
            new RolePermission { RoleId = 1, PermissionId = 10 },
            new RolePermission { RoleId = 1, PermissionId = 11 },
            new RolePermission { RoleId = 1, PermissionId = 12 },

            // ProgramMgr - برامج + حلقات + ضيوف + تقارير
            new RolePermission { RoleId = 2, PermissionId = 2 },
            new RolePermission { RoleId = 2, PermissionId = 3 },
            new RolePermission { RoleId = 2, PermissionId = 4 },
            new RolePermission { RoleId = 2, PermissionId = 5 },
            new RolePermission { RoleId = 2, PermissionId = 7 },
            new RolePermission { RoleId = 2, PermissionId = 9 },
            new RolePermission { RoleId = 2, PermissionId = 11 },

            // Director - تنفيذ الحلقات + تقارير
            new RolePermission { RoleId = 3, PermissionId = 4 },
            new RolePermission { RoleId = 3, PermissionId = 11 },

            // WebPublisher - نشر الموقع + تقارير
            new RolePermission { RoleId = 4, PermissionId = 6 },
            new RolePermission { RoleId = 4, PermissionId = 11 }
        );
    }
}