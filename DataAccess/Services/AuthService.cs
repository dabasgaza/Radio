using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace BroadcastWorkflow.Services;

public interface IAuthService
{
    Task<UserSession?> LoginAsync(string username, string password);
}

public class AuthService : IAuthService
{
    private readonly IDbContextFactory<BroadcastWorkflowDBContext> _contextFactory;
    public AuthService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) => _contextFactory = contextFactory;

    public async Task<UserSession?> LoginAsync(string username, string password)
    {
        using var context = await _contextFactory.CreateDbContextAsync();

        // ✅ تحسين: AsNoTracking لأن LoginAsync قراءة + تحديث واحد فقط
        // ✅ تحسين: جلب الصلاحيات في نفس الاستعلام بدلاً من رحلتين لقاعدة البيانات
        var user = await context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.Role.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        // ✅ تحسين: التحقق من وجود المستخدم أولاً قبل أي عملية BCrypt
        // السبب: BCrypt.HashPassword عملية مكلفة جداً (~100ms) — لا تُنفَّذ مجاناً
        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        var permissions = user.Role.RolePermissions
            .Select(rp => rp.Permission.SystemName)
            .ToList();

        // تحديث LastLoginAt يحتاج Tracked context منفصل
        using var writeContext = await _contextFactory.CreateDbContextAsync();
        await writeContext.Users
            .Where(u => u.UserId == user.UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastLoginAt, DateTime.UtcNow));

        return new UserSession
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            RoleName = user.Role.RoleName,
            Permissions = permissions
        };
    }
}