namespace DataAccess.DTOs;

public record SocialMediaPublishingLogDto(
    int LogId,
    int EpisodeGuestId,
    string? ClipTitle,
    TimeSpan? Duration,
    List<PlatformPublishDto> Platforms);