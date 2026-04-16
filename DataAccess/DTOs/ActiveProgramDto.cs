namespace DataAccess.DTOs
{
    public record ActiveProgramDto
    {
        public string ProgramName { get; init; }
        public string Category { get; init; }
        public int TotalEpisodes { get; init; }
        public int PublishedEpisodes { get; init; }
    }

}
