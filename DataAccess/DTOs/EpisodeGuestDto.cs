namespace DataAccess.DTOs
{
    public record EpisodeGuestDto(
        int Id,
        int GuestId,
        string? Topic,
        TimeSpan? HostingTime          // ✅ بدل SortOrder
);

}
