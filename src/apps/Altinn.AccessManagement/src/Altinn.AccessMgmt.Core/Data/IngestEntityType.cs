using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Data;

public partial class StaticDataIngest
{
    /// <summary>
    /// Ingest EntityType
    /// </summary>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    public async Task IngestEntityType(CancellationToken cancellationToken = default)
    {
        var provider = await db.Providers.AsNoTracking().SingleOrDefaultAsync(t => t.Code == "sys-altinn3", cancellationToken) ?? throw new KeyNotFoundException("Altinn3 provider not found");

        var data = new List<EntityType>()
        {
            new EntityType() { Id = Guid.Parse("8C216E2F-AFDD-4234-9BA2-691C727BB33D"), Name = "Organisasjon", ProviderId = provider.Id },
            new EntityType() { Id = Guid.Parse("BFE09E70-E868-44B3-8D81-DFE0E13E058A"), Name = "Person", ProviderId = provider.Id },
            new EntityType() { Id = Guid.Parse("FE643898-2F47-4080-85E3-86BF6FE39630"), Name = "Systembruker", ProviderId = provider.Id },
            new EntityType() { Id = Guid.Parse("4557CC81-C10D-40B4-8134-F8825060016E"), Name = "Intern", ProviderId = provider.Id },
        };

        var translations = new List<TranslationEntry>()
        {
            new TranslationEntry() { Id = Guid.Parse("8C216E2F-AFDD-4234-9BA2-691C727BB33D"), LanguageCode = "eng", Type = nameof(EntityType), FieldName = "Name", Value = "Organization" },
            new TranslationEntry() { Id = Guid.Parse("8C216E2F-AFDD-4234-9BA2-691C727BB33D"), LanguageCode = "nno", Type = nameof(EntityType), FieldName = "Name", Value = "Organisasjon" },

            new TranslationEntry() { Id = Guid.Parse("BFE09E70-E868-44B3-8D81-DFE0E13E058A"), LanguageCode = "eng", Type = nameof(EntityType), FieldName = "Name", Value = "Person" },
            new TranslationEntry() { Id = Guid.Parse("BFE09E70-E868-44B3-8D81-DFE0E13E058A"), LanguageCode = "nno", Type = nameof(EntityType), FieldName = "Name", Value = "Person" },

            new TranslationEntry() { Id = Guid.Parse("FE643898-2F47-4080-85E3-86BF6FE39630"), LanguageCode = "eng", Type = nameof(EntityType), FieldName = "Name", Value = "SystemUser" },
            new TranslationEntry() { Id = Guid.Parse("FE643898-2F47-4080-85E3-86BF6FE39630"), LanguageCode = "nno", Type = nameof(EntityType), FieldName = "Name", Value = "Systembrukar" },

            new TranslationEntry() { Id = Guid.Parse("4557CC81-C10D-40B4-8134-F8825060016E"), LanguageCode = "eng", Type = nameof(EntityType), FieldName = "Name", Value = "Internal" },
            new TranslationEntry() { Id = Guid.Parse("4557CC81-C10D-40B4-8134-F8825060016E"), LanguageCode = "nno", Type = nameof(EntityType), FieldName = "Name", Value = "Intern" },
        };

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
