using BroadcastWorkflow.Services;
using DataAccess.Common;
using DataAccess.Services.Messaging;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    public interface IPublishingService
    {
        Task LogPublishingAsync(PublishingLog log, UserSession session);
    }

    public class PublishingService : IPublishingService
    {
        private readonly IDbContextFactory<BroadcastWorkflowDBContext> _contextFactory;
        public PublishingService(IDbContextFactory<BroadcastWorkflowDBContext> factory) => _contextFactory = factory;

        public async Task LogPublishingAsync(PublishingLog log, UserSession session)
        {
            // 1. التحقق من الصلاحية (نشر الحلقات)
            SecurityHelper.EnsurePermission(session, AppPermissions.EpisodePublish);

            using var context = await _contextFactory.CreateDbContextAsync();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // 2. إضافة سجل النشر الرقمي
                log.PublishedByUserId = session.UserId;
                context.PublishingLogs.Add(log);

                // 3. تحديث حالة الحلقة مباشرة (بديل الـ SP)
                var episode = await context.Episodes.FindAsync(log.EpisodeId);

                if (episode == null)
                    throw new Exception("عذراً، لم يتم العثور على الحلقة المطلوبة.");

                // التأكد من أن الحلقة في حالة "منفذة" قبل السماح بنشرها
                if (episode.StatusId != 1)
                    throw new Exception("لا يمكن نشر حلقة لم يتم توثيق تنفيذها (الإنتاج) أولاً.");

                episode.StatusId = 2; // 2 = منشورة (Published)

                // ملاحظة: التواريخ والمستخدم يتم تحديثهم تلقائياً عبر الـ Interceptor
                await context.SaveChangesAsync();

                // 4. تثبيت العملية
                await transaction.CommitAsync();

                // إشعار النجاح عبر النظام المركزي
                MessageService.Current.ShowSuccess("تم توثيق روابط النشر وتحديث حالة الحلقة بنجاح.");
            }
            catch (Exception)
            {
                // التراجع عن كل شيء في حال حدوث خطأ
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

}
