namespace Domain.Models;

public class SocialMediaPlatform : BaseEntity
{
    public int SocialMediaPlatformId { get; set; }
    
    public string Name { get; set; } = null!; // e.g. Facebook, Twitter, TikTok
    
    public string? Icon { get; set; } // e.g. fa-facebook, or image path

    public virtual ICollection<SocialMediaPublishingLogPlatform> PublishingLogPlatforms { get; set; } = new List<SocialMediaPublishingLogPlatform>();
}
