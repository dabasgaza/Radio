namespace DataAccess.DTOs;

public record WebsitePublishingLogDto(
    int Id,
    int EpisodeId,
    string? MediaType,
    string? Title,
    string? Notes,
    DateTime PublishedAt);