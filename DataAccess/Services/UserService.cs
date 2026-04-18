using BroadcastWorkflow.Services;
using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();
        Task CreateUserAsync(User user, string plainPassword, UserSession session);
        Task UpdateUserAsync(User user, string? newPassword, UserSession session);
        Task ToggleUserStatusAsync(int userId, bool isActive, UserSession session);
        Task<List<Role>> GetRolesAsync();
        Task<List<PermissionViewModel>> GetPermissionsMatrixAsync(int roleId);
        Task UpdateRolePermissionsAsync(int roleId, List<int> selectedPermissionIds, UserSession session);

    }

    public class UserService : IUserService
    {
        private readonly IDbContextFactory<BroadcastWorkflowDBContext> _contextFactory;

        public UserService(IDbContextFactory<BroadcastWorkflowDBContext> factory)
        {
            _contextFactory = factory;
        }

        #region إدارة المستخدمين

        public async Task<List<User>> GetAllUsersAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Users
                .Include(u => u.Role)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task CreateUserAsync(User user, string plainPassword, UserSession session)
        {
            SecurityHelper.EnsurePermission(session, AppPermissions.UserManage);

            using var context = await _contextFactory.CreateDbContextAsync();

            // التحقق من تكرار اسم المستخدم
            if (await context.Users.AnyAsync(u => u.Username == user.Username))
                throw new Exception("اسم المستخدم موجود بالفعل في النظام.");

            // تشفير كلمة المرور
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
            user.CreatedAt = DateTime.UtcNow;
            user.CreatedByUserId = session.UserId;
            user.IsActive = true;

            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        public async Task UpdateUserAsync(User user, string? newPassword, UserSession session)
        {
            SecurityHelper.EnsurePermission(session, AppPermissions.UserManage);

            using var context = await _contextFactory.CreateDbContextAsync();
            var dbUser = await context.Users.FindAsync(user.UserId);

            if (dbUser == null) throw new Exception("المستخدم غير موجود.");

            // تحديث البيانات الأساسية
            dbUser.FullName = user.FullName;
            dbUser.EmailAddress = user.EmailAddress;
            dbUser.PhoneNumber = user.PhoneNumber;
            dbUser.RoleId = user.RoleId;
            dbUser.UpdatedAt = DateTime.UtcNow;
            dbUser.UpdatedByUserId = session.UserId;

            // تحديث كلمة المرور فقط إذا تم إدخال قيمة جديدة
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                dbUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            }

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new Exception("تم تعديل بيانات هذا المستخدم من قبل شخص آخر. يرجى التحديث والمحاولة ثانية.");
            }
        }

        public async Task ToggleUserStatusAsync(int userId, bool isActive, UserSession session)
        {
            SecurityHelper.EnsurePermission(session, AppPermissions.UserManage);

            // منع المستخدم من تعطيل حسابه الشخصي
            if (userId == session.UserId)
                throw new Exception("لا يمكنك تعطيل حسابك الشخصي لأسباب أمنية.");

            using var context = await _contextFactory.CreateDbContextAsync();
            var user = await context.Users.FindAsync(userId);

            if (user != null)
            {
                user.IsActive = isActive;
                user.UpdatedAt = DateTime.UtcNow;
                user.UpdatedByUserId = session.UserId;
                await context.SaveChangesAsync();
            }
        }

        #endregion

        #region إدارة الأدوار والصلاحيات

        public async Task<List<Role>> GetRolesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Roles
                .AsNoTracking()
                .Where(r => r.IsActive)
                .ToListAsync();
        }

        public async Task<List<PermissionViewModel>> GetPermissionsMatrixAsync(int roleId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            // جلب كافة الصلاحيات المتاحة في النظام
            var allPermissions = await context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Module)
                .ToListAsync();

            // جلب الصلاحيات المرتبطة بهذا الدور حالياً
            var assignedIds = await context.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            // التحويل إلى ViewModel للربط بالواجهة
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
            SecurityHelper.EnsurePermission(session, AppPermissions.UserManage);

            using var context = await _contextFactory.CreateDbContextAsync();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // 1. حذف الارتباطات القديمة
                var oldPermissions = await context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync();

                //context.RolePermissions.RemoveRange(oldPermissions);
                //await context.SaveChangesAsync();

                // ✅ تحسين: ExecuteDeleteAsync بدلاً من ToList + RemoveRange
                // السبب: رحلة واحدة لقاعدة البيانات (DELETE WHERE) بدلاً من SELECT ثم DELETE
                await context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .ExecuteDeleteAsync();


                // 2. إضافة الارتباطات الجديدة
                if (selectedPermissionIds != null && selectedPermissionIds.Any())
                {
                    var newMappings = selectedPermissionIds.Select(pId => new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = pId
                    });

                    await context.RolePermissions.AddRangeAsync(newMappings);
                    await context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<string>> GetUserPermissionsAsync(int roleId)
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.RolePermissions
                .AsNoTracking()
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission.SystemName)
                .ToListAsync();
        }

        #endregion

    }
}
