using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
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
        var data = new List<EntityType>()
        {
            EntityTypeConstants.Organisation,
            EntityTypeConstants.Person,
            EntityTypeConstants.SystemUser,
            EntityTypeConstants.Internal,
        };

        var translations = TranslationEntry.Create(
            EntityTypeConstants.Organisation,
            EntityTypeConstants.Person,
            EntityTypeConstants.SystemUser,
            EntityTypeConstants.Internal
        );

        foreach (var d in data)
        {
            var obj = db.EntityTypes.FirstOrDefault(t => t.Id == d.Id);
            if (obj == null)
            {
                db.EntityTypes.Add(d);
            }
            else
            {
                obj.Name = d.Name;
            }
        }

        foreach (var translation in translations)
        {
            await translationService.UpsertTranslationAsync(translation);
        }

        var result = await db.SaveChangesAsync(AuditValues, cancellationToken);
    }
}
