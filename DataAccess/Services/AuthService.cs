using DataAccess.Common;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public interface IAuthService
{
    Task<Result<UserSession>> LoginAsync(string username, string password);
}

public class AuthService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IAuthService
{
    private const string DummyHash = "$2a$11$dummyHashToPreventTimingAttack1234567890";

    public async Task<Result<UserSession>> LoginAsync(string username, string password)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        // ✅ Select مباشر لجلب الحقول المطلوبة فقط بدلاً من Include ثلاثي عميق
        var userProjection = await context.Users
            .AsNoTracking()
            .Where(u => u.Username == username && u.IsActive)
            .Select(u => new
            {
                u.UserId,
                u.Username,
                u.FullName,
                u.PasswordHash,
                u.IsActive,
                RoleName = u.Role != null ? u.Role.RoleName : null,
                Permissions = u.Role != null
                    ? u.Role.RolePermissions
                        .Select(rp => rp.Permission.SystemName)
                        .ToList()
                    : new List<string>()
            })
            .FirstOrDefaultAsync();

        var hashToVerify = userProjection?.PasswordHash ?? DummyHash;

        if (userProjection is null || !BCrypt.Net.BCrypt.Verify(password, hashToVerify))
            return Result<UserSession>.Fail("اسم المستخدم أو كلمة المرور غير صحيحة.");

        if (!userProjection.IsActive)
            return Result<UserSession>.Fail("حسابك معطل. يرجى التواصل مع مسؤول النظام.");

        await using var writeContext = await contextFactory.CreateDbContextAsync();
        await writeContext.Users
            .Where(u => u.UserId == userProjection.UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastLoginAt, DateTime.UtcNow));

        return Result<UserSession>.Success(new UserSession
        {
            UserId      = userProjection.UserId,
            Username    = userProjection.Username,
            FullName    = userProjection.FullName,
            RoleName    = userProjection.RoleName ?? "Unknown",
            Permissions = userProjection.Permissions
        });
    }
}