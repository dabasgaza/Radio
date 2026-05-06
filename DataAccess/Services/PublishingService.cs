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

    // ═══════════════════════════════════════════
    //  دوال استرجاع وتعديل سجلات النشر
    // ═══════════════════════════════════════════

    /// <summary>
    /// استرجاع سجلات النشر الرقمي لضيف معيّن في حلقة
    /// </summary>
    Task<SocialMediaPublishingLogDto?> GetSocialPublishingLogAsync(int episodeGuestId);

    /// <summary>
    /// استرجاع جميع سجلات النشر الرقمي لحلقة معيّنة (ضيف لكل سجل)
    /// </summary>
    Task<List<SocialMediaPublishingLogDto>> GetEpisodeSocialLogsAsync(int episodeId);

    /// <summary>
    /// تعديل سجل نشر رقمي موجود (عنوان المقطع، المدة، نوع الوسائط، المنصات والروابط)
    /// </summary>
    Task<Result> UpdateSocialPublishingLogAsync(SocialMediaPublishingLogDto dto, UserSession session);

    /// <summary>
    /// استرجاع سجل نشر الموقع الإلكتروني لحلقة معيّنة
    /// </summary>
    Task<WebsitePublishingLogDto?> GetWebsitePublishingLogAsync(int episodeId);

    /// <summary>
    /// تعديل سجل نشر الموقع الإلكتروني
    /// </summary>
    Task<Result> UpdateWebsitePublishingLogAsync(WebsitePublishingLogDto dto, UserSession session);

    /// <summary>
    /// استرجاع قائمة موحّدة من جميع سجلات النشر (أنواعها الثلاثة)
    /// يُستخدم في شاشة العرض الشامل مع دعم الفلترة حسب الحلقة
    /// </summary>
    Task<List<PublishingRecordDto>> GetAllPublishingRecordsAsync(int? episodeId = null);
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
                    MediaType = g.MediaType,
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
                MediaType = dto.MediaType,
                ClipTitle = dto.ClipTitle,
                ClipDuration = dto.Duration,
                PublishedAt = DateTime.UtcNow,
                PublishedByUserId = session.UserId
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
                    Url = platform.Url
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

            // التحقق من أن حالة الحلقة تسمح بنشر الموقع (منفذة أو منشورة اجتماعياً على الأقل)
            if (episode.StatusId < EpisodeStatus.Executed)
                return Result<int>.Fail("يجب تنفيذ الحلقة أولاً قبل نشرها على الموقع.");

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

    // ═══════════════════════════════════════════
    //  استرجاع وتعديل سجلات النشر
    // ═══════════════════════════════════════════

    /// <summary>
    /// استرجاع سجل النشر الرقمي لضيف معيّن
    /// يُرجع null إذا لم يوجد سجل نشط لهذا الضيف
    /// </summary>
    public async Task<SocialMediaPublishingLogDto?> GetSocialPublishingLogAsync(int episodeGuestId)
    {
        // لا نحتاج صلاحية خاصة — مجرد قراءة
        using var context = await contextFactory.CreateDbContextAsync();

        var log = await context.SocialMediaPublishingLogs
            .AsNoTracking()
            .Include(l => l.Platforms)
            .Where(l => l.EpisodeGuestId == episodeGuestId && l.IsActive)
            .OrderByDescending(l => l.PublishedAt)  // أحدث سجل أولاً
            .FirstOrDefaultAsync();

        if (log is null) return null;

        // تحويل الكيان إلى DTO مع المنصات والروابط
        return new SocialMediaPublishingLogDto(
            log.SocialMediaPublishingLogId,
            log.EpisodeGuestId,
            log.ClipTitle,
            log.ClipDuration,
            log.MediaType,
            log.Platforms
                .Where(p => p.IsActive)
                .Select(p => new PlatformPublishDto(
                    p.SocialMediaPlatformId,
                    p.SocialMediaPlatform.Name,
                    p.Url))
                .ToList());
    }

    /// <summary>
    /// استرجاع جميع سجلات النشر الرقمي لحلقة معيّنة
    /// كل ضيف في الحلقة قد يكون له سجل واحد
    /// </summary>
    public async Task<List<SocialMediaPublishingLogDto>> GetEpisodeSocialLogsAsync(int episodeId)
    {
        using var context = await contextFactory.CreateDbContextAsync();

        // البحث عن سجلات النشر الرقمي المرتبطة بضيوف الحلقة
        var logs = await context.SocialMediaPublishingLogs
            .AsNoTracking()
            .Include(l => l.Platforms)
                .ThenInclude(p => p.SocialMediaPlatform)
            .Include(l => l.EpisodeGuest)
            .Where(l => l.EpisodeGuest.EpisodeId == episodeId && l.IsActive)
            .OrderByDescending(l => l.PublishedAt)
            .ToListAsync();

        // تحويل كل كيان إلى DTO
        return logs.Select(log => new SocialMediaPublishingLogDto(
            log.SocialMediaPublishingLogId,
            log.EpisodeGuestId,
            log.ClipTitle,
            log.ClipDuration,
            log.MediaType,
            log.Platforms
                .Where(p => p.IsActive)
                .Select(p => new PlatformPublishDto(
                    p.SocialMediaPlatformId,
                    p.SocialMediaPlatform.Name,
                    p.Url))
                .ToList())).ToList();
    }

    /// <summary>
    /// تعديل سجل نشر رقمي موجود
    /// يحدّث: عنوان المقطع، المدة، نوع الوسائط
    /// ويستبدل جميع روابط المنصات (يحذف القديمة ويضيف الجديدة)
    /// </summary>
    public async Task<Result> UpdateSocialPublishingLogAsync(SocialMediaPublishingLogDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodePublish);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // 1. البحث عن السجل الموجود
            var log = await context.SocialMediaPublishingLogs
                .Include(l => l.Platforms)
                .FirstOrDefaultAsync(l => l.SocialMediaPublishingLogId == dto.LogId && l.IsActive);

            if (log is null)
                return Result.Fail("سجل النشر غير موجود أو تم حذفه.");

            // 2. تحديث الحقول الأساسية
            log.ClipTitle = dto.ClipTitle;
            log.ClipDuration = dto.Duration;
            log.MediaType = dto.MediaType;
            log.UpdatedByUserId = session.UserId;

            // 3. حذف روابط المنصات القديمة (soft-delete)
            foreach (var oldPlatform in log.Platforms)
            {
                oldPlatform.IsActive = false;
            }

            // 4. إضافة روابط المنصات الجديدة
            foreach (var platform in dto.Platforms)
            {
                var newLink = new SocialMediaPublishingLogPlatform
                {
                    SocialMediaPublishingLogId = log.SocialMediaPublishingLogId,
                    SocialMediaPlatformId = platform.PlatformId,
                    Url = platform.Url
                };
                context.SocialMediaPublishingLogPlatforms.Add(newLink);
            }

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
    /// استرجاع سجل نشر الموقع الإلكتروني لحلقة معيّنة
    /// يُرجع null إذا لم يوجد سجل نشط
    /// </summary>
    public async Task<WebsitePublishingLogDto?> GetWebsitePublishingLogAsync(int episodeId)
    {
        using var context = await contextFactory.CreateDbContextAsync();

        var log = await context.WebsitePublishingLogs
            .AsNoTracking()
            .Where(l => l.EpisodeId == episodeId && l.IsActive)
            .OrderByDescending(l => l.PublishedAt)
            .FirstOrDefaultAsync();

        if (log is null) return null;

        return new WebsitePublishingLogDto(
            log.WebsitePublishingLogId,
            log.EpisodeId,
            log.MediaType.ToString(),
            log.Title,
            log.Notes,
            log.PublishedAt);
    }

    /// <summary>
    /// تعديل سجل نشر الموقع الإلكتروني
    /// يحدّث: العنوان، نوع الوسائط، الملاحظات
    /// </summary>
    public async Task<Result> UpdateWebsitePublishingLogAsync(WebsitePublishingLogDto dto, UserSession session)
    {
        var permCheck = session.EnsurePermission(AppPermissions.EpisodeWebPublish);
        if (!permCheck.IsSuccess) return Result.Fail(permCheck.ErrorMessage!);

        using var context = await contextFactory.CreateDbContextAsync();

        var log = await context.WebsitePublishingLogs
            .FirstOrDefaultAsync(l => l.WebsitePublishingLogId == dto.Id && l.IsActive);

        if (log is null)
            return Result.Fail("سجل نشر الموقع غير موجود أو تم حذفه.");

        // تحديث الحقول القابلة للتعديل
        log.Title = dto.Title;
        log.MediaType = Enum.TryParse<MediaType>(dto.MediaType, true, out var mt) ? mt : MediaType.Audio;
        log.Notes = dto.Notes;
        log.UpdatedByUserId = session.UserId;

        await context.SaveChangesAsync();
        return Result.Success();
    }

    /// <summary>
    /// استرجاع قائمة موحّدة من جميع سجلات النشر (الأنواع الثلاثة)
    /// تُستخدم في شاشة العرض الشامل مع دعم الفلترة حسب الحلقة
    /// </summary>
    public async Task<List<PublishingRecordDto>> GetAllPublishingRecordsAsync(int? episodeId = null)
    {
        using var context = await contextFactory.CreateDbContextAsync();

        var records = new List<PublishingRecordDto>();

        // 1. سجلات التنفيذ
        var execQuery = context.ExecutionLogs
            .AsNoTracking()
            .Include(l => l.Episode)
                .ThenInclude(e => e.Program)
            .Include(l => l.ExecutedByUser)
            .Where(l => l.IsActive);

        // فلترة حسب الحلقة إن طُلبت
        if (episodeId.HasValue)
            execQuery = execQuery.Where(l => l.EpisodeId == episodeId.Value);

        var execLogs = await execQuery
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();

        foreach (var log in execLogs)
        {
            records.Add(new PublishingRecordDto
            {
                RecordId = log.ExecutionLogId,
                RecordType = "Execution",
                EpisodeId = log.EpisodeId,
                EpisodeName = log.Episode?.EpisodeName,
                ProgramName = log.Episode?.Program?.ProgramName,
                Summary = $"مدة التنفيذ: {log.DurationMinutes} دقيقة",
                RecordDate = log.CreatedAt,
                RecordedBy = log.ExecutedByUser?.FullName,
                RecordIcon = "PlayCircleOutline",
                RecordColor = "#4CAF50"  // أخضر
            });
        }

        // 2. سجلات النشر الرقمي (سوشال ميديا)
        var socialQuery = context.SocialMediaPublishingLogs
            .AsNoTracking()
            .Include(l => l.EpisodeGuest)
                .ThenInclude(eg => eg.Guest)
            .Include(l => l.EpisodeGuest.Episode)
                .ThenInclude(e => e.Program)
            .Include(l => l.PublishedByUser)
            .Include(l => l.Platforms)
                .ThenInclude(p => p.SocialMediaPlatform)
            .Where(l => l.IsActive);

        if (episodeId.HasValue)
            socialQuery = socialQuery.Where(l => l.EpisodeGuest.EpisodeId == episodeId.Value);

        var socialLogs = await socialQuery
            .OrderByDescending(l => l.PublishedAt)
            .ToListAsync();

        foreach (var log in socialLogs)
        {
            // تجميع أسماء المنصات المنشورة للعرض كملخص
            var platformNames = log.Platforms
                .Where(p => p.IsActive)
                .Select(p => p.SocialMediaPlatform?.Name ?? "غير معروف")
                .ToList();

            records.Add(new PublishingRecordDto
            {
                RecordId = log.SocialMediaPublishingLogId,
                RecordType = "SocialMedia",
                EpisodeId = log.EpisodeGuest.EpisodeId,
                EpisodeName = log.EpisodeGuest?.Episode?.EpisodeName,
                ProgramName = log.EpisodeGuest?.Episode?.Program?.ProgramName,
                Summary = $"{log.EpisodeGuest?.Guest?.FullName} → {string.Join("، ", platformNames)}",
                RecordDate = log.PublishedAt,
                RecordedBy = log.PublishedByUser?.FullName,
                RecordIcon = "ShareVariant",
                RecordColor = "#2196F3"  // أزرق
            });
        }

        // 3. سجلات نشر الموقع الإلكتروني
        var webQuery = context.WebsitePublishingLogs
            .AsNoTracking()
            .Include(l => l.Episode)
                .ThenInclude(e => e.Program)
            .Include(l => l.PublishedByUser)
            .Where(l => l.IsActive);

        if (episodeId.HasValue)
            webQuery = webQuery.Where(l => l.EpisodeId == episodeId.Value);

        var webLogs = await webQuery
            .OrderByDescending(l => l.PublishedAt)
            .ToListAsync();

        foreach (var log in webLogs)
        {
            records.Add(new PublishingRecordDto
            {
                RecordId = log.WebsitePublishingLogId,
                RecordType = "Website",
                EpisodeId = log.EpisodeId,
                EpisodeName = log.Episode?.EpisodeName,
                ProgramName = log.Episode?.Program?.ProgramName,
                Summary = log.Title ?? "بدون عنوان",
                RecordDate = log.PublishedAt,
                RecordedBy = log.PublishedByUser?.FullName,
                RecordIcon = "Web",
                RecordColor = "#5C6BC0"  // بنفسجي
            });
        }

        // ترتيب نهائي: الأحدث أولاً
        return records
            .OrderByDescending(r => r.RecordDate)
            .ToList();
    }
}
