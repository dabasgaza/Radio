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


        // 👈 منطق تفعيل زر التنفيذ: فقط إذا كانت الحالة "مجدولة" (0)
        public bool CanMarkExecuted => StatusId == EpisodeStatus.Planned;

        // 👈 منطق تفعيل زر النشر: فقط إذا كانت الحالة "منفذة" (1)
        public bool CanMarkPublished => StatusId == EpisodeStatus.Executed;
        
        // 👈 منطق تفعيل زر نشر الموقع: فقط إذا كانت الحالة "منفذة" (1) أو "منشورة" (2) أو "منشورة على الموقع" (3)
        public bool CanToggleWebsitePublish => StatusId >= EpisodeStatus.Executed && StatusId != EpisodeStatus.Cancelled;

        // 👈 منطق تفعيل زر التراجع: منفّذة، منشورة، أو منشورة على الموقع
        public bool CanRevert => StatusId is EpisodeStatus.Executed or EpisodeStatus.Published or EpisodeStatus.WebsitePublished;

        // 👈 منطق تفعيل زر الإلغاء: مجدولة أو منفّذة فقط (لا يمكن إلغاء منشورة)
        public bool CanCancel => StatusId is EpisodeStatus.Planned or EpisodeStatus.Executed;

        public List<GuestDisplayItem> GuestItems { get; init; } = [];
        public string? CancellationReason { get; set; }
    }

}
