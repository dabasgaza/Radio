using System;

namespace Domain.Models;

public class WebsitePublishingLog : BaseEntity
{
    public int WebsitePublishingLogId { get; set; }

    public int EpisodeId { get; set; }
    public int PublishedByUserId { get; set; }

    public MediaType MediaType { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
    
    public DateTime PublishedAt { get; set; }

    public virtual Episode Episode { get; set; } = null!;
    public virtual User PublishedByUser { get; set; } = null!;
}
