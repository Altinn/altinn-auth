using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Utils.Models;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Data;

public partial class StaticDataIngest
{
    /// <summary>
    /// Ingest Entity
    /// </summary>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    public async Task IngestSystemEntity(CancellationToken cancellationToken = default)
    {
        var internalTypeId = (await db.EntityTypes.AsNoTracking().SingleOrDefaultAsync(t => t.Name == "Intern"))?.Id ?? throw new KeyNotFoundException(string.Format("EntityType '{0}' not found", "Intern"));
        var internalVariantId = (await db.EntityVariants.AsNoTracking().SingleOrDefaultAsync(t => t.TypeId == internalTypeId && t.Name == "Standard"))?.Id ?? throw new KeyNotFoundException(string.Format("EntityVariant '{0}' not found", "Intern"));

        var data = new List<Entity>()
        {
            new Entity() { Id = AuditDefaults.StaticDataIngest, Name = "StaticDataIngest", RefId = "sys-static-data-ingest", ParentId = null, TypeId = internalTypeId, VariantId = internalVariantId },
            new Entity() { Id = AuditDefaults.RegisterImportSystem, Name = "RegisterImportSystem", RefId = "sys-register-import-system", ParentId = null, TypeId = internalTypeId, VariantId = internalVariantId },
            new Entity() { Id = AuditDefaults.ResourceRegistryImportSystem, Name = nameof(AuditDefaults.ResourceRegistryImportSystem), RefId = "sys-resource-register-import-system", ParentId = null, TypeId = internalTypeId, VariantId = internalVariantId },
            new Entity() { Id = AuditDefaults.EnduserApi, Name = "EnduserApi", RefId = "accessmgmt-enduser-api", ParentId = null, TypeId = internalTypeId, VariantId = internalVariantId },
            new Entity() { Id = AuditDefaults.InternalApi, Name = "InternalApi", RefId = "accessmgmt-internal-api", ParentId = null, TypeId = internalTypeId, VariantId = internalVariantId },
            new Entity() { Id = AuditDefaults.InternalApiImportSystem, Name = nameof(AuditDefaults.InternalApiImportSystem), RefId = "sys-internal-api-import-system", ParentId = null, TypeId = internalTypeId, VariantId = internalVariantId },
        };

        db.Database.SetAuditSession(AuditValues);

        foreach (var d in data)
        {
            var obj = db.Entities.FirstOrDefault(t => t.Id == d.Id);
            if (obj == null)
            {
                db.Entities.Add(d);
            }
            else
            {
                obj.Name = d.Name;
            }
        }

        var result = await db.SaveChangesAsync();
    }
}
