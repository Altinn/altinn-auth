using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.Extensions.Configuration;

namespace Altinn.AccessMgmt.Core.Data;

/// <summary>
/// Ingest static data into the database
/// </summary>
/// <param name="db">AppDbContext</param>
/// <param name="translationService">TranslationService</param>
/// <param name="configuration">Configuration</param>
public partial class StaticDataIngest(AppDbContext db, ITranslationService translationService, IConfiguration configuration)
{
    //, IIngestService ingestService
    public AuditValues auditValues { get; set; } = new AuditValues(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid().ToString());

    public async Task IngestAll(CancellationToken cancellationToken = default)
    {
        await IngestProviderType(cancellationToken);
        await IngestProvider(cancellationToken);
        await IngestEntityType(cancellationToken);
        await IngestEntityVariant(cancellationToken);
        await IngestSystemEntity(cancellationToken);
        await IngestAreaGroup(cancellationToken);
        await IngestRole(cancellationToken);
        await IngestRoleLookup(cancellationToken);
        await IngestRolePackage(cancellationToken);
        await IngestEntityVariantRole(cancellationToken);
    }
}
