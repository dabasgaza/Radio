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
        var user = await context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        // داخل AuthService.cs في دالة LoginAsync
        var permissions = await context.RolePermissions
            .Where(rp => rp.RoleId == user.RoleId)
            .Select(rp => rp.Permission.SystemName)
            .ToListAsync();


        user.LastLoginAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

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