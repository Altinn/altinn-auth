# Altinn AccessManagement Persistence - EntityFramework

## Usage

Saving changes

```csharp
public ThingsController(AppDbContext db, IAuditContextAccessor auditAccessor)

[...]

auditAccessor.Current = new BaseAudit
{
    Audit_ChangedBy = userId,
    Audit_ChangedBySystem = systemId,
    Audit_ChangeOperation = opId
};

await db.SaveChangesAsync();
```

or

```csharp
public ThingsController(AppDbContext db)

[...]

await _db.SaveChangesAsync(new BaseAudit
{
    Audit_ChangedBy = userId,
    Audit_ChangedBySystem = systemId,
    Audit_ChangeOperation = opId
});
```

