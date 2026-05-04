using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

public interface IPublishingService
{
    Task<Result> LogWebsitePublishingAsync(int episodeId, string title, MediaType mediaType, string notes, UserSession session);

    /// <summary>
    /// تسجيل النشر الاجتماعي لكل ضيف في الحلقة مع المنصات والروابط
    /// </summary>
    Task<Result> LogSocialPublishingAsync(int episodeId, List<SocialMediaPublishingLogDto> guestLogs, UserSession session);

    // المتطلبات الجديدة من الـ Dialogs
    Task<List<SocialMediaPlatformDto>> GetAllPlatformsAsync();
    Task<Result<int>> SavePublishingLogAsync(SocialMediaPublishingLogDto dto, UserSession session);
    Task<Result<int>> PublishToWebsiteAsync(WebsitePublishingLogDto dto, UserSession session);
}

// ✨ استخدام Primary Constructor
public class PublishingService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IPublishingService
{
    public async Task<Result> LogWebsitePublishingAsync(int episodeId, string title, MediaType mediaType, string notes, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodePublish);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            var log = new WebsitePublishingLog
            {
                EpisodeId = episodeId,
                PublishedByUserId = session.UserId,
                Title = title,
                MediaType = mediaType,
                Notes = notes,
                PublishedAt = DateTime.UtcNow
            };

            context.WebsitePublishingLogs.Add(log);

            var episode = await context.Episodes.FindAsync(episodeId);

            if (episode == null)
                return Result.Fail("عذراً، لم يتم العثور على الحلقة المطلوبة.");

            if (episode.StatusId != EpisodeStatus.Executed)
                return Result.Fail("لا يمكن نشر حلقة لم يتم توثيق تنفيذها (الإنتاج) أولاً.");

            episode.StatusId = EpisodeStatus.Published;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result.Success();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<Result> LogSocialPublishingAsync(int episodeId, List<SocialMediaPublishingLogDto> guestLogs, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodePublish);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // 1. التحقق من وجود الحلقة
            var episode = await context.Episodes.FindAsync(episodeId);
            if (episode == null)
                return Result.Fail("عذراً، لم يتم العثور على الحلقة المطلوبة.");

            if (episode.StatusId != EpisodeStatus.Executed)
                return Result.Fail("لا يمكن نشر حلقة لم يتم توثيق تنفيذها (الإنتاج) أولاً.");

            var now = DateTime.UtcNow;

            // 2. إنشاء سجلات النشر لكل ضيف
            foreach (var g in guestLogs)
            {
                var log = new SocialMediaPublishingLog
                {
                    EpisodeGuestId = g.EpisodeGuestId,
                    PublishedByUserId = session.UserId,
                    MediaType = MediaType.Both, // سيتم تحديثه لاحقاً من الواجهة
                    ClipTitle = g.ClipTitle,
                    ClipDuration = g.Duration,
                    PublishedAt = now
                };

                context.SocialMediaPublishingLogs.Add(log);
                await context.SaveChangesAsync(); // نحتاج الـ ID لربط المنصات

                // 3. إنشاء روابط المنصات لكل سجل
                foreach (var p in g.Platforms)
                {
                    var platformLink = new SocialMediaPublishingLogPlatform
                    {
                        SocialMediaPublishingLogId = log.SocialMediaPublishingLogId,
                        SocialMediaPlatformId = p.PlatformId,
                        Url = p.Url
                    };
                    context.SocialMediaPublishingLogPlatforms.Add(platformLink);
                }

                // 4. تحديث ClipStatus للضيف
                var episodeGuest = await context.EpisodeGuests.FindAsync(g.EpisodeGuestId);
                if (episodeGuest != null)
                {
                    episodeGuest.ClipStatus = GuestClipStatus.Published;
                }
            }

            // 5. تحديث حالة الحلقة
            episode.StatusId = EpisodeStatus.Published;

            await context.SaveChangesAsync();
            await transaction.CommitAsync();
            return Result.Success();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// الحصول على جميع منصات السوشيال ميديا المتاحة
    /// </summary>
    public async Task<List<SocialMediaPlatformDto>> GetAllPlatformsAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();
        return await context.SocialMediaPlatforms
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new SocialMediaPlatformDto(p.SocialMediaPlatformId, p.Name, p.Icon))
            .ToListAsync();
    }

    /// <summary>
    /// حفظ سجل نشر اجتماعي لضيف مع المنصات والروابط
    /// </summary>
    public async Task<Result<int>> SavePublishingLogAsync(SocialMediaPublishingLogDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodePublish);
        if (!permCheck.IsSuccess) return Result<int>.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            var log = new SocialMediaPublishingLog
            {
                EpisodeGuestId = dto.EpisodeGuestId,
                ClipTitle = dto.ClipTitle,
                ClipDuration = dto.Duration,
                PublishedAt = DateTime.UtcNow,
                PublishedByUserId = session.UserId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.SocialMediaPublishingLogs.Add(log);
            await context.SaveChangesAsync();

            // إضافة المنصات والروابط
            foreach (var platform in dto.Platforms)
            {
                var platformLink = new SocialMediaPublishingLogPlatform
                {
                    SocialMediaPublishingLogId = log.SocialMediaPublishingLogId,
                    SocialMediaPlatformId = platform.PlatformId,
                    Url = platform.Url,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.SocialMediaPublishingLogPlatforms.Add(platformLink);
            }

            await context.SaveChangesAsync();
            return Result<int>.Success(log.SocialMediaPublishingLogId);
        }
        catch (Exception ex)
        {
            return Result<int>.Fail($"خطأ في حفظ سجل النشر: {ex.Message}");
        }
    }

    /// <summary>
    /// نشر حلقة على الموقع الإلكتروني
    /// </summary>
    public async Task<Result<int>> PublishToWebsiteAsync(WebsitePublishingLogDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeWebPublish);
        if (!permCheck.IsSuccess) return Result<int>.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();
        try
        {
            // التحقق من وجود الحلقة
            var episode = await context.Episodes.FindAsync(dto.EpisodeId);
            
            if (episode == null) return Result<int>.Fail("الحلقة غير موجودة");

            var log = new WebsitePublishingLog
            {
                EpisodeId = dto.EpisodeId,
                Title = dto.Title,
                MediaType = Enum.TryParse<MediaType>(dto.MediaType, true, out var parsedMediaType) ? parsedMediaType : MediaType.Audio,
                Notes = dto.Notes,
                PublishedAt = DateTime.UtcNow,
                PublishedByUserId = session.UserId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            context.WebsitePublishingLogs.Add(log);
            
            // تحديث حالة الحلقة إلى WebsitePublished
            episode.StatusId = EpisodeStatus.WebsitePublished;
            
            await context.SaveChangesAsync();
            return Result<int>.Success(log.WebsitePublishingLogId);
        }
        catch (Exception ex)
        {
            return Result<int>.Fail($"خطأ في نشر الحلقة: {ex.Message}");
        }
    }
}
