using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IDbContextFactory<BroadcastWorkflowDBContext> _contextFactory;

        public PermissionService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<Result<List<PermissionDto>>> GetAllPermissionsAsync()
        {
            using var context = _contextFactory.CreateDbContext();
            var permissions = await context.Permissions
                .Select(p => new PermissionDto(p.PermissionId, p.SystemName, p.DisplayName, p.Module))
                .ToListAsync();

            return Result<List<PermissionDto>>.Success(permissions);
        }

        public async Task<Result<PermissionDto>> GetPermissionByIdAsync(int id)
        {
            using var context = _contextFactory.CreateDbContext();
            var p = await context.Permissions.FindAsync(id);
            if (p == null) return Result<PermissionDto>.Fail("الصلاحية غير موجودة");

            return Result<PermissionDto>.Success(new PermissionDto(p.PermissionId, p.SystemName, p.DisplayName, p.Module));
        }

        public async Task<Result<int>> CreatePermissionAsync(PermissionUpsertDto dto)
        {
            using var context = _contextFactory.CreateDbContext();
            
            if (await context.Permissions.AnyAsync(p => p.SystemName == dto.SystemName))
                return Result<int>.Fail("الاسم البرمجي للصلاحية موجود مسبقاً");

            var permission = new Permission
            {
                SystemName = dto.SystemName,
                DisplayName = dto.DisplayName,
                Module = dto.Module
            };

            context.Permissions.Add(permission);
            await context.SaveChangesAsync();

            return Result<int>.Success(permission.PermissionId);
        }

        public async Task<Result> UpdatePermissionAsync(int id, PermissionUpsertDto dto)
        {
            using var context = _contextFactory.CreateDbContext();
            var p = await context.Permissions.FindAsync(id);
            if (p == null) return Result.Fail("الصلاحية غير موجودة");

            if (await context.Permissions.AnyAsync(x => x.SystemName == dto.SystemName && x.PermissionId != id))
                return Result.Fail("الاسم البرمجي للصلاحية موجود مسبقاً");

            p.SystemName = dto.SystemName;
            p.DisplayName = dto.DisplayName;
            p.Module = dto.Module;

            await context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> DeletePermissionAsync(int id)
        {
            using var context = _contextFactory.CreateDbContext();
            var p = await context.Permissions.FindAsync(id);
            if (p == null) return Result.Fail("الصلاحية غير موجودة");

            if (await context.RolePermissions.AnyAsync(rp => rp.PermissionId == id))
                return Result.Fail("لا يمكن حذف الصلاحية لأنها مرتبطة بأدوار حالية. قم بإلغاء الربط أولاً.");

            context.Permissions.Remove(p);
            await context.SaveChangesAsync();
            return Result.Success();
        }
    }
}
