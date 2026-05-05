using Domain.Models;

namespace DataAccess.DTOs;

public record SocialMediaPublishingLogDto(
    int LogId,
    int EpisodeGuestId,
    string? ClipTitle,
    TimeSpan? Duration,
    MediaType MediaType,
    List<PlatformPublishDto> Platforms);
