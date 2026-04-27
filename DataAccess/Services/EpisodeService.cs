using DataAccess.Common;
using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services;

// 1. استخدام الثوابت بدلاً من الأرقام السحرية لتسهيل القراءة
public static class EpisodeStatus
{
    public const byte Planned = 0;
    public const byte Executed = 1;
    public const byte Published = 2;
    public const byte Cancelled = 3;
}

public interface IEpisodeService
{
    Task<List<ActiveEpisodeDto>> GetActiveEpisodesAsync();
    Task CreateEpisodeAsync(EpisodeDto dto, UserSession session);
    Task UpdateEpisodeAsync(EpisodeDto dto, UserSession session);
    Task UpdateStatusAsync(int episodeId, byte newStatusId, UserSession session);
    Task DeleteEpisodeAsync(int episodeId, UserSession session);
}

// ✨ استخدام C# 13 Primary Constructor
public class EpisodeService(IDbContextFactory<BroadcastWorkflowDBContext> contextFactory) : IEpisodeService
{
    // تم إزالة IAuditService لأن الـ AuditInterceptor يتولى الأمر تلقائياً!

    public async Task<List<ActiveEpisodeDto>> GetActiveEpisodesAsync()
    {
        using var context = await contextFactory.CreateDbContextAsync();

        return await context.Episodes
            .AsNoTracking()
            .OrderBy(e => e.ScheduledExecutionTime)
            .Select(e => new ActiveEpisodeDto
            {
                EpisodeId = e.EpisodeId,
                StatusId = e.StatusId,
                ProgramId = e.ProgramId,
                GuestId = e.GuestId,
                EpisodeName = e.EpisodeName,
                GuestName = e.Guest != null ? e.Guest.FullName : "لا يوجد ضيف",
                ProgramName = e.Program.ProgramName,
                ScheduledExecutionTime = e.ScheduledExecutionTime,
                StatusText = e.EpisodeStatus.DisplayName,
                SpecialNotes = e.SpecialNotes
            }).ToListAsync();
    }

    public async Task UpdateEpisodeAsync(EpisodeDto dto, UserSession session)
    {
        session.EnsurePermission(AppPermissions.EpisodeManage);
        using var context = await contextFactory.CreateDbContextAsync();

        var episode = await context.Episodes.FindAsync(dto.EpisodeId);
        if (episode == null) throw new KeyNotFoundException("الحلقة غير موجودة.");

        // تحديث الحقول
        episode.ProgramId = dto.ProgramId;
        episode.GuestId = dto.GuestId;
        episode.EpisodeName = dto.EpisodeName;
        episode.ScheduledExecutionTime = dto.ScheduledTime;
        episode.SpecialNotes = dto.SpecialNotes;

        // الـ Interceptor سيتولى تحديث UpdatedAt و UpdatedByUserId تلقائياً
        await context.SaveChangesAsync();

        // ❌ تم إزالة MessageService.Current.ShowSuccess من هنا! الـ UI هي من تعرض الرسالة.
    }

    public async Task CreateEpisodeAsync(EpisodeDto dto, UserSession session)
    {
        session.EnsurePermission(AppPermissions.CoordinationManage);
        using var context = await contextFactory.CreateDbContextAsync();

        var episode = new Episode
        {
            ProgramId = dto.ProgramId,
            GuestId = dto.GuestId,
            EpisodeName = dto.EpisodeName,
            ScheduledExecutionTime = dto.ScheduledTime,
            StatusId = EpisodeStatus.Planned, // ✨ استخدام الثوابت
            SpecialNotes = dto.SpecialNotes,
            // ❌ لا حاجة لـ CreatedByUserId، الـ Interceptor سيضعه تلقائياً!
        };

        context.Episodes.Add(episode);
        await context.SaveChangesAsync();

        // ❌ تم إزالة استدعاء _audit.LogActionAsync! الـ Interceptor سجل البيانات والـ JSON تلقائياً
    }

    public async Task UpdateStatusAsync(int episodeId, byte newStatusId, UserSession session)
    {
        // التحقق من الصلاحيات بناءً على الحالة الجديدة
        if (newStatusId == EpisodeStatus.Executed)
            session.EnsurePermission(AppPermissions.EpisodeExecute);
        if (newStatusId == EpisodeStatus.Published)
            session.EnsurePermission(AppPermissions.EpisodePublish);

        using var context = await contextFactory.CreateDbContextAsync();

        var episode = await context.Episodes.FindAsync(episodeId);
        if (episode == null) throw new KeyNotFoundException("الحلقة غير موجودة.");

        // ✨ قواعد الـ Workflow باستخدام الثوابت (أوضح وأسهل للصيانة)
        if (episode.StatusId == EpisodeStatus.Published)
            throw new InvalidOperationException("لا يمكن تعديل حالة حلقة تم نشرها بالفعل.");

        if (episode.StatusId == EpisodeStatus.Executed && newStatusId == EpisodeStatus.Planned)
            throw new InvalidOperationException("لا يمكن إعادة حلقة منفذة إلى حالة الجدولة.");

        if (episode.StatusId == EpisodeStatus.Planned && newStatusId == EpisodeStatus.Published)
            throw new InvalidOperationException("يجب تنفيذ الحلقة وتوثيقها قبل عملية النشر الرقمي.");

        // تحديث الحالة
        episode.StatusId = newStatusId;

        if (newStatusId == EpisodeStatus.Executed)
            episode.ActualExecutionTime = DateTime.UtcNow;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("فشل التحديث: قام مستخدم آخر بتعديل حالة هذه الحلقة للتو.");
        }
    }

    public async Task DeleteEpisodeAsync(int episodeId, UserSession session)
    {
        session.EnsurePermission(AppPermissions.EpisodeManage);

        await using var context = await contextFactory.CreateDbContextAsync();

        var episode = await context.Episodes
            .FindAsync(episodeId)
            ?? throw new InvalidOperationException("الحلقة المحددة غير موجودة أو تم حذفها مسبقاً.");

        // ✅ قاعدة عمل: منع حذف الحلقات المنفّذة أو المنشورة
        if (episode.ExecutionLogs.Any())
            throw new InvalidOperationException("لا يمكن حذف حلقة تم تنفيذها، يُرجى إلغاء التنفيذ أولاً.");

        if (episode.PublishingLogs.Any())
            throw new InvalidOperationException("لا يمكن حذف حلقة تم نشرها، يُرجى إلغاء النشر أولاً.");

        episode.IsActive = false;

        await context.SaveChangesAsync();

    }
}