using System;

namespace Domain.Models;

public class EpisodeCorrespondent : BaseEntity
{
    public int EpisodeCorrespondentId { get; set; }

    public int EpisodeId { get; set; }
    public int CorrespondentId { get; set; }

    public string? Topic { get; set; }
    public TimeSpan? HostingTime { get; set; }

    public virtual Episode Episode { get; set; } = null!;
    public virtual Correspondent Correspondent { get; set; } = null!;
}
