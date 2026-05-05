using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public class PlatformService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IPlatformService
{
    public async Task<List<SocialMediaPlatformDto>> GetAllActiveAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();
        return await context.SocialMediaPlatforms
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new SocialMediaPlatformDto(p.SocialMediaPlatformId, p.Name, p.Icon, p.CreatedAt))
            .ToListAsync();
    }

    public async Task<Result<int>> CreateAsync(SocialMediaPlatformDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.StaffManage);
        if (!permCheck.IsSuccess) return Result<int>.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();

        var platform = new SocialMediaPlatform
        {
            Name = dto.Name,
            Icon = dto.Icon
        };

        context.SocialMediaPlatforms.Add(platform);
        await context.SaveChangesAsync();

        return Result<int>.Success(platform.SocialMediaPlatformId);
    }

    public async Task<Result> UpdateAsync(SocialMediaPlatformDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.StaffManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();

        var platform = await context.SocialMediaPlatforms.FindAsync(dto.SocialMediaPlatformId);
        if (platform == null)
            return Result.Fail("المنصة غير موجودة.");

        platform.Name = dto.Name;
        platform.Icon = dto.Icon;

        await context.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int platformId, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.StaffManage);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();

        var platform = await context.SocialMediaPlatforms.FindAsync(platformId);
        if (platform == null)
            return Result.Fail("المنصة غير موجودة.");

        platform.IsActive = false;
        await context.SaveChangesAsync();
        return Result.Success();
    }
}
