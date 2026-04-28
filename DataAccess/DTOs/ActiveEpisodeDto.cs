using DataAccess.Services;

namespace DataAccess.DTOs
{
    public record ActiveEpisodeDto
    {
        public int EpisodeId { get; init; }
        public int ProgramId { get; init; } // 👈 ضروري للتعديل
        public int? GuestId { get; init; }   // 👈 ضروري للتعديل
        public string? EpisodeName { get; init; }
        public string? ProgramName { get; init; }
        public string? GuestsDisplay { get; init; } // 👈 إضافة اسم الضيف هنا
        public DateTime? ScheduledExecutionTime { get; init; }
        public string? StatusText { get; init; }
        public byte StatusId { get; init; } // نحتاج الـ ID هنا لاتخاذ القرار
        public string? SpecialNotes { get; init; }
        public bool IsWebsitePublished { get; init; }         // ✅ نشر الموقع


        // 👈 منطق تفعيل زر التنفيذ: فقط إذا كانت الحالة "مجدولة" (0)
        public bool CanMarkExecuted => StatusId == EpisodeStatus.Planned;

        // 👈 منطق تفعيل زر النشر: فقط إذا كانت الحالة "منفذة" (1)
        public bool CanMarkPublished => StatusId == EpisodeStatus.Executed;
        
        public bool CanToggleWebsitePublish => StatusId >= EpisodeStatus.Executed && StatusId <= EpisodeStatus.Published;

        public List<GuestDisplayItem> GuestItems { get; init; } = [];
    }

}
