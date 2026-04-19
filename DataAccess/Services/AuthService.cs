using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public interface IAuthService
{
    Task<UserSession?> LoginAsync(string username, string password);
}

// ✨ استخدام C# 13 Primary Constructor لتبسيط الكود
public class AuthService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IAuthService
{
    // هاش وهمي ثابت يتم التحقق منه في حال كان المستخدم غير موجود لتضليل المخترق
    private const string DummyHash = "$2a$11$dummyHashToPreventTimingAttack1234567890";

    public async Task<UserSession?> LoginAsync(string username, string password)
    {
        using var context = contextFactory.CreateDbContext();

        var user = await context.Users
            .AsNoTracking()
            .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        // ✨ حماية من Timing Attack
        // إذا كان المستخدم غير موجود، سنستخدم الـ DummyHash لنضمن تنفيذ خوارزمية BCrypt واستغراق نفس الوقت
        var hashToVerify = user?.PasswordHash ?? DummyHash;

        if (!BCrypt.Net.BCrypt.Verify(password, hashToVerify) || user == null)
            return null;

        var permissions = user.Role?.RolePermissions
            .Select(rp => rp.Permission.SystemName)
            .ToList() ?? new List<string>();

        // تحديث LastLoginAt باستخدام ExecuteUpdate (أداء فائق)
        using var writeContext = await contextFactory.CreateDbContextAsync();
        await writeContext.Users
            .Where(u => u.UserId == user.UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastLoginAt, DateTime.UtcNow));

        return new UserSession
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            RoleName = user.Role?.RoleName ?? "Unknown",
            Permissions = permissions
        };
    }
}