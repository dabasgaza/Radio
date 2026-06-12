using DataAccess.Common;
using DataAccess.Services;

namespace Radio.Tests.Common;

public class EpisodeStatusTransitionTests
{
    [Theory]
    [InlineData(EpisodeStatus.Planned, EpisodeStatus.Executed, true)]
    [InlineData(EpisodeStatus.Planned, EpisodeStatus.Cancelled, true)]
    [InlineData(EpisodeStatus.Executed, EpisodeStatus.Published, true)]
    [InlineData(EpisodeStatus.Executed, EpisodeStatus.Cancelled, true)]
    [InlineData(EpisodeStatus.Published, EpisodeStatus.WebsitePublished, true)]
    [InlineData(EpisodeStatus.Published, EpisodeStatus.Cancelled, true)]
    [InlineData(EpisodeStatus.WebsitePublished, EpisodeStatus.Cancelled, true)]
    [InlineData(EpisodeStatus.Planned, EpisodeStatus.Published, false)]
    [InlineData(EpisodeStatus.Planned, EpisodeStatus.WebsitePublished, false)]
    [InlineData(EpisodeStatus.Executed, EpisodeStatus.WebsitePublished, false)]
    [InlineData(EpisodeStatus.Cancelled, EpisodeStatus.Planned, false)]
    [InlineData(EpisodeStatus.Cancelled, EpisodeStatus.Executed, false)]
    [InlineData(EpisodeStatus.Cancelled, EpisodeStatus.Published, false)]
    [InlineData(EpisodeStatus.Cancelled, EpisodeStatus.WebsitePublished, false)]
    [InlineData(EpisodeStatus.Planned, EpisodeStatus.Planned, false)]
    [InlineData(EpisodeStatus.Executed, EpisodeStatus.Executed, false)]
    public void IsValid_ReturnsExpected(byte from, byte to, bool expected)
    {
        Assert.Equal(expected, EpisodeStatusTransition.IsValid(from, to));
    }

    [Fact]
    public void GetAllowedTransitions_FromPlanned_ReturnsExecutedAndCancelled()
    {
        var allowed = EpisodeStatusTransition.GetAllowedTransitions(EpisodeStatus.Planned);
        Assert.Equal(2, allowed.Count);
        Assert.Contains(EpisodeStatus.Executed, allowed);
        Assert.Contains(EpisodeStatus.Cancelled, allowed);
    }

    [Fact]
    public void GetAllowedTransitions_FromExecuted_ReturnsPublishedAndCancelled()
    {
        var allowed = EpisodeStatusTransition.GetAllowedTransitions(EpisodeStatus.Executed);
        Assert.Equal(2, allowed.Count);
        Assert.Contains(EpisodeStatus.Published, allowed);
        Assert.Contains(EpisodeStatus.Cancelled, allowed);
    }

    [Fact]
    public void GetAllowedTransitions_FromCancelled_ReturnsEmpty()
    {
        var allowed = EpisodeStatusTransition.GetAllowedTransitions(EpisodeStatus.Cancelled);
        Assert.Empty(allowed);
    }

    [Fact]
    public void GetAllowedTransitions_FromUnknown_ReturnsEmpty()
    {
        var allowed = EpisodeStatusTransition.GetAllowedTransitions(99);
        Assert.Empty(allowed);
    }

    [Fact]
    public void IsValid_SameStatus_ReturnsFalse()
    {
        Assert.False(EpisodeStatusTransition.IsValid(EpisodeStatus.Planned, EpisodeStatus.Planned));
        Assert.False(EpisodeStatusTransition.IsValid(EpisodeStatus.Cancelled, EpisodeStatus.Cancelled));
    }
}
