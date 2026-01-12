using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services;

/// <inheritdoc/>
public class ProviderService : IProviderService
{
    public AppDbContext Db { get; }

    public IAuditAccessor AuditAccessor { get; }

    public ProviderService(AppDbContext appDbContext, IAuditAccessor auditAccessor)
    {
        Db = appDbContext;
        AuditAccessor = auditAccessor;
    }

    /// <inheritdoc/>
    public async ValueTask<Provider> GetProviderByOrganizationId(string organizationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
        {
            throw new ArgumentException("Organization ID cannot be null or empty.", nameof(organizationId));
        }
        
        return await Db.Providers.AsNoTracking().SingleOrDefaultAsync(p => p.RefId == organizationId, cancellationToken);
    }

    /// <inheritdoc/>
    public async ValueTask<Provider> GetProviderByCode(string providerCode, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(providerCode))
        {
            throw new ArgumentException("Provider code cannot be null or empty.", nameof(providerCode));
        }

        return await Db.Providers.AsNoTracking().SingleOrDefaultAsync(p => p.Code == providerCode, cancellationToken);
    }
}
