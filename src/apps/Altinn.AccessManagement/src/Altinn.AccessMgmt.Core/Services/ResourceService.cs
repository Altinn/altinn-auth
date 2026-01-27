using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class ResourceService : IResourceService
{
    public AppDbContext Db { get; }

    public IAuditAccessor AuditAccessor { get; }

    public ResourceService(AppDbContext appDbContext, IAuditAccessor auditAccessor)
    {
        Db = appDbContext;
        AuditAccessor = auditAccessor;
    }

    public async ValueTask<Resource> GetResource(Guid id, CancellationToken cancellationToken = default)
    {
        return await Db.Resources.AsNoTracking().Include(t => t.Type).Include(t => t.Provider).SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async ValueTask<Resource> GetResource(string refId, CancellationToken cancellationToken = default)
    {
        return await Db.Resources.AsNoTracking().Include(t => t.Type).Include(t => t.Provider).SingleOrDefaultAsync(r => r.RefId == refId, cancellationToken);
    }
}
