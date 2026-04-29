using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllUsersAsync();
        Task<Result> CreateUserAsync(UserDto dto, string plainPassword, UserSession session);
        Task<Result> UpdateUserAsync(UserDto dto, string? newPassword, UserSession session);
        Task<Result> ToggleUserStatusAsync(int userId, bool isActive, UserSession session);
        Task<List<RoleDto>> GetRolesAsync();
        Task<List<PermissionViewModel>> GetPermissionsMatrixAsync(int roleId);
        Task<Result> UpdateRolePermissionsAsync(int roleId, List<int> selectedPermissionIds, UserSession session);
        Task<Result> DeleteUserAsync(int userId, UserSession session);
    }

    public class UserService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IUserService
    {
        #region إدارة المستخدمين

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            return await context.Users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Username = u.Username,
                    EmailAddress = u.EmailAddress,
                    PhoneNumber = u.PhoneNumber,
                    RoleId = u.RoleId,
                    RoleName = u.Role != null ? u.Role.RoleName : "N/A",
                    IsActive = u.IsActive
                })
                .ToListAsync();
        }

        public async Task<Result> CreateUserAsync(UserDto dto, string plainPassword, UserSession session)
        {
            var permCheck = session.EnsurePermission(AppPermissions.UserManage);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

            await using var context = await contextFactory.CreateDbContextAsync();

            if (await context.Users.AnyAsync(u => u.Username == dto.Username))
                return Result.Fail("اسم المستخدم موجود بالفعل في النظام.");

            var user = new User
            {
                Username = dto.Username,
                FullName = dto.FullName,
                EmailAddress = dto.EmailAddress,
                PhoneNumber = dto.PhoneNumber,
                RoleId = dto.RoleId,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword),
                IsActive = true
            };

            context.Users.Add(user);
            await context.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> UpdateUserAsync(UserDto dto, string? newPassword, UserSession session)
        {
            var permCheck = session.EnsurePermission(AppPermissions.UserManage);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

            await using var context = await contextFactory.CreateDbContextAsync();
            var dbUser = await context.Users.FindAsync(dto.UserId);

            if (dbUser == null) return Result.Fail("المستخدم غير موجود.");

            dbUser.FullName = dto.FullName;
            dbUser.EmailAddress = dto.EmailAddress;
            dbUser.PhoneNumber = dto.PhoneNumber;
            dbUser.RoleId = dto.RoleId;

            if (!string.IsNullOrWhiteSpace(newPassword))
                dbUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            try
            {
                await context.SaveChangesAsync();
                return Result.Success();
            }
            catch (DbUpdateConcurrencyException)
            {
                return Result.Fail("تم تعديل بيانات هذا المستخدم من قبل شخص آخر. يرجى التحديث والمحاولة ثانية.");
            }
        }

        public async Task<Result> ToggleUserStatusAsync(int userId, bool isActive, UserSession session)
        {
            var permCheck = session.EnsurePermission(AppPermissions.UserManage);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

            if (userId == session.UserId)
                return Result.Fail("لا يمكنك تعطيل حسابك الشخصي لأسباب أمنية.");

            await using var context = await contextFactory.CreateDbContextAsync();
            var user = await context.Users.FindAsync(userId);

            if (user == null) return Result.Fail("المستخدم غير موجود.");

            user.IsActive = isActive;
            await context.SaveChangesAsync();
            return Result.Success();
        }

        #endregion

        #region إدارة الأدوار والصلاحيات

        public async Task<List<RoleDto>> GetRolesAsync()
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            return await context.Roles
                .AsNoTracking()
                .Where(r => r.IsActive)
                .Select(r => new RoleDto
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    RoleDescription = r.RoleDescription
                })
                .ToListAsync();
        }

        public async Task<List<PermissionViewModel>> GetPermissionsMatrixAsync(int roleId)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            var allPermissions = await context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Module)
                .ToListAsync();

            var assignedIds = await context.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            return allPermissions.Select(p => new PermissionViewModel
            {
                PermissionId = p.PermissionId,
                DisplayName = p.DisplayName,
                Module = p.Module,
                IsAssigned = assignedIds.Contains(p.PermissionId)
            }).ToList();
        }

        public async Task<Result> UpdateRolePermissionsAsync(int roleId, List<int> selectedPermissionIds, UserSession session)
        {
            var permCheck = session.EnsurePermission(AppPermissions.UserManage);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

            await using var context = await contextFactory.CreateDbContextAsync();

            var existing = await context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            if (existing.Count > 0)
            {
                context.RolePermissions.RemoveRange(existing);
                await context.SaveChangesAsync();
            }

            if (selectedPermissionIds.Count > 0)
            {
                context.RolePermissions.AddRange(
                    selectedPermissionIds.Select(id => new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = id
                    }));

                await context.SaveChangesAsync();
            }

            return Result.Success();
        }
        public Task<Result> DeleteUserAsync(int userId, UserSession session)
        {
            return Task.FromResult(Result.Fail("خاصية حذف المستخدمين غير متاحة حالياً."));
        }


        #endregion
    }
}