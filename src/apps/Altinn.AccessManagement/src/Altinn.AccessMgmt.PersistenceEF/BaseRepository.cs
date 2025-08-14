using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF;

public class BaseRepository<TBasic, TExtended, TAudit>(AppDbContext basicDb)
    where TBasic : class
    where TExtended : class
    where TAudit : class
{
    private readonly AppDbContext basicDb = basicDb;

    public virtual async ValueTask<TBasic> Get(Guid id) =>
        await basicDb.Set<TBasic>().SingleOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
    /*
    //public virtual async ValueTask<TExtended> GetExtended(Guid id) =>
    //    await extendedDb.Set<TExtended>().SingleOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);

    //public virtual async ValueTask<TAudit> GetAudit(Guid id) =>
    //   await auditDb.Set<TAudit>().SingleOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
    
    //public virtual async ValueTask<TAudit> GetAudit(Guid id, DateTime asOf) =>
    //    await auditDb.Set<TAudit>().SingleOrDefaultAsync(e =>
    //    EF.Property<Guid>(e, "Id") == id &&
    //    EF.Property<DateTime>(e, "ValidFrom") <= asOf &&
    //    EF.Property<DateTime>(e, "ValidTo") > asOf
    //    );

    //public virtual async ValueTask<TAudit> CreateAudit(TAudit obj)
    //{
    //    await auditDb.AddAsync(obj);

    //    using (ReadOnlyWriteOverride.Enable())
    //    {
    //        await auditDb.SaveChangesAsync();
    //    }

    //    return obj;
    //}
    */

    public virtual async ValueTask<TBasic> Create(TBasic obj)
    {
        await basicDb.AddAsync(obj);
        await basicDb.SaveChangesAsync();
        return obj;
    }

    public virtual async Task Create(TBasic obj, AuditValues audit)
    {
        basicDb.Database.SetAuditSession(audit);
        await basicDb.AddAsync(obj);
        await basicDb.SaveChangesAsync();
    }

    public virtual async Task Update(TBasic obj)
    {
        basicDb.Update(obj);
        await basicDb.SaveChangesAsync();
    }

    public virtual async Task Delete(Guid id)
    {
        var obj = basicDb.Set<TBasic>().FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
        basicDb.Remove(obj);
        await basicDb.SaveChangesAsync();
    }
}
