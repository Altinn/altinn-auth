using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class ResourceService : IResourceService
{
    public AppDbContextFactory DbContextFactory { get; }

    public IAuditAccessor AuditAccessor { get; }

    public ResourceService(AppDbContextFactory dbContextFactory, IAuditAccessor auditAccessor)
    {
        DbContextFactory = dbContextFactory;
        AuditAccessor = auditAccessor;
    }

    public async ValueTask<Resource> GetResource(Guid id, CancellationToken cancellationToken = default)
    {
        using var db = DbContextFactory.CreateDbContext();
        return await db.Resources.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, cancellationToken);
    }
}
