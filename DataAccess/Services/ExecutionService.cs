using BroadcastWorkflow.Services;
using DataAccess.Common;
using DataAccess.Services.Messaging;
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

                // 4. تحديث حالة الحلقة مباشرة عبر EF Core (بديل الـ Stored Procedure)
                var episode = await context.Episodes.FindAsync(log.EpisodeId);
                if (episode != null)
                {
                    // التحقق من قواعد العمل
                    if (episode.StatusId == 2)
                        throw new Exception("لا يمكن تعديل حالة حلقة تم نشرها بالفعل.");

                    episode.StatusId = 1; // 1 = منفّذة (Executed)
                    episode.ActualExecutionTime = DateTime.UtcNow;
                    episode.UpdatedByUserId = session.UserId;
                    episode.UpdatedAt = DateTime.UtcNow;
                }

                // حفظ كل التغييرات في قاعدة البيانات
                await context.SaveChangesAsync();

                // تثبيت المعاملة
                await transaction.CommitAsync();

                // إظهار رسالة نجاح
                MessageService.Current.ShowSuccess("تم تسجيل بيانات التنفيذ وتحديث حالة الحلقة بنجاح.");
            }
            catch (Exception ex)
            {
                // التراجع عن كل شيء في حال حدوث خطأ
                await transaction.RollbackAsync();
                throw new Exception($"حدث خطأ أثناء تسجيل التنفيذ: {ex.Message}");
            }
        }


    }
}