using DataAccess.Common;
using Domain.Models;

namespace Radio.Tests.Common;

public class CollectionSyncHelperTests
{
    private sealed class TestEntity : BaseEntity
    {
        public int Id { get; set; }
        public int ForeignKey { get; set; }
        public string Name { get; set; } = "";
    }

    private sealed record TestDto(int Id, int Fk, string Name);

    [Fact]
    public void Sync_AddsNewEntity_WhenDtoHasZeroId()
    {
        var episode = new Episode { EpisodeId = 1 };
        var active = new List<TestEntity>();
        var deleted = new List<TestEntity>();
        var newItems = new List<TestDto> { new(0, 10, "New") };
        var added = new List<TestEntity>();

        CollectionSyncHelper.Sync(
            active, deleted, newItems, episode,
            e => e.Id, d => d.Id, e => e.ForeignKey, d => d.Fk,
            (e, d) => e.Name = d.Name,
            d => new TestEntity { Id = d.Id, ForeignKey = d.Fk, Name = d.Name },
            (ep, e) => added.Add(e));

        Assert.Single(added);
        Assert.Equal("New", added[0].Name);
        Assert.Equal(10, added[0].ForeignKey);
    }

    [Fact]
    public void Sync_UpdatesExistingEntity_WhenDtoIdMatches()
    {
        var episode = new Episode { EpisodeId = 1 };
        var existing = new TestEntity { Id = 5, ForeignKey = 10, Name = "Old" };
        var active = new List<TestEntity> { existing };
        var deleted = new List<TestEntity>();
        var newItems = new List<TestDto> { new(5, 10, "Updated") };

        CollectionSyncHelper.Sync(
            active, deleted, newItems, episode,
            e => e.Id, d => d.Id, e => e.ForeignKey, d => d.Fk,
            (e, d) => e.Name = d.Name,
            d => new TestEntity(),
            (_, _) => { });

        Assert.Equal("Updated", existing.Name);
    }

    [Fact]
    public void Sync_SoftDeletes_WhenDtoIdMissing()
    {
        var episode = new Episode { EpisodeId = 1 };
        var existing = new TestEntity { Id = 5, ForeignKey = 10, Name = "Remove" };
        var active = new List<TestEntity> { existing };
        var deleted = new List<TestEntity>();
        var newItems = new List<TestDto>();

        CollectionSyncHelper.Sync(
            active, deleted, newItems, episode,
            e => e.Id, d => d.Id, e => e.ForeignKey, d => d.Fk,
            (e, d) => e.Name = d.Name,
            d => new TestEntity(),
            (_, _) => { });

        Assert.False(existing.IsActive);
    }

    [Fact]
    public void Sync_RestoresSoftDeleted_WhenFkMatchesDeleted()
    {
        var episode = new Episode { EpisodeId = 1 };
        var softDeleted = new TestEntity { Id = 99, ForeignKey = 10, Name = "Restored", IsActive = false };
        var active = new List<TestEntity>();
        var deleted = new List<TestEntity> { softDeleted };
        var newItems = new List<TestDto> { new(0, 10, "Restored") };
        var added = new List<TestEntity>();

        CollectionSyncHelper.Sync(
            active, deleted, newItems, episode,
            e => e.Id, d => d.Id, e => e.ForeignKey, d => d.Fk,
            (e, d) => e.Name = d.Name,
            d => new TestEntity(),
            (_, e) => added.Add(e));

        Assert.True(softDeleted.IsActive);
        Assert.Empty(added);
    }
}
