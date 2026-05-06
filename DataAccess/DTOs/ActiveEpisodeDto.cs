using DataAccess.Services;

namespace DataAccess.DTOs
{
    public record ActiveEpisodeDto
    {
        public int EpisodeId { get; init; }
        public int ProgramId { get; init; }
        public string? EpisodeName { get; init; }
        public string? ProgramName { get; init; }
        public string? GuestsDisplay { get; init; }
        public string? EmployeesDisplay { get; init; }
        public DateTime? ScheduledExecutionTime { get; init; }
        public string? StatusText { get; init; }
        public byte StatusId { get; init; }
        public string? SpecialNotes { get; init; }

        public bool CanMarkExecuted => StatusId == EpisodeStatus.Planned;
        public bool CanMarkPublished => StatusId == EpisodeStatus.Executed;
        public bool CanToggleWebsitePublish => StatusId >= EpisodeStatus.Executed && StatusId != EpisodeStatus.Cancelled;
        public bool CanRevert => StatusId is EpisodeStatus.Executed or EpisodeStatus.Published or EpisodeStatus.WebsitePublished;
        public bool CanCancel => StatusId is EpisodeStatus.Planned or EpisodeStatus.Executed;

        /// <summary>
        /// هل يمكن عرض السجلات؟ — نعم إذا كانت الحلقة منفّذة أو منشورة (لديها سجلات)
        /// </summary>
        public bool CanViewRecords => StatusId >= EpisodeStatus.Executed && StatusId != EpisodeStatus.Cancelled;

        public List<GuestDisplayItem> GuestItems { get; init; } = [];
        public List<EpisodeCorrespondentDto> CorrespondentItems { get; init; } = [];
        public List<EpisodeEmployeeDto> EmployeeItems { get; init; } = [];
        public string? CancellationReason { get; set; }
    }

    /// <summary>مراسل مضاف لحلقة بكامل بياناته القابلة للتحرير</summary>
    public record EpisodeCorrespondentDto(int Id, int CorrespondentId, string FullName, string? Topic, TimeSpan? HostingTime);
    // بعد:
    /// <summary>موظف مضاف لحلقة بكامل بياناته (الاسم + الدور الوظيفي)</summary>
    public record EpisodeEmployeeDto(int Id, int EmployeeId, string FullName, string? StaffRoleName);
}
