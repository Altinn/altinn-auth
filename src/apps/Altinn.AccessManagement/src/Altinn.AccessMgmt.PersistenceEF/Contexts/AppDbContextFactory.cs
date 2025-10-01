using Altinn.AccessMgmt.PersistenceEF.Audit;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Contexts;

/// <inheritdoc />
public class AppDbContextFactory(IDbContextFactory<AppDbContext> factory, IAuditAccessor audit) : IDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext()
    {
        var dbContext = factory.CreateDbContext();
        dbContext.AuditAccessor = audit;
        return dbContext;
    }
}
