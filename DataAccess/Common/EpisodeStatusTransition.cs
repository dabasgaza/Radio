using DataAccess.Services;

namespace DataAccess.Common;

public static class EpisodeStatusTransition
{
    private static readonly Dictionary<byte, HashSet<byte>> s_validTransitions = new()
    {
        [EpisodeStatus.Planned] = [EpisodeStatus.Executed, EpisodeStatus.Cancelled],
        [EpisodeStatus.Executed] = [EpisodeStatus.Published, EpisodeStatus.Cancelled],
        [EpisodeStatus.Published] = [EpisodeStatus.WebsitePublished, EpisodeStatus.Cancelled],
        [EpisodeStatus.WebsitePublished] = [EpisodeStatus.Cancelled],
        [EpisodeStatus.Cancelled] = []
    };

    public static bool IsValid(byte fromStatus, byte toStatus)
    {
        if (fromStatus == toStatus) return false;
        return s_validTransitions.TryGetValue(fromStatus, out var allowed) && allowed.Contains(toStatus);
    }

    public static IReadOnlySet<byte> GetAllowedTransitions(byte fromStatus)
    {
        return s_validTransitions.TryGetValue(fromStatus, out var allowed)
            ? allowed
            : [];
    }
}
