using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Data;

public partial class StaticDataIngest
{
    /// <summary>
    /// Ingest ProviderTypes
    /// </summary>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    public async Task IngestProvider(CancellationToken cancellationToken = default)
    {
        var type = await db.ProviderTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Name == "System", cancellationToken) ?? throw new Exception("Providertype 'System' not found.");

        var data = new List<Provider>()
        {
            new Provider() { Id = Guid.Parse("0195ea92-2080-777d-8626-69c91ea2a05d"), Name = "Altinn 2", Code = "sys-altinn2", TypeId = type.Id, RefId = string.Empty },
            new Provider() { Id = Guid.Parse("0195ea92-2080-7e7c-bbe3-bb0521c1e51a"), Name = "Altinn 3", Code = "sys-altinn3", TypeId = type.Id, RefId = string.Empty },
            new Provider() { Id = Guid.Parse("0195ea92-2080-79d8-9859-0b26375f145e"), Name = "Ressursregisteret", Code = "sys-resreg", TypeId = type.Id, RefId = string.Empty },
            new Provider() { Id = Guid.Parse("0195ea92-2080-758b-89db-7735c4f68320"), Name = "Enhetsregisteret", Code = "sys-ccr", TypeId = type.Id, RefId = string.Empty }
        };

        db.Database.SetAuditSession(AuditValues);

        foreach (var d in data)
        {
            // Verify: Compare on Id or Code?
            var obj = db.Providers.FirstOrDefault(t => t.Id == d.Id);
            if (obj == null)
            {
                db.Providers.Add(d);
            }
            else
            {
                obj.Name = d.Name;
            }
        }
        
        await db.SaveChangesAsync();
    }
}
