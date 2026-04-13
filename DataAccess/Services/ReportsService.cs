using DataAccess.DTOs;
using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    public interface IReportsService
    {
        Task<List<TodayEpisodeDto>> GetTodayEpisodesAsync();
        Task<Dictionary<string, int>> GetEpisodeStatusStatsAsync();
        Task<List<ActiveGuestDto>> GetTopGuestsAsync(int count = 10);
    }

    public class ReportsService : IReportsService
    {
        private readonly IDbContextFactory<BroadcastWorkflowDBContext> _contextFactory;
        public ReportsService(IDbContextFactory<BroadcastWorkflowDBContext> factory) => _contextFactory = factory;

        // بديل vw_TodayEpisodes
        public async Task<List<TodayEpisodeDto>> GetTodayEpisodesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var today = DateTime.Today;

            // 1. جلب البيانات من القاعدة
            var episodes = await context.Episodes
                .AsNoTracking()
                .Include(e => e.Program)
                .Include(e => e.EpisodeGuests).ThenInclude(eg => eg.Guest)
                .Where(e => e.IsActive && e.ScheduledExecutionTime.HasValue &&
                            e.ScheduledExecutionTime.Value.Date == today)
                .ToListAsync();

            // 2. التحويل إلى Record باستخدام الـ Constructor (أقواس دائرية)
            return episodes.Select(e => new TodayEpisodeDto(
                e.EpisodeId,
                e.EpisodeName,
                e.Program.ProgramName,
                e.ScheduledExecutionTime,
                string.Join(", ", e.EpisodeGuests.Where(eg => eg.IsActive).Select(eg => eg.Guest.FullName)),
                e.Status == 0 ? "مخطط لها" : (e.Status == 1 ? "تم التنفيذ" : "تم النشر")
            )).ToList();
        }

        // بديل vw_ActiveGuests
        public async Task<List<ActiveGuestDto>> GetTopGuestsAsync(int count = 10)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            // 1. جلب البيانات باستخدام Anonymous Type لضمان ترجمة SQL صحيحة
            var data = await context.Guests
                .AsNoTracking()
                .Where(g => g.IsActive)
                .Select(g => new
                {
                    g.GuestId,
                    g.FullName,
                    g.Organization,
                    // حساب العدد هنا كقيمة رقمية بسيطة
                    Count = g.EpisodeGuests.Count(eg => eg.IsActive)
                })
                // 2. الفرز والتقطيع يتمان في SQL بنجاح
                .OrderByDescending(x => x.Count)
                .Take(count)
                .ToListAsync();

            // 3. التحويل النهائي إلى الـ Record يتم في الذاكرة (Memory)
            return data.Select(x => new ActiveGuestDto(
                x.GuestId,
                x.FullName,
                x.Organization,
                x.Count
            )).ToList();
        }

        public async Task<Dictionary<string, int>> GetEpisodeStatusStatsAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var stats = await context.Episodes
                .Where(e => e.IsActive)
                .GroupBy(e => e.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            return new Dictionary<string, int> {
            { "Planned",   stats.FirstOrDefault(s => s.Status == 0)?.Count ?? 0 },
            { "Executed",  stats.FirstOrDefault(s => s.Status == 1)?.Count ?? 0 },
            { "Published", stats.FirstOrDefault(s => s.Status == 2)?.Count ?? 0 }
        };
        }
    }

}
