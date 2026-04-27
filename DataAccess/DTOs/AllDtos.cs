namespace DataAccess.DTOs
{
    public record GuestDto(int GuestId, string FullName, string? Organization, string? PhoneNumber, string? EmailAddress, string? Bio, string? Gender);
    public record ProgramDto(int ProgramId, string ProgramName, string? Category, string? ProgramDescription);
    public record EpisodeDto(int EpisodeId, int ProgramId, List<EpisodeGuestDto> Guests   // ← بدل int? GuestId
, string EpisodeName, DateTime? ScheduledTime, string? SpecialNotes);
    public record CorrespondentDto(int CorrespondentId, string FullName, string? PhoneNumber, string? AssignedLocations);
    public record TodayEpisodeDto(
        int EpisodeId, string EpisodeName, string ProgramName,
        string GuestsDisplay,                                   // ✅ بدل string GuestName
        DateTime? ScheduledExecutionTime, string StatusText);
    public record ActiveGuestDto(int GuestId, string FullName, string? Organization, int EpisodeCount);
}
