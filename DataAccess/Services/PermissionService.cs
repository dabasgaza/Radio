using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

/// <summary>
/// خدمة قراءة الصلاحيات فقط.
/// الصلاحيات تُعرَّف في AppPermissions وتُزامَن تلقائياً مع DB عبر DbSeeder — لا يمكن إنشاؤها أو حذفها يدوياً.
/// </summary>
public interface IPermissionService
{
    Task<Result<List<PermissionDto>>> GetAllPermissionsAsync();
    Task<Result<PermissionDto>> GetPermissionByIdAsync(int id);
}

public class PermissionService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IPermissionService
{
    public async Task<Result<List<PermissionDto>>> GetAllPermissionsAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var permissions = await context.Permissions
            .AsNoTracking()
            .OrderBy(p => p.Module)
            .ThenBy(p => p.DisplayName)
            .Select(p => new PermissionDto(p.PermissionId, p.SystemName, p.DisplayName, p.Module))
            .ToListAsync();

        return Result<List<PermissionDto>>.Success(permissions);
    }

    public async Task<Result<PermissionDto>> GetPermissionByIdAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        var p = await context.Permissions
            .AsNoTracking()
            .Where(x => x.PermissionId == id)
            .Select(x => new PermissionDto(x.PermissionId, x.SystemName, x.DisplayName, x.Module))
            .FirstOrDefaultAsync();

        if (p is null)
            return Result<PermissionDto>.Fail("الصلاحية غير موجودة.");

        return Result<PermissionDto>.Success(p);
    }
}
