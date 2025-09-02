using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;

namespace Altinn.AccessMgmt.Core.Data;

public partial class StaticDataIngest
{
    /// <summary>
    /// Ingest ProviderTypes
    /// </summary>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    public async Task IngestProviderType(CancellationToken cancellationToken = default)
    {
        var data = new List<ProviderType>()
        {
            new ProviderType() { Id = Guid.Parse("0195efb8-7c80-7bb5-a35c-11d58ea36695"), Name = "System" },
            new ProviderType() { Id = Guid.Parse("0195efb8-7c80-713e-ad96-a9896d12f444"), Name = "Tjenesteeier" }
        };

        var translations = new List<TranslationEntry>()
        {
            new TranslationEntry() { Id = Guid.Parse("0195efb8-7c80-7bb5-a35c-11d58ea36695"), LanguageCode = "eng", Type = nameof(ProviderType), FieldName = "Name", Value = "System" },
            new TranslationEntry() { Id = Guid.Parse("0195efb8-7c80-7bb5-a35c-11d58ea36695"), LanguageCode = "nno", Type = nameof(ProviderType), FieldName = "Name", Value = "ServiceOwner" },

            new TranslationEntry() { Id = Guid.Parse("0195efb8-7c80-713e-ad96-a9896d12f444"), LanguageCode = "eng", Type = nameof(ProviderType), FieldName = "Name", Value = "System" },
            new TranslationEntry() { Id = Guid.Parse("0195efb8-7c80-713e-ad96-a9896d12f444"), LanguageCode = "nno", Type = nameof(ProviderType), FieldName = "Name", Value = "Tenesteeigar" },
        };

        db.Database.SetAuditSession(AuditValues);

        foreach (var d in data)
        {
            var obj = db.ProviderTypes.FirstOrDefault(t => t.Id == d.Id);
            if (obj == null)
            {
                db.ProviderTypes.Add(d);
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

        var result = await db.SaveChangesAsync();
    }
}
