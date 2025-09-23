using Altinn.AccessMgmt.PersistenceEF.Constants;

namespace Altinn.AccessMgmt.PersistenceEF.Data;

public partial class StaticDataIngest
{
    /// <summary>
    /// Ingest EntityVariant
    /// </summary>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    public async Task IngestEntityVariant(CancellationToken cancellationToken = default)
    {
        foreach (var value in EntityVariantConstants.AllEntities())
        {
            var obj = db.EntityVariants.FirstOrDefault(t => t.Id == value.Id);
            if (obj == null)
            {
                db.EntityVariants.Add(value);
            }
            else
            {
                obj.Name = value.Entity.Name;
            }
        }

        foreach (var translation in EntityVariantConstants.AllTranslations())
        {
            await translationService.UpsertTranslationAsync(translation);
        }

        var result = await db.SaveChangesAsync(AuditValues, cancellationToken);
    }
}
