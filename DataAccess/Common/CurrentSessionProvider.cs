using Microsoft.EntityFrameworkCore;
using Domain.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace DataAccess.Common
{
    /// <summary>
    /// كلاس وحيد (Singleton) يحتفظ ببيانات المستخدم الحالي لكي تصل إليها قاعدة البيانات تلقائياً
    /// </summary>
    public class CurrentSessionProvider(IServiceProvider serviceProvider)
    {
        public UserSession? CurrentSession { get; set; }

        public async Task RefreshPermissionsAsync()
        {
            if (CurrentSession == null) return;

            var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<BroadcastWorkflowDBContext>>();
            await using var context = await contextFactory.CreateDbContextAsync();
            
            var userRoleInfo = await context.Users
                .AsNoTracking()
                .Where(u => u.UserId == CurrentSession.UserId)
                .Select(u => new
                {
                    RoleName = u.Role != null ? u.Role.RoleName : "Unknown",
                    Permissions = u.Role != null
                        ? u.Role.RolePermissions
                            .Select(rp => rp.Permission.SystemName)
                            .ToList()
                        : new List<string>()
                })
                .FirstOrDefaultAsync();

            if (userRoleInfo != null)
            {
                CurrentSession.RoleName = userRoleInfo.RoleName;
                CurrentSession.Permissions = userRoleInfo.Permissions;
            }
        }
    }
}


