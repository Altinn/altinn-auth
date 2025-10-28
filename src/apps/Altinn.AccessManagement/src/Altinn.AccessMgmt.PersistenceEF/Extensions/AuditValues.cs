using System.Data.Common;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

public record AuditValues(Guid ChangedBy, Guid ChangedBySystem, string OperationId = null, DateTimeOffset? ValidFrom = null)
{
    public AuditValues(Guid changedBy, Guid changedBySystem)
        : this(changedBy, changedBySystem, Activity.Current?.TraceId.ToString() ?? Guid.CreateVersion7().ToString()) { }
    
    public AuditValues(Guid changedBy) 
        : this(changedBy, changedBy, Activity.Current?.TraceId.ToString() ?? Guid.CreateVersion7().ToString()) { }
}

public interface IAuditContextProvider
{
    AuditValues Current { get; }
}

//public static class DatabaseFacadeExtensions
//{
//    public static void SetAuditSession(this DatabaseFacade db, AuditValues audit)
//    {
//        var sql = """
//            CREATE TEMP TABLE IF NOT EXISTS session_audit_context (
//                changed_by UUID,
//                changed_by_system UUID,
//                change_operation_id TEXT
//            ) ON COMMIT DROP;
//            TRUNCATE session_audit_context;
//            INSERT INTO session_audit_context (changed_by, changed_by_system, change_operation_id)
//            VALUES ({0}, {1}, {2});
//        """;
        
//        db.ExecuteSqlRaw(sql, audit.ChangedBy, audit.ChangedBySystem, audit.OperationId);
//    }
//}

public static class ReadOnlyWriteOverride
{
    private static readonly AsyncLocal<bool> _override = new();

    public static bool IsOverridden => _override.Value;

    public static IDisposable Enable()
    {
        _override.Value = true;
        return new DisposableAction(() => _override.Value = false);
    }

    private class DisposableAction : IDisposable
    {
        private readonly Action _onDispose;

        public DisposableAction(Action onDispose) => _onDispose = onDispose;

        public void Dispose() => _onDispose();
    }
}

public class ReadOnlyInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ThrowIfHasChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ThrowIfHasChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ThrowIfHasChanges(DbContext? context)
    {
        if (context == null) 
        {
            return;        
        }

        var hasModifications = context.ChangeTracker.Entries()
            .Any(e => e.State == EntityState.Added
                   || e.State == EntityState.Modified
                   || e.State == EntityState.Deleted);

        if (hasModifications)
        {
            throw new DbUpdateException("Writing is disabled in this context.");
        }
    }
}

public class AuditConnectionInterceptor : DbConnectionInterceptor
{
    private readonly IAuditContextProvider _context;

    public AuditConnectionInterceptor(IAuditContextProvider context)
    {
        _context = context;
    }

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        var audit = _context.Current;

        var hasModifications = eventData.Context.ChangeTracker.Entries()
            .Any(e => e.State == EntityState.Added
                   || e.State == EntityState.Modified
                   || e.State == EntityState.Deleted);

        if (hasModifications)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                CREATE TEMP TABLE IF NOT EXISTS session_audit_context (
                    changed_by UUID,
                    changed_by_system UUID,
                    change_operation_id TEXT
                ) ON COMMIT DROP;
                TRUNCATE session_audit_context;
                INSERT INTO session_audit_context (changed_by, changed_by_system, change_operation_id)
                VALUES (@user, @system, @op);
            """;

            cmd.Parameters.Add(new NpgsqlParameter("user", audit.ChangedBy));
            cmd.Parameters.Add(new NpgsqlParameter("system", audit.ChangedBySystem));
            cmd.Parameters.Add(new NpgsqlParameter("op", audit.OperationId));
            cmd.ExecuteNonQuery();
        }
    }
}

/*
DI Implementation:

builder.Services.AddScoped<IAuditContextProvider, HttpContextAuditContextProvider>();
builder.Services.AddScoped<AuditConnectionInterceptor>();

builder.Services.AddDbContext<AppDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<AuditConnectionInterceptor>();
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(interceptor);
});

public class HttpContextAuditContextProvider(IHttpContextAccessor accessor) : IAuditContextProvider
{
    public AuditValues Current
    {
        get
        {
            var user = accessor.HttpContext?.User;
            var userId = Guid.Parse(user?.FindFirst("sub")?.Value ?? throw new Exception("Missing sub"));
            var systemId = Guid.Parse("00000000-0000-0000-0000-000000000001"); // evt fra config
            var operationId = Guid.NewGuid().ToString();

            return new AuditValues(userId, systemId, operationId);
        }
    }
}

[ApiController]
[Route("api/[controller]")]
public class PackageController(PackageService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(Package dto)
    {
        await service.Create(dto);
        return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Package>> Get(Guid id)
    {
        var result = await service.Get(id);
        return result is not null ? Ok(result) : NotFound();
    }
}

[ApiController]
[Route("api/[controller]")]
public class PackageController(PackageService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PackageCreateDto dto)
    {
        var audit = new AuditValues(dto.ChangedBy, dto.ChangedBySystem, dto.OperationId);
        await service.Create(dto.Package, audit);
        return CreatedAtAction(nameof(Get), new { id = dto.Package.Id }, dto.Package);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Package>> Get(Guid id)
    {
        var result = await service.Get(id);
        return result is not null ? Ok(result) : NotFound();
    }
}

*/
