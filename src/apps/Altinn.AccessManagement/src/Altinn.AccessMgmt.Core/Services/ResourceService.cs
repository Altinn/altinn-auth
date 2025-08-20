using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class ResourceService(AppDbContext db, AuditValues auditValues) : IResourceService
{
    public async ValueTask<Resource> GetResource(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Resources.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
    }
}
