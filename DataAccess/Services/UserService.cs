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
        Task<Result> CreateRoleAsync(RoleDto dto, UserSession session);
        Task<Result> UpdateRoleAsync(RoleDto dto, UserSession session);
        Task<Result> DeleteRoleAsync(int roleId, UserSession session);
        Task<List<PermissionViewModel>> GetPermissionsMatrixAsync(int roleId);
        Task<Result> UpdateRolePermissionsAsync(int roleId, List<int> selectedPermissionIds, UserSession session);
        Task<Result> DeleteUserAsync(int userId, UserSession session);
    }

    public class UserService(
        IDbContextFactory<BroadcastWorkflowDBContext> contextFactory,
        CurrentSessionProvider sessionProvider) : IUserService
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

            try
            {
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
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to create User: {Username}", dto.Username);
                return Result.Fail("حدث خطأ في قاعدة البيانات أثناء إضافة المستخدم. يرجى المحاولة لاحقاً.");
            }
        }

        public async Task<Result> UpdateUserAsync(UserDto dto, string? newPassword, UserSession session)
        {
            var permCheck = session.EnsurePermission(AppPermissions.UserManage);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

            try
            {
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
                catch (DbUpdateConcurrencyException ex)
                {
                    Serilog.Log.Error(ex, "Concurrency error updating User: {UserId}", dto.UserId);
                    return Result.Fail("تم تعديل بيانات هذا المستخدم من قبل شخص آخر. يرجى التحديث والمحاولة ثانية.");
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to update User: {UserId}, {Username}", dto.UserId, dto.Username);
                return Result.Fail("حدث خطأ في قاعدة البيانات أثناء تعديل بيانات المستخدم. يرجى المحاولة لاحقاً.");
            }
        }

        public async Task<Result> ToggleUserStatusAsync(int userId, bool isActive, UserSession session)
        {
            var permCheck = session.EnsurePermission(AppPermissions.UserManage);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

            if (userId == session.UserId)
                return Result.Fail("لا يمكنك تعطيل حسابك الشخصي لأسباب أمنية.");

            try
            {
                await using var context = await contextFactory.CreateDbContextAsync();
                var user = await context.Users.FindAsync(userId);

                if (user == null) return Result.Fail("المستخدم غير موجود.");

                user.IsActive = isActive;
                await context.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to toggle User status: {UserId} to {IsActive}", userId, isActive);
                return Result.Fail("حدث خطأ في قاعدة البيانات أثناء تغيير حالة المستخدم.");
            }
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

        public async Task<Result> CreateRoleAsync(RoleDto dto, UserSession session)
        {
            var permCheck = session.EnsurePermission(AppPermissions.UserManage);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

            try
            {
                await using var context = await contextFactory.CreateDbContextAsync();

                if (await context.Roles.AnyAsync(r => r.RoleName == dto.RoleName))
                    return Result.Fail("اسم الدور موجود مسبقاً");

                var role = new Role
                {
                    RoleName = dto.RoleName,
                    RoleDescription = dto.RoleDescription,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                context.Roles.Add(role);
                await context.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to create Role: {RoleName}", dto.RoleName);
                return Result.Fail("حدث خطأ في قاعدة البيانات أثناء إضافة الدور. يرجى المحاولة لاحقاً.");
            }
        }

        public async Task<Result> UpdateRoleAsync(RoleDto dto, UserSession session)
        {
            var permCheck = session.EnsurePermission(AppPermissions.UserManage);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

            try
            {
                await using var context = await contextFactory.CreateDbContextAsync();
                var role = await context.Roles.FindAsync(dto.RoleId);
                if (role == null) return Result.Fail("الدور غير موجود");

                if (await context.Roles.AnyAsync(r => r.RoleName == dto.RoleName && r.RoleId != dto.RoleId))
                    return Result.Fail("اسم الدور موجود مسبقاً");

                role.RoleName = dto.RoleName;
                role.RoleDescription = dto.RoleDescription;
                role.UpdatedAt = DateTime.Now;

                await context.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to update Role: {RoleId}, {RoleName}", dto.RoleId, dto.RoleName);
                return Result.Fail("حدث خطأ في قاعدة البيانات أثناء تعديل الدور. يرجى المحاولة لاحقاً.");
            }
        }

        public async Task<Result> DeleteRoleAsync(int roleId, UserSession session)
        {
            var permCheck = session.EnsurePermission(AppPermissions.UserManage);
            if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

            try
            {
                await using var context = await contextFactory.CreateDbContextAsync();
                // ✨ استخدام IgnoreQueryFilters لإيجاد الدور حتى لو كان محذوفاً ناعمياً
                var role = await context.Roles
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(r => r.RoleId == roleId);
                if (role == null) return Result.Fail("الدور غير موجود");
                if (!role.IsActive) return Result.Success(); // محذوف ناعمياً بالفعل

                if (await context.Users.AnyAsync(u => u.RoleId == roleId))
                    return Result.Fail("لا يمكن حذف الدور لأنه مرتبط بمستخدمين حاليين");

                // ✨ حذف ناعم للدور بدلاً من الحذف الفعلي — للحفاظ على سجل التدقيق
                role.IsActive = false;

                // ✨ حذف ناعم لصلاحيات الدور المرتبطة
                // RolePermission لا يرث BaseEntity لذا نستخدم ExecuteDeleteAsync
                await context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .ExecuteDeleteAsync();

                await context.SaveChangesAsync();
                return Result.Success();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to delete Role: {RoleId}", roleId);
                return Result.Fail("حدث خطأ في قاعدة البيانات أثناء حذف الدور.");
            }
        }

        public async Task<List<PermissionViewModel>> GetPermissionsMatrixAsync(int roleId)
        {
            await using var context = await contextFactory.CreateDbContextAsync();

            // ✅ استعلام واحد يجلب الصلاحيات مع IsAssigned بدلاً من استعلامين منفصلين
            return await context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Module)
                .Select(p => new PermissionViewModel
                {
                    PermissionId = p.PermissionId,
                    DisplayName = p.DisplayName,
                    Module = p.Module,
                    IsAssigned = context.RolePermissions
                        .Any(rp => rp.RoleId == roleId && rp.PermissionId == p.PermissionId)
                })
                .ToListAsync();
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

            // تحديث الصلاحيات فورياً للمستخدم الحالي في الجلسة النشطة
            await sessionProvider.RefreshPermissionsAsync();

            return Result.Success();
        }
        public Task<Result> DeleteUserAsync(int userId, UserSession session)
        {
            return Task.FromResult(Result.Fail("خاصية حذف المستخدمين غير متاحة حالياً."));
        }


        #endregion
    }
}