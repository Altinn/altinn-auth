using Altinn.AccessMgmt.PersistenceEF.Constants;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Data;

public partial class StaticDataIngest
{
    /// <summary>
    /// Ingest EntityType
    /// </summary>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    public async Task IngestEntityType(CancellationToken cancellationToken = default)
    {
        var entityTypes = await db.EntityTypes
            .AsTracking()
            .ToDictionaryAsync(e => e.Id, cancellationToken);
        
        foreach (var seed in EntityTypeConstants.AllEntities())
        {
            if (entityTypes.TryGetValue(seed, out var entity))
            {
                entity.Name = seed.Entity.Name;
            }
            else
            {
                db.EntityTypes.Add(seed);
            }
        }

        foreach (var translation in EntityTypeConstants.AllTranslations())
        {
            await translationService.UpsertTranslationAsync(translation);
        }

        var result = await db.SaveChangesAsync(AuditValues, cancellationToken);
    }
}
