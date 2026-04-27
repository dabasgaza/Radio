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
    Task ToggleWebsitePublishAsync(int episodeId, bool isPublished, UserSession session);
    Task DeleteEpisodeAsync(int episodeId, UserSession session);
    Task<List<EpisodeGuestDto>> GetEpisodeGuestsAsync(int episodeId);
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
                EpisodeName = e.EpisodeName,
                GuestsDisplay = FormatGuestsDisplay(e.EpisodeGuests),
                ProgramName = e.Program.ProgramName,
                ScheduledExecutionTime = e.ScheduledExecutionTime,
                StatusText = e.EpisodeStatus.DisplayName,
                SpecialNotes = e.SpecialNotes,
                IsWebsitePublished = e.IsWebsitePublished
            }).ToListAsync();
    }

    public async Task CreateEpisodeAsync(EpisodeDto dto, UserSession session)
    {
        session.EnsurePermission(AppPermissions.EpisodeManage);
        using var context = await contextFactory.CreateDbContextAsync();

        var episode = new Episode
        {
            ProgramId = dto.ProgramId,
            EpisodeName = dto.EpisodeName,
            ScheduledExecutionTime = dto.ScheduledTime,
            StatusId = EpisodeStatus.Planned, // ✨ استخدام الثوابت
            SpecialNotes = dto.SpecialNotes,
            IsWebsitePublished = false
        };

        // ✅ إضافة الضيوف مع حماية من null
        if (dto.Guests is { Count: > 0 })
            AddGuestsToEpisode(episode, dto.Guests);

        context.Episodes.Add(episode);
        await context.SaveChangesAsync();
    }

    public async Task UpdateEpisodeAsync(EpisodeDto dto, UserSession session)
    {
        session.EnsurePermission(AppPermissions.EpisodeManage);
        using var context = await contextFactory.CreateDbContextAsync();

        var episode = await context.Episodes
            .Include(e => e.EpisodeGuests)     // ✅ ضروري
            .FirstOrDefaultAsync(e => e.EpisodeId == dto.EpisodeId);

        if (episode == null) throw new KeyNotFoundException("الحلقة غير موجودة.");

        // تحديث الحقول
        episode.ProgramId = dto.ProgramId;
        //episode.GuestId = dto.GuestId;
        episode.EpisodeName = dto.EpisodeName;
        episode.ScheduledExecutionTime = dto.ScheduledTime;
        episode.SpecialNotes = dto.SpecialNotes;


        // ✅ مزامنة الضيوف — حذف القديم ثم إضافة الجديد
        context.EpisodeGuests.RemoveRange(episode.EpisodeGuests.ToList());

        AddGuestsToEpisode(episode, dto.Guests);

        // الـ Interceptor سيتولى تحديث UpdatedAt و UpdatedByUserId تلقائياً
        await context.SaveChangesAsync();
    }

    public async Task<List<EpisodeGuestDto>> GetEpisodeGuestsAsync(int episodeId)
    {
        await using var context = await contextFactory.CreateDbContextAsync();

        return await context.EpisodeGuests
            .AsNoTracking()
            .Where(eg => eg.EpisodeId == episodeId)
            .Select(eg => new EpisodeGuestDto(
                eg.GuestId,
                eg.Topic,
                eg.HostingTime))
            .ToListAsync();
    }

    // تحديث الحالة مع قواعد الـ Workflow والصلاحيات    
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
            .Include(e => e.EpisodeGuests)
            .FirstOrDefaultAsync(e => e.EpisodeId == episodeId);

        if (episode == null) throw new KeyNotFoundException("الحلقة غير موجودة.");

        // ✅ قاعدة عمل: منع حذف الحلقات المنفّذة أو المنشورة
        if (episode.ExecutionLogs.Any())
            throw new InvalidOperationException("لا يمكن حذف حلقة تم تنفيذها، يُرجى إلغاء التنفيذ أولاً.");

        if (episode.PublishingLogs.Any())
            throw new InvalidOperationException("لا يمكن حذف حلقة تم نشرها، يُرجى إلغاء النشر أولاً.");

        episode.IsActive = false;

        foreach (var guest in episode.EpisodeGuests)
            guest.IsActive = false;


        await context.SaveChangesAsync();

    }

    public async Task ToggleWebsitePublishAsync(int episodeId, bool isPublished, UserSession session)
    {
        session.EnsurePermission(AppPermissions.EpisodeWebPublish);
        await using var context = await contextFactory.CreateDbContextAsync();

        var episode = await context.Episodes.FindAsync(episodeId)
            ?? throw new KeyNotFoundException("الحلقة غير موجودة.");

        // ✅ يمكن نشر الحلقات المنفذة أو المنشورة رقمياً فقط
        if (episode.StatusId < EpisodeStatus.Executed)
            throw new InvalidOperationException("لا يمكن نشر حلقة على الموقع قبل تنفيذها.");

        episode.IsWebsitePublished = isPublished;
        await context.SaveChangesAsync();
    }


    #region Private Helpers

    /// <summary>
    /// تحويل كيان Episode إلى ActiveEpisodeDto مع تنسيق أسماء الضيوف.
    /// </summary>
    private static ActiveEpisodeDto MapToActiveDto(Episode e)
    {
        return new ActiveEpisodeDto
        {
            EpisodeId = e.EpisodeId,
            StatusId = e.StatusId,
            ProgramId = e.ProgramId,
            EpisodeName = e.EpisodeName,
            ProgramName = e.Program.ProgramName,
            GuestsDisplay = FormatGuestsDisplay(e.EpisodeGuests),
            ScheduledExecutionTime = e.ScheduledExecutionTime,
            StatusText = e.EpisodeStatus.DisplayName,
            SpecialNotes = e.SpecialNotes,
            IsWebsitePublished = e.IsWebsitePublished
        };
    }

    /// <summary>
    /// تنسيق قائمة الضيوف كنص للعرض.
    /// مثال: "أحمد (08:30) — السياسة ، سعيد (09:45) — الاقتصاد"
    /// </summary>
    private static string FormatGuestsDisplay(IEnumerable<EpisodeGuest> guests)
    {
        var list = guests.OrderBy(g => g.HostingTime).ToList();

        if (list.Count == 0)
            return "لا يوجد ضيف";

        return string.Join(" ، ", list.Select(g =>
        {
            var name = g.Guest?.FullName ?? "غير معروف";
            if (g.HostingTime.HasValue)
                name += $" ({g.HostingTime.Value:hh\\:mm})";
            if (!string.IsNullOrWhiteSpace(g.Topic))
                name += $" — {g.Topic}";
            return name;
        }));
    }

    /// <summary>
    /// إضافة ضيوف إلى كيان الحلقة.
    /// </summary>
    private static void AddGuestsToEpisode(Episode episode, List<EpisodeGuestDto> guestDtos)
    {
        foreach (var guestDto in guestDtos)
        {
            episode.EpisodeGuests.Add(new EpisodeGuest
            {
                GuestId = guestDto.GuestId,
                Topic = guestDto.Topic,
                HostingTime = guestDto.HostingTime
            });
        }
    }

    #endregion

}