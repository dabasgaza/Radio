using Domain.Models;

namespace DataAccess.Common;

public static class CollectionSyncHelper
{
    public static void Sync<TEntity, TDto>(
        List<TEntity> activeEntities,
        List<TEntity> allIncludingDeleted,
        List<TDto> newItems,
        Episode parentEpisode,
        Func<TEntity, int> entityIdSelector,
        Func<TDto, int> dtoIdSelector,
        Func<TEntity, int> entityFkSelector,
        Func<TDto, int> dtoFkSelector,
        Action<TEntity, TDto> updater,
        Func<TDto, TEntity> factory,
        Action<Episode, TEntity> addToParent)
        where TEntity : BaseEntity
    {
        var existingById = activeEntities.ToDictionary(entityIdSelector);
        var newIds = newItems.Where(i => dtoIdSelector(i) != 0).Select(dtoIdSelector).ToHashSet();

        foreach (var entity in activeEntities.Where(e => !newIds.Contains(entityIdSelector(e))))
            entity.IsActive = false;

        foreach (var dto in newItems)
        {
            var dtoId = dtoIdSelector(dto);
            if (dtoId != 0 && existingById.TryGetValue(dtoId, out var existing))
            {
                updater(existing, dto);
            }
            else
            {
                var fk = dtoFkSelector(dto);
                var softDeleted = allIncludingDeleted.FirstOrDefault(e => entityFkSelector(e) == fk && !e.IsActive);
                if (softDeleted != null)
                {
                    softDeleted.IsActive = true;
                    updater(softDeleted, dto);
                }
                else
                {
                    addToParent(parentEpisode, factory(dto));
                }
            }
        }
    }
}
