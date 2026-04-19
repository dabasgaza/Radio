using System.Security.AccessControl;

namespace DataAccess.DTOs
{
    public record PublishingLogDto
    {
        public int EpisodeId { get; set; }
        public int PublishedByUserId { get; set; }
        public string? YouTubeUrl { get; set; } = null;
        public string? SoundCloudUrl { get; set; } = null;
        public string? FacebookUrl { get; set; } = null;
        public string? TwitterUrl { get; set; } = null;
        public DateTime PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
