using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Data;

public partial class StaticDataIngest
{
    /// <summary>
    /// Ingest AreaGroup
    /// </summary>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    public async Task IngestAreaGroup(CancellationToken cancellationToken = default)
    {
        var orgEntityTypeId = (await db.EntityTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Name == "Organisasjon"))?.Id ?? throw new KeyNotFoundException(string.Format("EntityType not found '{0}'", "Organisasjon"));

        var data = new List<AreaGroup>()
        {
            new AreaGroup() { Id = Guid.Parse("7E2A3AF8-08CB-43A9-BDD7-7D5C7E377145"), Name = "Allment", Description = "Standard gruppe", EntityTypeId = orgEntityTypeId },
            new AreaGroup() { Id = Guid.Parse("3757643A-316D-4D0E-A52B-4DC7CDEBC0B4"), Name = "Bransje", Description = "For bransje grupper", EntityTypeId = orgEntityTypeId },
            new AreaGroup() { Id = Guid.Parse("554F0321-53B8-4D97-BE12-6A585C507159"), Name = "Særskilt", Description = "For de sære tingene", EntityTypeId = orgEntityTypeId }
        };

        var translations = new List<TranslationEntry>()
        {
            new TranslationEntry() { Id = Guid.Parse("7E2A3AF8-08CB-43A9-BDD7-7D5C7E377145"), LanguageCode = "eng", Type = nameof(ProviderType), FieldName = "Name", Value = "General" },
            new TranslationEntry() { Id = Guid.Parse("7E2A3AF8-08CB-43A9-BDD7-7D5C7E377145"), LanguageCode = "nno", Type = nameof(ProviderType), FieldName = "Name", Value = "Allment" },

            new TranslationEntry() { Id = Guid.Parse("3757643A-316D-4D0E-A52B-4DC7CDEBC0B4"), LanguageCode = "eng", Type = nameof(ProviderType), FieldName = "Name", Value = "Industry" },
            new TranslationEntry() { Id = Guid.Parse("3757643A-316D-4D0E-A52B-4DC7CDEBC0B4"), LanguageCode = "nno", Type = nameof(ProviderType), FieldName = "Name", Value = "Bransje" },

            new TranslationEntry() { Id = Guid.Parse("554F0321-53B8-4D97-BE12-6A585C507159"), LanguageCode = "eng", Type = nameof(ProviderType), FieldName = "Name", Value = "Special" },
            new TranslationEntry() { Id = Guid.Parse("554F0321-53B8-4D97-BE12-6A585C507159"), LanguageCode = "nno", Type = nameof(ProviderType), FieldName = "Name", Value = "Særskilt" },
        };

        foreach (var d in data)
        {
            var obj = db.AreaGroups.FirstOrDefault(t => t.Id == d.Id);
            if (obj == null)
            {
                db.AreaGroups.Add(d);
            }
            else
            {
                if (!obj.Name.Equals(d.Name) || !obj.Description.Equals(d.Description))
                {
                    obj.Name = d.Name;
                    obj.Description = d.Description;
                }
            }
        }

        foreach (var translation in translations)
        {
            await translationService.UpsertTranslationAsync(translation);
        }

        var result = await db.SaveChangesAsync(AuditValues, cancellationToken);
    }
}
