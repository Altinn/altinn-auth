using Altinn.AccessMgmt.FFB.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.FFB.Services.PageData;

/// <summary>
/// Runs ConnectionQuery against an environment and loads the role list
/// for the connection query page.
/// </summary>
public sealed class ConnectionQueryService(IEnvironmentDbContextFactory dbFactory, ILogger<ConnectionQuery> logger)
{
    public async Task<List<Role>> GetRolesAsync(string environment, CancellationToken ct = default)
    {
        using var db = dbFactory.CreateContext(environment);
        return await db.Roles
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(ct);
    }

    public async Task<List<ConnectionQueryExtendedRecord>> QueryAsync(
        string environment,
        ConnectionQueryFilter filter,
        ConnectionQueryDirection direction,
        CancellationToken ct = default)
    {
        using var db = dbFactory.CreateContext(environment);
        var query = new ConnectionQuery(db, logger);

        return direction == ConnectionQueryDirection.FromOthers
            ? await query.GetConnectionsFromOthersAsync(filter, ct: ct)
            : await query.GetConnectionsToOthersAsync(filter, ct: ct);
    }
}
