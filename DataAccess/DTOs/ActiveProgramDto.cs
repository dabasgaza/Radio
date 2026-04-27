namespace DataAccess.DTOs
{
    public record ActiveProgramDto
    {
        public string ProgramName { get; init; }
        public string Category { get; init; }
        public int TotalEpisodes { get; init; }
        public int PublishedEpisodes { get; init; }
        public string GuestDisplay { get; init; }
        public DateTime? ScheduledExecutionTime { get; init; }
        public string? StatusText { get; init; }
        public string? SpecialNotes { get; init; }
    }

}
