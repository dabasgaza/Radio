using System;
using System.Collections.Generic;

namespace Domain.Models;

public class SocialMediaPublishingLog : BaseEntity
{
    public int SocialMediaPublishingLogId { get; set; }

    public int EpisodeGuestId { get; set; }
    public int PublishedByUserId { get; set; }

    public MediaType MediaType { get; set; }
    public TimeSpan? ClipDuration { get; set; }
    public string? ClipTitle { get; set; }
    public string? Notes { get; set; }
    
    public DateTime PublishedAt { get; set; }

    public virtual EpisodeGuest EpisodeGuest { get; set; } = null!;
    public virtual User PublishedByUser { get; set; } = null!;

    public virtual ICollection<SocialMediaPublishingLogPlatform> Platforms { get; set; } = new List<SocialMediaPublishingLogPlatform>();
}
