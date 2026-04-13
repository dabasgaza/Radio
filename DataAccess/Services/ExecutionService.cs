using BroadcastWorkflow.Services;
using DataAccess.Common;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    public interface IExecutionService
    {
        Task LogExecutionAsync(ExecutionLog log, UserSession session);
    }

    public class ExecutionService : IExecutionService
    {
        private readonly IDbContextFactory<BroadcastWorkflowDBContext> _contextFactory;
        public ExecutionService(IDbContextFactory<BroadcastWorkflowDBContext> factory) => _contextFactory = factory;

        public async Task LogExecutionAsync(ExecutionLog log, UserSession session)
        {
            // 1. التحقق من الصلاحية: هل المستخدم منتج أو منسق؟
            SecurityHelper.EnsureRole(session, AppPermissions.CoordinationManage, AppPermissions.ProgramManage);

            using var context = await _contextFactory.CreateDbContextAsync();
            // 2. بدء معاملة (Transaction) لضمان تنفيذ كل شيء أو لا شيء
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // 3. ربط السجل بالمستخدم الحالي وحفظه
                log.ExecutedByUserId = session.UserId;
                context.ExecutionLogs.Add(log);

                // 4. استدعاء الـ Stored Procedure لتغيير حالة الحلقة إلى 1 (Executed)
                await context.Database.ExecuteSqlRawAsync("EXEC sp_UpdateEpisodeStatus @p0, 1, @p1", log.EpisodeId, session.UserId);

                // 5. حفظ التغييرات نهائياً
                await context.SaveChangesAsync();

                // 6. تثبيت المعاملة
                await transaction.CommitAsync();
            }
            catch
            {
                // 7. في حال حدوث أي خطأ، يتم التراجع عن كل شيء (Rollback)
                await transaction.RollbackAsync();
                throw; // إعادة إرسال الخطأ للواجهة لعرضه للمستخدم
            }
        }


    }
}