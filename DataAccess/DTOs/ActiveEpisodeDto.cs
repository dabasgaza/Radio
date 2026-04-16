using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.DTOs
{
    public record ActiveEpisodeDto
    {
        public int EpisodeId { get; init; }
        public int ProgramId { get; init; } // 👈 ضروري للتعديل
        public int? GuestId { get; init; }   // 👈 ضروري للتعديل
        public string EpisodeName { get; init; }
        public string ProgramName { get; init; }
        public string GuestName { get; init; } // 👈 إضافة اسم الضيف هنا
        public DateTime? ScheduledExecutionTime { get; init; }
        public string StatusText { get; init; }
        public byte StatusId { get; init; } // نحتاج الـ ID هنا لاتخاذ القرار
        public string SpecialNotes { get; init; }

        // 👈 منطق تفعيل زر التنفيذ: فقط إذا كانت الحالة "مجدولة" (0)
        public bool CanMarkExecuted => StatusId == 0 ;

        // 👈 منطق تفعيل زر النشر: فقط إذا كانت الحالة "منفذة" (1)
        public bool CanMarkPublished => StatusId == 1 ;
    }

}
