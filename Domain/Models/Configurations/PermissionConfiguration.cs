using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Domain.Models.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.HasKey(e => e.PermissionId);

        builder.Property(e => e.SystemName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(e => e.DisplayName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(e => e.Module)
               .IsRequired()
               .HasMaxLength(100);

        // ── فهرس فريد على SystemName لمنع تكرار أسماء الصلاحيات ──
        builder.HasIndex(e => e.SystemName)
               .IsUnique()
               .HasDatabaseName("UQ_Permissions_SystemName");

        builder.HasData(
            new Permission { PermissionId = 1, SystemName = "USER_MANAGE", DisplayName = "إدارة المستخدمين", Module = "المستخدمين" },
            new Permission { PermissionId = 2, SystemName = "PROGRAM_MANAGE", DisplayName = "إدارة البرامج", Module = "البرامج" },
            new Permission { PermissionId = 3, SystemName = "EPISODE_MANAGE", DisplayName = "إدارة الحلقات", Module = "الحلقات" },
            new Permission { PermissionId = 4, SystemName = "EPISODE_EXECUTE", DisplayName = "تنفيذ الحلقات", Module = "الحلقات" },
            new Permission { PermissionId = 5, SystemName = "EPISODE_PUBLISH", DisplayName = "نشر رقمي", Module = "الحلقات" },
            new Permission { PermissionId = 6, SystemName = "EPISODE_WEB_PUBLISH", DisplayName = "نشر الموقع", Module = "الحلقات" },
            new Permission { PermissionId = 7, SystemName = "EPISODE_EDIT", DisplayName = "تعديل الحلقات", Module = "الحلقات" },
            new Permission { PermissionId = 8, SystemName = "EPISODE_DELETE", DisplayName = "حذف الحلقات", Module = "الحلقات" },
            new Permission { PermissionId = 9, SystemName = "GUEST_MANAGE", DisplayName = "إدارة الضيوف", Module = "الضيوف" },
            new Permission { PermissionId = 10, SystemName = "CORR_MANAGE", DisplayName = "إدارة التنسيق الميداني", Module = "التنسيق" },
            new Permission { PermissionId = 11, SystemName = "VIEW_REPORTS", DisplayName = "عرض التقارير", Module = "التقارير" },
            new Permission { PermissionId = 12, SystemName = "EPISODE_REVERT", DisplayName = "تراجع عن تنفيذ/نشر", Module = "الحلقات" }
        );
    }
}
