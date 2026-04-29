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

        var user = await context.Users
            .AsNoTracking()
            .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        var hashToVerify = user?.PasswordHash ?? DummyHash;

        if (user is null || !BCrypt.Net.BCrypt.Verify(password, hashToVerify))
            return Result<UserSession>.Fail("اسم المستخدم أو كلمة المرور غير صحيحة.");

        if (!user.IsActive)
            return Result<UserSession>.Fail("حسابك معطل. يرجى التواصل مع مسؤول النظام.");

        var permissions = user.Role?.RolePermissions
            .Select(rp => rp.Permission.SystemName)
            .ToList() ?? [];

        await using var writeContext = await contextFactory.CreateDbContextAsync();
        await writeContext.Users
            .Where(u => u.UserId == user.UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.LastLoginAt, DateTime.UtcNow));

        return Result<UserSession>.Success(new UserSession
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            RoleName = user.Role?.RoleName ?? "Unknown",
            Permissions = permissions
        });
    }
}