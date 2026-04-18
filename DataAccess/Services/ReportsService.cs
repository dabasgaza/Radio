using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    public interface IReportsService
    {
        Task<List<TodayEpisodeDto>> GetTodayEpisodesAsync();
        Task<Dictionary<string, int>> GetEpisodeStatusStatsAsync();
        Task<List<ActiveProgramDto>> GetMostActiveProgramsAsync();
    }

    public class ReportsService : IReportsService
    {
        private readonly IDbContextFactory<BroadcastWorkflowDBContext> _contextFactory;
        public ReportsService(IDbContextFactory<BroadcastWorkflowDBContext> factory) => _contextFactory = factory;

        // 1. إحصائيات الحالات (معتمد كلياً على Episodes)
        public async Task<Dictionary<string, int>> GetEpisodeStatusStatsAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            // جلب الإحصائيات مباشرة من جدول الحلقات
            var stats = await context.Episodes.AsNoTracking()
                .GroupBy(e => e.EpisodeStatus.StatusName)
                .Select(g => new { StatusName = g.Key, Count = g.Count() })
                .ToListAsync();

            return stats.ToDictionary(x => x.StatusName, x => x.Count);
        }

        // 2. جدول بث اليوم (محوره الحلقة)
        public async Task<List<TodayEpisodeDto>> GetTodayEpisodesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var today = DateTime.Today;

            return await context.Episodes
                .AsNoTracking()
                .Where(e => e.ScheduledExecutionTime.HasValue &&
                            e.ScheduledExecutionTime.Value.Date == today)
                .OrderBy(e => e.ScheduledExecutionTime)
                .Select(e => new TodayEpisodeDto
                (
                    e.EpisodeId,
                    e.EpisodeName,
                    e.Program.ProgramName,
                    // جلب اسم الضيف مباشرة من العلاقة الجديدة
                    e.Guest != null ? e.Guest.FullName : "بدون ضيف",
                    e.ScheduledExecutionTime,
                    e.EpisodeStatus.DisplayName
                ))
                .ToListAsync();

        }

        // 3. تقرير البرامج الأكثر نشاطاً (بدلاً من الضيوف، لتركيز التقرير على الحلقات)
        // هذا التقرير يخبرك أي البرامج تستهلك أكبر عدد من الحلقات
        public async Task<List<ActiveEpisodeDto>> GetActiveEpisodesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            return await context.Episodes
                .AsNoTracking()
                .Select(e => new ActiveEpisodeDto
                {
                    EpisodeId = e.EpisodeId,
                    EpisodeName = e.EpisodeName,
                    ProgramName = e.Program.ProgramName,
                    // جلب اسم الضيف مباشرة أو وضع نص في حال عدم وجوده
                    GuestName = e.Guest != null ? e.Guest.FullName : "بدون ضيف",
                    ScheduledExecutionTime = e.ScheduledExecutionTime,
                    StatusText = e.EpisodeStatus.DisplayName,
                    StatusId = e.StatusId
                })
                .ToListAsync();
        }

        public async Task<List<ActiveProgramDto>> GetMostActiveProgramsAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            return await context.Programs
                .AsNoTracking()
                .Select(p => new ActiveProgramDto
                {
                    ProgramName = p.ProgramName,
                    Category = p.Category,
                    // حساب عدد الحلقات الكلي المرتبط بالبرنامج
                    TotalEpisodes = p.Episodes.Count(),
                    // حساب عدد الحلقات التي وصلت لحالة النشر (StatusId = 2)
                    PublishedEpisodes = p.Episodes.Count(e => e.StatusId == 2)
                })
                .OrderByDescending(x => x.TotalEpisodes)
                .Take(5) // جلب أعلى 5 برامج نشاطاً
                .ToListAsync();
        }
    }
}
