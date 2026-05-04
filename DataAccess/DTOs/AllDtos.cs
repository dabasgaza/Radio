namespace DataAccess.DTOs
{
    public record GuestDto(int GuestId, string FullName, string? Organization, string? PhoneNumber, string? EmailAddress, string? Bio, string? Gender);

    /// <summary>
    /// يمثل ضيفاً مضافاً لحلقة معينة بكامل بياناته القابلة للتحرير
    /// </summary>
    public record EpisodeGuestDto(
        int EpisodeGuestId,
        int GuestId,
        string FullName,
        string? Topic,
        TimeSpan? HostingTime,
        string? ClipNotes);
    public record ProgramDto(int ProgramId, string ProgramName, string? Category, string? ProgramDescription);
    public record EpisodeDto(
        int EpisodeId,
        int ProgramId,
        List<EpisodeGuestDto> Guests,
        List<EpisodeCorrespondentDto> Correspondents,
        List<EpisodeEmployeeDto> Employees,
        string EpisodeName,
        DateTime? ScheduledDate,     // التاريخ فقط من DatePicker
        TimeSpan? BroadcastTime,     // الوقت فقط من TimePicker
        string? SpecialNotes)
    {
        /// <summary>يدمج التاريخ والوقت في قيمة DateTime واحدة لحفظها في قاعدة البيانات</summary>
        public DateTime? ScheduledDateTime =>
            ScheduledDate.HasValue
                ? ScheduledDate.Value.Date + (BroadcastTime ?? TimeSpan.Zero)
                : null;
    };
    public record CorrespondentDto(int CorrespondentId, string FullName, string? PhoneNumber, string? AssignedLocations);
    public record TodayEpisodeDto(
        int EpisodeId, string EpisodeName, string ProgramName,
        string GuestsDisplay,                                   // ✅ بدل string GuestName
        DateTime? ScheduledExecutionTime, string StatusText);
    public record ActiveGuestDto(int GuestId, string FullName, string? Organization, int EpisodeCount);

    public record GuestDisplayItem(string Name, string? Topic, TimeSpan? HostingTime);

    /// <summary>
    /// نتيجة تقرير الحلقات بفلتر التاريخ
    /// </summary>
    public record DateRangeEpisodeDto(
        int EpisodeId,
        string EpisodeName,
        string ProgramName,
        string GuestsDisplay,
        DateTime? ScheduledExecutionTime,
        string StatusText);

    /// <summary>
    /// تقرير الضيوف الأكثر ظهوراً
    /// </summary>
    public record TopGuestDto(
        int Rank,
        int GuestId,
        string FullName,
        string? Organization,
        int AppearanceCount,
        string? LastTopic,
        DateTime? LastAppearance);

    /// <summary>
    /// تقرير الحلقات الملغاة مع الأسباب
    /// </summary>
    public record CancelledEpisodeDto(
        int EpisodeId,
        string EpisodeName,
        string ProgramName,
        DateTime? ScheduledExecutionTime,
        string CancellationReason,
        string? CancelledBy,
        DateTime CancelledAt);

}
