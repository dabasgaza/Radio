namespace DataAccess.DTOs
{
    public record ActiveProgramDto
    {
        public string ProgramName { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public int TotalEpisodes { get; init; }
        public int PublishedEpisodes { get; init; }
        public string GuestDisplay { get; init; } = string.Empty;
        public DateTime? ScheduledExecutionTime { get; init; }
        public string? StatusText { get; init; }
        public string? SpecialNotes { get; init; }
    }

}
