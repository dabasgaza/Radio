using DataAccess.Common;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Seeding;

/// <summary>
/// المصدر الوحيد للبيانات الابتدائية في النظام.
/// يُستدعى عند كل تشغيل للتطبيق وهو idempotent تماماً — آمن للتشغيل المتكرر.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IDbContextFactory<BroadcastWorkflowDBContext> dbFactory)
    {
        await using var context = await dbFactory.CreateDbContextAsync();

        await SeedEpisodeStatusesAsync(context);
        await SeedPermissionsAsync(context);   // يجب أن يسبق SeedRolesAsync
        await SeedRolesAsync(context);
        await SeedRolePermissionsAsync(context);
        await SeedAdminUserAsync(context);
        await SeedSocialMediaPlatformsAsync(context);
        await SeedStaffRolesAsync(context);
    }

    // ═══════════════════════════════════════════
    // حالات الحلقات
    // ═══════════════════════════════════════════

    private static async Task SeedEpisodeStatusesAsync(BroadcastWorkflowDBContext context)
    {
        var existing = await context.Set<EpisodeStatus>().Select(s => s.StatusId).ToListAsync();

        var statuses = new List<EpisodeStatus>
        {
            new() { StatusId = 0, StatusName = "Planned",          DisplayName = "مخطط لها",           SortOrder = 0 },
            new() { StatusId = 1, StatusName = "Executed",         DisplayName = "تم تنفيذها",          SortOrder = 1 },
            new() { StatusId = 2, StatusName = "Published",        DisplayName = "تم نشرها",            SortOrder = 2 },
            new() { StatusId = 3, StatusName = "WebsitePublished", DisplayName = "منشورة على الموقع",   SortOrder = 3 },
            new() { StatusId = 4, StatusName = "Cancelled",        DisplayName = "ملغاة",               SortOrder = 4 },
        };

        var missing = statuses.Where(s => !existing.Contains(s.StatusId)).ToList();
        if (missing.Count > 0)
        {
            context.Set<EpisodeStatus>().AddRange(missing);
            await context.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════
    // الصلاحيات — يتزامن تلقائياً مع AppPermissions
    // ═══════════════════════════════════════════

    private static async Task SeedPermissionsAsync(BroadcastWorkflowDBContext context)
    {
        var fields = typeof(AppPermissions).GetFields(
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.FlattenHierarchy);

        var dbPermissions = await context.Set<Permission>().ToDictionaryAsync(p => p.SystemName);
        var toInsert = new List<Permission>();
        bool anyChange = false;

        foreach (var field in fields)
        {
            if (!field.IsLiteral || field.IsInitOnly || field.FieldType != typeof(string))
                continue;

            var systemName = (string)field.GetValue(null)!;
            var attr = (PermissionInfoAttribute?)Attribute.GetCustomAttribute(field, typeof(PermissionInfoAttribute));
            var displayName = attr?.DisplayName ?? systemName;
            var module = attr?.Module ?? "عام";

            if (dbPermissions.TryGetValue(systemName, out var existing))
            {
                // تحديث الاسم الظاهر أو القسم إن تغيّرا في الكود
                if (existing.DisplayName != displayName || existing.Module != module)
                {
                    existing.DisplayName = displayName;
                    existing.Module = module;
                    anyChange = true;
                }
            }
            else
            {
                toInsert.Add(new Permission { SystemName = systemName, DisplayName = displayName, Module = module });
                anyChange = true;
            }
        }

        if (toInsert.Count > 0)
            context.Set<Permission>().AddRange(toInsert);

        if (anyChange)
        {
            await context.SaveChangesAsync();

            // إسناد الصلاحيات الجديدة تلقائياً للـ Admin
            var adminRole = await context.Set<Role>().FirstOrDefaultAsync(r => r.RoleName == "Admin");
            if (adminRole != null)
            {
                var allPermIds = await context.Set<Permission>().Select(p => p.PermissionId).ToListAsync();
                var assigned = await context.Set<RolePermission>()
                    .Where(rp => rp.RoleId == adminRole.RoleId)
                    .Select(rp => rp.PermissionId)
                    .ToHashSetAsync();

                var newLinks = allPermIds
                    .Where(id => !assigned.Contains(id))
                    .Select(id => new RolePermission { RoleId = adminRole.RoleId, PermissionId = id })
                    .ToList();

                if (newLinks.Count > 0)
                {
                    context.Set<RolePermission>().AddRange(newLinks);
                    await context.SaveChangesAsync();
                }
            }
        }
    }

    // ═══════════════════════════════════════════
    // الأدوار الافتراضية
    // ═══════════════════════════════════════════

    private static async Task SeedRolesAsync(BroadcastWorkflowDBContext context)
    {
        // نتحقق بالاسم وليس بالـ ID لتجنب تعارض الـ RowVersion seed
        var existingNames = await context.Set<Role>()
            .IgnoreQueryFilters()
            .Select(r => r.RoleName)
            .ToHashSetAsync();

        var now = DateTime.UtcNow;

        var roles = new[]
        {
            new Role { RoleName = "Admin",        RoleDescription = "مسؤول النظام — صلاحيات كاملة",                    IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Role { RoleName = "Producer",     RoleDescription = "منتج البرامج — إدارة البرامج والحلقات والضيوف",   IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Role { RoleName = "WebPublisher", RoleDescription = "ناشر الموقع — نشر الحلقات على الموقع فقط",        IsActive = true, CreatedAt = now, UpdatedAt = now },
            new Role { RoleName = "Reporter",     RoleDescription = "مراسل — عرض التقارير وإدارة التغطيات",            IsActive = true, CreatedAt = now, UpdatedAt = now },
        };

        var missing = roles.Where(r => !existingNames.Contains(r.RoleName)).ToList();
        if (missing.Count > 0)
        {
            context.Set<Role>().AddRange(missing);
            await context.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════
    // صلاحيات الأدوار الافتراضية
    // ═══════════════════════════════════════════

    private static async Task SeedRolePermissionsAsync(BroadcastWorkflowDBContext context)
    {
        // نُشغّل هذا فقط إن لم يكن هناك أي ربط بعد
        if (await context.Set<RolePermission>().AnyAsync())
            return;

        var roles = await context.Set<Role>()
            .IgnoreQueryFilters()
            .ToDictionaryAsync(r => r.RoleName);

        var perms = await context.Set<Permission>()
            .ToDictionaryAsync(p => p.SystemName);

        if (roles.Count == 0 || perms.Count == 0)
            return;

        var links = new List<RolePermission>();

        void Assign(string roleName, params string[] permNames)
        {
            if (!roles.TryGetValue(roleName, out var role)) return;
            foreach (var name in permNames)
                if (perms.TryGetValue(name, out var perm))
                    links.Add(new RolePermission { RoleId = role.RoleId, PermissionId = perm.PermissionId });
        }

        // Admin: كل الصلاحيات
        if (roles.TryGetValue("Admin", out var admin))
            foreach (var perm in perms.Values)
                links.Add(new RolePermission { RoleId = admin.RoleId, PermissionId = perm.PermissionId });

        // منتج البرامج
        Assign("Producer",
            AppPermissions.ProgramManage,
            AppPermissions.EpisodeManage,
            AppPermissions.EpisodeExecute,
            AppPermissions.EpisodePublish,
            AppPermissions.EpisodeEdit,
            AppPermissions.EpisodeDelete,
            AppPermissions.EpisodeRevert,
            AppPermissions.GuestManage,
            AppPermissions.CoordinationManage,
            AppPermissions.StaffManage,
            AppPermissions.ViewReports);

        // ناشر الموقع
        Assign("WebPublisher",
            AppPermissions.EpisodeWebPublish,
            AppPermissions.ViewReports);

        // مراسل
        Assign("Reporter",
            AppPermissions.ViewReports,
            AppPermissions.CoordinationManage);

        if (links.Count > 0)
        {
            context.Set<RolePermission>().AddRange(links);
            await context.SaveChangesAsync();
        }
    }

    // ═══════════════════════════════════════════
    // مستخدم Admin الافتراضي
    // ═══════════════════════════════════════════

    private static async Task SeedAdminUserAsync(BroadcastWorkflowDBContext context)
    {
        if (await context.Set<User>().AnyAsync())
            return;

        var adminRole = await context.Set<Role>()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.RoleName == "Admin");

        if (adminRole == null)
            return;

        var now = DateTime.UtcNow;
        context.Set<User>().Add(new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            FullName = "مسؤول النظام",
            EmailAddress = "admin@broadcast.pro",
            PhoneNumber = "",
            RoleId = adminRole.RoleId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        });

        await context.SaveChangesAsync();
    }

    // ═══════════════════════════════════════════
    // منصات السوشيال ميديا
    // ═══════════════════════════════════════════

    private static async Task SeedSocialMediaPlatformsAsync(BroadcastWorkflowDBContext context)
    {
        if (await context.Set<SocialMediaPlatform>().AnyAsync())
            return;

        context.Set<SocialMediaPlatform>().AddRange(
            new SocialMediaPlatform { Name = "Facebook", Icon = "Facebook" },
            new SocialMediaPlatform { Name = "Twitter", Icon = "Twitter" },
            new SocialMediaPlatform { Name = "TikTok", Icon = "MusicNote" },
            new SocialMediaPlatform { Name = "YouTube", Icon = "Youtube" },
            new SocialMediaPlatform { Name = "Instagram", Icon = "Instagram" });
    }

    // ═══════════════════════════════════════════
    // المسميات الوظيفية
    // ═══════════════════════════════════════════

    private static async Task SeedStaffRolesAsync(BroadcastWorkflowDBContext context)
    {
        if (await context.Set<StaffRole>().AnyAsync())
            return;

        context.Set<StaffRole>().AddRange(
            new StaffRole { RoleName = "مذيع" },
            new StaffRole { RoleName = "منفذ" },
            new StaffRole { RoleName = "مهندس صوت" },
            new StaffRole { RoleName = "مخرج" },
            new StaffRole { RoleName = "مصور" });
    }
}
