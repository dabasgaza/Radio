using DataAccess.Common;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Seeding;

/// <summary>
/// فئة مسؤولة عن إنشاء البيانات الأساسية عند أول تشغيل للنظام
/// تُستدعى من App.xaml.cs بعد التحقق من وجود Migration حديث
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// نقطة الدخول الرئيسية — تفحص إن كانت الجداول فارغة ثم تملأها
    /// </summary>
    public static async Task SeedAsync(IDbContextFactory<BroadcastWorkflowDBContext> dbFactory)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        // نقوم بإنشاء البيانات فقط إن لم تكن موجودة مسبقاً
        await SeedEpisodeStatusesAsync(context);
        await SeedPermissionsAsync(context);
        await SeedRolesAsync(context);
        await SeedRolePermissionsAsync(context);
        await SeedAdminUserAsync(context);
        await SeedSocialMediaPlatformsAsync(context);
        await SeedStaffRolesAsync(context);

        await context.SaveChangesAsync();
    }

    #region EpisodeStatuses

    private static async Task SeedEpisodeStatusesAsync(BroadcastWorkflowDBContext context)
    {
        var existingStatuses = await context.Set<EpisodeStatus>().Select(s => s.StatusId).ToListAsync();

        var statuses = new List<EpisodeStatus>
        {
            new() { StatusId = 0, StatusName = "Planned",   DisplayName = "مخطط لها",  SortOrder = 0 },
            new() { StatusId = 1, StatusName = "Executed",  DisplayName = "تم تنفيذها", SortOrder = 1 },
            new() { StatusId = 2, StatusName = "Published", DisplayName = "تم نشرها",  SortOrder = 2 },
            new() { StatusId = 3, StatusName = "WebsitePublished", DisplayName = "منشورة على الموقع", SortOrder = 3 },
            new() { StatusId = 4, StatusName = "Cancelled", DisplayName = "ملغاة", SortOrder = 4 },
        };

        var missingStatuses = statuses.Where(s => !existingStatuses.Contains(s.StatusId)).ToList();

        if (missingStatuses.Any())
        {
            context.Set<EpisodeStatus>().AddRange(missingStatuses);
            await context.SaveChangesAsync();
        }
    }

    #endregion

    #region Permissions

    private static async Task SeedPermissionsAsync(BroadcastWorkflowDBContext context)
    {
        if (await context.Set<Permission>().AnyAsync())
            return;

        var now = DateTime.UtcNow;

        var permissions = new List<Permission>
        {
            // المستخدمين
            new() { SystemName = AppPermissions.UserManage,      DisplayName = "إدارة المستخدمين",   Module = "المستخدمين" },

            // البرامج
            new() { SystemName = AppPermissions.ProgramManage,   DisplayName = "إدارة البرامج",     Module = "البرامج" },

            // الحلقات
            new() { SystemName = AppPermissions.EpisodeManage,     DisplayName = "إدارة الحلقات",     Module = "الحلقات" },
            new() { SystemName = AppPermissions.EpisodeExecute,    DisplayName = "تنفيذ الحلقات",     Module = "الحلقات" },
            new() { SystemName = AppPermissions.EpisodePublish,    DisplayName = "نشر الحلقات",       Module = "الحلقات" },
            new() { SystemName = AppPermissions.EpisodeWebPublish, DisplayName = "نشر على الموقع",    Module = "الحلقات" },
            new() { SystemName = AppPermissions.EpisodeEdit,       DisplayName = "تعديل الحلقات",     Module = "الحلقات" },
            new() { SystemName = AppPermissions.EpisodeDelete,     DisplayName = "حذف الحلقات",       Module = "الحلقات" },
            new() { SystemName = AppPermissions.EpisodeRevert,     DisplayName = "تراجع عن تنفيذ/نشر", Module = "الحلقات" },

            // الضيوف
            new() { SystemName = AppPermissions.GuestManage,      DisplayName = "إدارة الضيوف",      Module = "الضيوف" },

            // المراسلين
            new() { SystemName = AppPermissions.CoordinationManage, DisplayName = "إدارة المراسلين",   Module = "المراسلين" },

            // التقارير
            new() { SystemName = AppPermissions.ViewReports,      DisplayName = "عرض التقارير",      Module = "التقارير" },
        };

        context.Set<Permission>().AddRange(permissions);
        await context.SaveChangesAsync();
    }

    #endregion

    #region Roles

    private static async Task SeedRolesAsync(BroadcastWorkflowDBContext context)
    {
        if (await context.Set<Role>().AnyAsync())
            return;

        var now = DateTime.UtcNow;

        var roles = new List<Role>
        {
            new()
            {
                RoleName = "Admin",
                RoleDescription = "مدير النظام — صلاحيات كاملة",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                RowVersion = Array.Empty<byte>(),
            },
            new()
            {
                RoleName = "Producer",
                RoleDescription = "منتج البرامج — إدارة البرامج والحلقات والضيوف",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                RowVersion = Array.Empty<byte>(),
            },
            new()
            {
                RoleName = "WebPublisher",
                RoleDescription = "ناشر الموقع — نشر الحلقات على الموقع فقط",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                RowVersion = Array.Empty<byte>(),
            },
            new()
            {
                RoleName = "Reporter",
                RoleDescription = "مراسل — عرض التقارير وإدارة التغطيات",
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
                RowVersion = Array.Empty<byte>(),
            },
        };

        context.Set<Role>().AddRange(roles);
        await context.SaveChangesAsync();
    }

    #endregion

    #region RolePermissions

    private static async Task SeedRolePermissionsAsync(BroadcastWorkflowDBContext context)
    {
        if (await context.Set<RolePermission>().AnyAsync())
            return;

        // نجلب الأدوار بالاسم والأذونات بالـ SystemName
        var roles = await context.Set<Role>()
            .ToDictionaryAsync(r => r.RoleName);

        var permissions = await context.Set<Permission>()
            .ToDictionaryAsync(p => p.SystemName);

        if (roles.Count == 0 || permissions.Count == 0)
            return;

        var rolePermissions = new List<RolePermission>();

        // ===== مدير النظام: كل الصلاحيات =====
        var adminId = roles["Admin"].RoleId;
        foreach (var perm in permissions.Values)
        {
            rolePermissions.Add(new RolePermission { RoleId = adminId, PermissionId = perm.PermissionId });
        }

        // ===== منتج البرامج =====
        var producerId = roles["Producer"].RoleId;
        var producerPerms = new[]
        {
            AppPermissions.ProgramManage,
            AppPermissions.EpisodeManage,
            AppPermissions.EpisodeExecute,
            AppPermissions.EpisodePublish,
            AppPermissions.EpisodeEdit,
            AppPermissions.EpisodeDelete,
            AppPermissions.GuestManage,
            AppPermissions.CoordinationManage,
            AppPermissions.ViewReports,
        };
        foreach (var permName in producerPerms)
        {
            if (permissions.TryGetValue(permName, out var perm))
                rolePermissions.Add(new RolePermission { RoleId = producerId, PermissionId = perm.PermissionId });
        }

        // ===== ناشر الموقع =====
        var webPubId = roles["WebPublisher"].RoleId;
        if (permissions.TryGetValue(AppPermissions.EpisodeWebPublish, out var webPubPerm))
        {
            rolePermissions.Add(new RolePermission { RoleId = webPubId, PermissionId = webPubPerm.PermissionId });
        }

        // ===== مراسل =====
        var reporterId = roles["Reporter"].RoleId;
        var reporterPerms = new[]
        {
            AppPermissions.ViewReports,
            AppPermissions.CoordinationManage,
        };
        foreach (var permName in reporterPerms)
        {
            if (permissions.TryGetValue(permName, out var perm))
                rolePermissions.Add(new RolePermission { RoleId = reporterId, PermissionId = perm.PermissionId });
        }

        context.Set<RolePermission>().AddRange(rolePermissions);
        await context.SaveChangesAsync();
    }

    #endregion

    #region AdminUser

    private static async Task SeedAdminUserAsync(BroadcastWorkflowDBContext context)
    {
        // لا ننشئ المستخدم إن كان موجود مسبقاً
        if (await context.Set<User>().AnyAsync())
            return;

        var adminRole = await context.Set<Role>()
            .FirstOrDefaultAsync(r => r.RoleName == "Admin");

        if (adminRole == null)
            return;

        var now = DateTime.UtcNow;

        // ⚠️ استبدل هذا الهاش بكلمة المرور الفعلية المشفرة
        // هذا الهاش يمثل كلمة المرور: Admin@123
        // استخدم نفس طريقة التشفير في AuthService لتوليد الهاش
        var defaultPasswordHash = HashPassword("Admin@123");

        var adminUser = new User
        {
            Username = "admin",
            PasswordHash = defaultPasswordHash,
            FullName = "مدير النظام",
            EmailAddress = "admin@broadcast.pro",
            PhoneNumber = "",
            RoleId = adminRole.RoleId,
            LastLoginAt = null,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = Array.Empty<byte>(),
        };

        context.Set<User>().Add(adminUser);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// طريقة التشفير — يجب أن تتطابق مع الطريقة المستخدمة في AuthService
    /// إن كنت تستخدم BCrypt أو أي مكتبة أخرى، عدّل هذه الدالة
    /// </summary>
    private static string HashPassword(string plainPassword)
    {
        // ⚠️ عدّل هذا السطر حسب طريقة التشفير في مشروعك
        // مثال BCrypt:
        // return BCrypt.Net.BCrypt.HashPassword(plainPassword);

        // كحل مؤقت، نخزن كلمة المرور مشفرة بـ SHA256 (ليست الآمنة للإنتاج)
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(plainPassword));
        return Convert.ToBase64String(bytes);
    }

    #endregion

    #region SocialMediaPlatforms

    private static async Task SeedSocialMediaPlatformsAsync(BroadcastWorkflowDBContext context)
    {
        if (await context.Set<SocialMediaPlatform>().AnyAsync())
            return;

        var platforms = new List<SocialMediaPlatform>
        {
            new() { Name = "Facebook",  Icon = "Facebook" },
            new() { Name = "Twitter",   Icon = "Twitter" },
            new() { Name = "TikTok",    Icon = "MusicNote" }, 
            new() { Name = "YouTube",   Icon = "Youtube" },
            new() { Name = "Instagram", Icon = "Instagram" },
        };

        context.Set<SocialMediaPlatform>().AddRange(platforms);
    }

    #endregion

    #region StaffRoles

    private static async Task SeedStaffRolesAsync(BroadcastWorkflowDBContext context)
    {
        if (await context.Set<StaffRole>().AnyAsync())
            return;

        var roles = new List<StaffRole>
        {
            new() { RoleName = "مذيع" },
            new() { RoleName = "منفذ" },
            new() { RoleName = "مهندس صوت" },
            new() { RoleName = "مخرج" },
            new() { RoleName = "مصور" },
        };

        context.Set<StaffRole>().AddRange(roles);
    }

    #endregion
}
