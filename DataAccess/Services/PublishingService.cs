using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public interface IPublishingService
{
    // ✨ استقبال DTO بدلاً من الكيان
    Task LogPublishingAsync(PublishingLogDto dto, UserSession session);
}

// ✨ استخدام Primary Constructor
public class PublishingService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IPublishingService
{
    public async Task LogPublishingAsync(PublishingLogDto dto, UserSession session)
    {
        // ✨ استخدام الـ Extension Method
        session.EnsurePermission(AppPermissions.EpisodePublish);

        using var context = await contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // ✨ إنشاء الكيان من الـ DTO داخل الـ Service فقط (حماية من Mass Assignment)
            var log = new PublishingLog
            {
                EpisodeId = dto.EpisodeId,
                PublishedByUserId = session.UserId, // حقل أعمال خاص بالنشر
                YouTubeUrl = dto.YouTubeUrl,
                SoundCloudUrl = dto.SoundCloudUrl,
                FacebookUrl = dto.FacebookUrl,
                TwitterUrl = dto.TwitterUrl
            };

            context.PublishingLogs.Add(log);

            var episode = await context.Episodes.FindAsync(dto.EpisodeId);

            if (episode == null)
                throw new KeyNotFoundException("عذراً، لم يتم العثور على الحلقة المطلوبة.");

            // ✨ استخدام الثوابت بدلاً من الأرقام السحرية
            if (episode.StatusId != EpisodeStatus.Executed)
                throw new InvalidOperationException("لا يمكن نشر حلقة لم يتم توثيق تنفيذها (الإنتاج) أولاً.");

            episode.StatusId = EpisodeStatus.Published;
            // ✨ الإشارة للـ Interceptor أنه سيتولى UpdatedAt و UpdatedByUserId

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            // ❌ تم إزالة MessageService (الـ UI سيتولى عرض رسالة النجاح)
        }
        catch
        {
            await transaction.RollbackAsync();
            throw; // ✨ رمي الاستثناء الأصلي كما هو للحفاظ على الـ Stack Trace
        }
    }
}