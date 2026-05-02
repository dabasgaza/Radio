namespace Domain.Models;

public class SocialMediaPublishingLogPlatform : BaseEntity
{
    public int SocialMediaPublishingLogPlatformId { get; set; }
    
    public int SocialMediaPublishingLogId { get; set; }
    public int SocialMediaPlatformId { get; set; }
    
    public string? Url { get; set; }

    public virtual SocialMediaPublishingLog SocialMediaPublishingLog { get; set; } = null!;
    public virtual SocialMediaPlatform SocialMediaPlatform { get; set; } = null!;
}
