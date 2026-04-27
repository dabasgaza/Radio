namespace DataAccess.DTOs
{
    public record EpisodeGuestDto(
    int GuestId,
    string? Topic,
    TimeSpan? HostingTime          // ✅ بدل SortOrder
);

}
