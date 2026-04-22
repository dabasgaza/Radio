using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    public interface IUserService
    {
        Task<List<UserDto>> GetAllUsersAsync();
        Task CreateUserAsync(UserDto dto, string plainPassword, UserSession session);
        Task UpdateUserAsync(UserDto dto, string? newPassword, UserSession session);
        Task ToggleUserStatusAsync(int userId, bool isActive, UserSession session);
        Task<List<RoleDto>> GetRolesAsync();
        Task<List<PermissionViewModel>> GetPermissionsMatrixAsync(int roleId);
        Task UpdateRolePermissionsAsync(int roleId, List<int> selectedPermissionIds, UserSession session);
        Task DeleteUserAsync(int userId, UserSession session);
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

        public async Task CreateUserAsync(UserDto dto, string plainPassword, UserSession session)
        {
            session.EnsurePermission(AppPermissions.UserManage);

            await using var context = await contextFactory.CreateDbContextAsync();

            if (await context.Users.AnyAsync(u => u.Username == dto.Username))
                throw new InvalidOperationException("اسم المستخدم موجود بالفعل في النظام.");

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
        }

        public async Task UpdateUserAsync(UserDto dto, string? newPassword, UserSession session)
        {
            session.EnsurePermission(AppPermissions.UserManage);

            await using var context = await contextFactory.CreateDbContextAsync();
            var dbUser = await context.Users.FindAsync(dto.UserId)
                ?? throw new KeyNotFoundException("المستخدم غير موجود.");

            dbUser.FullName = dto.FullName;
            dbUser.EmailAddress = dto.EmailAddress;
            dbUser.PhoneNumber = dto.PhoneNumber;
            dbUser.RoleId = dto.RoleId;

            if (!string.IsNullOrWhiteSpace(newPassword))
                dbUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new InvalidOperationException("تم تعديل بيانات هذا المستخدم من قبل شخص آخر. يرجى التحديث والمحاولة ثانية.");
            }
        }

        public async Task ToggleUserStatusAsync(int userId, bool isActive, UserSession session)
        {
            session.EnsurePermission(AppPermissions.UserManage);

            if (userId == session.UserId)
                throw new InvalidOperationException("لا يمكنك تعطيل حسابك الشخصي لأسباب أمنية.");

            await using var context = await contextFactory.CreateDbContextAsync();
            var user = await context.Users.FindAsync(userId)
                ?? throw new KeyNotFoundException("المستخدم غير موجود.");

            user.IsActive = isActive;
            await context.SaveChangesAsync();
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

        public async Task UpdateRolePermissionsAsync(int roleId, List<int> selectedPermissionIds, UserSession session)
        {
            session.EnsurePermission(AppPermissions.UserManage);

            await using var context = await contextFactory.CreateDbContextAsync();

            // ✅ الخطوة 1: حذف الصلاحيات الحالية (SaveChanges منفصل)
            var existing = await context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .ToListAsync();

            if (existing.Count > 0)
            {
                context.RolePermissions.RemoveRange(existing);
                await context.SaveChangesAsync();
            }

            // ✅ الخطوة 2: إضافة الصلاحيات الجديدة (SaveChanges منفصل)
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
        }
        public Task DeleteUserAsync(int userId, UserSession session)
        {
            throw new NotImplementedException();
        }


        #endregion
    }
}