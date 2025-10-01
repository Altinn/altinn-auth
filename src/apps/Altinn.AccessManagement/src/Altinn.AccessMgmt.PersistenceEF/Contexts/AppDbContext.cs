using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Configurations;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Queries.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Contexts;

/// <inheritdoc />
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    internal IAuditAccessor AuditAccessor { get; set; }

    public DbSet<Connection> Connections => Set<Connection>();

    public DbSet<TranslationEntry> TranslationEntries => Set<TranslationEntry>();

    #region DbSets

    public DbSet<Area> Areas => Set<Area>();

    public DbSet<AreaGroup> AreaGroups => Set<AreaGroup>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<AssignmentPackage> AssignmentPackages => Set<AssignmentPackage>();

    public DbSet<AssignmentResource> AssignmentResources => Set<AssignmentResource>();

    public DbSet<Delegation> Delegations => Set<Delegation>();

    public DbSet<DelegationPackage> DelegationPackages => Set<DelegationPackage>();

    public DbSet<DelegationResource> DelegationResources => Set<DelegationResource>();

    public DbSet<Entity> Entities => Set<Entity>();

    public DbSet<EntityLookup> EntityLookups => Set<EntityLookup>();

    public DbSet<EntityType> EntityTypes => Set<EntityType>();

    public DbSet<EntityVariant> EntityVariants => Set<EntityVariant>();

    public DbSet<EntityVariantRole> EntityVariantRoles => Set<EntityVariantRole>();

    public DbSet<Package> Packages => Set<Package>();

    public DbSet<PackageResource> PackageResources => Set<PackageResource>();

    public DbSet<Provider> Providers => Set<Provider>();

    public DbSet<ProviderType> ProviderTypes => Set<ProviderType>();

    public DbSet<Resource> Resources => Set<Resource>();

    public DbSet<ResourceType> ResourceTypes => Set<ResourceType>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RoleLookup> RoleLookups => Set<RoleLookup>();

    public DbSet<RoleMap> RoleMaps => Set<RoleMap>();

    public DbSet<RolePackage> RolePackages => Set<RolePackage>();

    public DbSet<RoleResource> RoleResources => Set<RoleResource>();

    #endregion

    #region Audit

    public DbSet<AuditArea> AuditAreas => Set<AuditArea>();

    public DbSet<AuditAreaGroup> AuditAreaGroups => Set<AuditAreaGroup>();

    public DbSet<AuditAssignment> AuditAssignments => Set<AuditAssignment>();

    public DbSet<AuditAssignmentPackage> AuditAssignmentPackages => Set<AuditAssignmentPackage>();

    public DbSet<AuditAssignmentResource> AuditAssignmentResources => Set<AuditAssignmentResource>();

    public DbSet<AuditDelegation> AuditDelegations => Set<AuditDelegation>();

    public DbSet<AuditDelegationPackage> AuditDelegationPackages => Set<AuditDelegationPackage>();

    public DbSet<AuditDelegationResource> AuditDelegationResources => Set<AuditDelegationResource>();

    public DbSet<AuditEntity> AuditEntities => Set<AuditEntity>();

    public DbSet<AuditEntityLookup> AuditEntityLookups => Set<AuditEntityLookup>();

    public DbSet<AuditEntityType> AuditEntityTypes => Set<AuditEntityType>();

    public DbSet<AuditEntityVariant> AuditEntityVariants => Set<AuditEntityVariant>();

    public DbSet<AuditEntityVariantRole> AuditEntityVariantRoles => Set<AuditEntityVariantRole>();

    public DbSet<AuditPackage> AuditPackages => Set<AuditPackage>();

    public DbSet<AuditPackageResource> AuditPackageResources => Set<AuditPackageResource>();

    public DbSet<AuditProvider> AuditProviders => Set<AuditProvider>();

    public DbSet<AuditProviderType> AuditProviderTypes => Set<AuditProviderType>();

    public DbSet<AuditResource> AuditResources => Set<AuditResource>();

    public DbSet<AuditResourceType> AuditResourceTypes => Set<AuditResourceType>();

    public DbSet<AuditRole> AuditRoles => Set<AuditRole>();

    public DbSet<AuditRoleLookup> AuditRoleLookups => Set<AuditRoleLookup>();

    public DbSet<AuditRoleMap> AuditRoleMaps => Set<AuditRoleMap>();

    public DbSet<AuditRolePackage> AuditRolePackages => Set<AuditRolePackage>();

    public DbSet<AuditRoleResource> AuditRoleResources => Set<AuditRoleResource>();

    #endregion

    public DbSet<PackageDelegationCheckRow> PackageDelegationChecks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
         modelBuilder.Entity<PackageDelegationCheckRow>().HasNoKey();
        ApplyAuditConfiguration(modelBuilder);
        ApplyConfiguration(modelBuilder);
        ApplyViewConfiguration(modelBuilder);
        modelBuilder.UseLowerCaseNamingConvention();
    }

    private void ApplyViewConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration<Connection>(new ConnectionConfiguration());
        //modelBuilder.ApplyConfiguration<CompactEntity>(new CompactEntityConfiguration());
        //modelBuilder.ApplyConfiguration<CompactRole>(new CompactRoleConfiguration());
        //modelBuilder.ApplyConfiguration<CompactPackage>(new CompactPackageConfiguration());
        //modelBuilder.ApplyConfiguration<CompactResource>(new CompactResourceConfiguration());

        // modelBuilder.ApplyConfiguration<Relation>(new RelationConfiguration2());
    }

    private void ApplyAuditConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration<AuditArea>(new AuditAreaConfiguration());
        modelBuilder.ApplyConfiguration<AuditAreaGroup>(new AuditAreaGroupConfiguration());
        modelBuilder.ApplyConfiguration<AuditAssignment>(new AuditAssignmentConfiguration());
        modelBuilder.ApplyConfiguration<AuditAssignmentPackage>(new AuditAssignmentPackageConfiguration());
        modelBuilder.ApplyConfiguration<AuditAssignmentResource>(new AuditAssignmentResourceConfiguration());
        modelBuilder.ApplyConfiguration<AuditDelegation>(new AuditDelegationConfiguration());
        modelBuilder.ApplyConfiguration<AuditDelegationPackage>(new AuditDelegationPackageConfiguration());
        modelBuilder.ApplyConfiguration<AuditDelegationResource>(new AuditDelegationResourceConfiguration());
        modelBuilder.ApplyConfiguration<AuditEntity>(new AuditEntityConfiguration());
        modelBuilder.ApplyConfiguration<AuditEntityLookup>(new AuditEntityLookupConfiguration());
        modelBuilder.ApplyConfiguration<AuditEntityType>(new AuditEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration<AuditEntityVariant>(new AuditEntityVariantConfiguration());
        modelBuilder.ApplyConfiguration<AuditEntityVariantRole>(new AuditEntityVariantRoleConfiguration());
        modelBuilder.ApplyConfiguration<AuditPackage>(new AuditPackageConfiguration());
        modelBuilder.ApplyConfiguration<AuditPackageResource>(new AuditPackageResourceConfiguration());
        modelBuilder.ApplyConfiguration<AuditProvider>(new AuditProviderConfiguration());
        modelBuilder.ApplyConfiguration<AuditProviderType>(new AuditProviderTypeConfiguration());
        modelBuilder.ApplyConfiguration<AuditResource>(new AuditResourceConfiguration());
        modelBuilder.ApplyConfiguration<AuditResourceType>(new AuditResourceTypeConfiguration());
        modelBuilder.ApplyConfiguration<AuditRole>(new AuditRoleConfiguration());
        modelBuilder.ApplyConfiguration<AuditRoleLookup>(new AuditRoleLookupConfiguration());
        modelBuilder.ApplyConfiguration<AuditRoleMap>(new AuditRoleMapConfiguration());
        modelBuilder.ApplyConfiguration<AuditRolePackage>(new AuditRolePackageConfiguration());
        modelBuilder.ApplyConfiguration<AuditRoleResource>(new AuditRoleResourceConfiguration());
    }

    private void ApplyConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration<TranslationEntry>(new TranslationEntryConfiguration());

        modelBuilder.ApplyConfiguration<Area>(new AreaConfiguration());
        modelBuilder.ApplyConfiguration<AreaGroup>(new AreaGroupConfiguration());
        modelBuilder.ApplyConfiguration<Assignment>(new AssignmentConfiguration());
        modelBuilder.ApplyConfiguration<AssignmentPackage>(new AssignmentPackageConfiguration());
        modelBuilder.ApplyConfiguration<AssignmentResource>(new AssignmentResourceConfiguration());
        modelBuilder.ApplyConfiguration<Delegation>(new DelegationConfiguration());
        modelBuilder.ApplyConfiguration<DelegationPackage>(new DelegationPackageConfiguration());
        modelBuilder.ApplyConfiguration<DelegationResource>(new DelegationResourceConfiguration());
        modelBuilder.ApplyConfiguration<Entity>(new EntityConfiguration());
        modelBuilder.ApplyConfiguration<EntityLookup>(new EntityLookupConfiguration());
        modelBuilder.ApplyConfiguration<EntityType>(new EntityTypeConfiguration());
        modelBuilder.ApplyConfiguration<EntityVariant>(new EntityVariantConfiguration());
        modelBuilder.ApplyConfiguration<EntityVariantRole>(new EntityVariantRoleConfiguration());
        modelBuilder.ApplyConfiguration<Package>(new PackageConfiguration());
        modelBuilder.ApplyConfiguration<PackageResource>(new PackageResourceConfiguration());
        modelBuilder.ApplyConfiguration<Provider>(new ProviderConfiguration());
        modelBuilder.ApplyConfiguration<ProviderType>(new ProviderTypeConfiguration());
        modelBuilder.ApplyConfiguration<ResourceType>(new ResourceTypeConfiguration());
        modelBuilder.ApplyConfiguration<Resource>(new ResourceConfiguration());
        modelBuilder.ApplyConfiguration<ResourceType>(new ResourceTypeConfiguration());
        modelBuilder.ApplyConfiguration<Role>(new RoleConfiguration());
        modelBuilder.ApplyConfiguration<RoleLookup>(new RoleLookupConfiguration());
        modelBuilder.ApplyConfiguration<RoleMap>(new RoleMapConfiguration());
        modelBuilder.ApplyConfiguration<RolePackage>(new RolePackageConfiguration());
        modelBuilder.ApplyConfiguration<RoleResource>(new RoleResourceConfiguration());
    }

    #region Extensions

    public override Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        SaveChangesAsync(AuditAccessor.AuditValues ?? throw MissingAudit(), ct);

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default) =>
        SaveChangesAsync(AuditAccessor.AuditValues ?? throw MissingAudit(), acceptAllChangesOnSuccess, ct);

    private static InvalidOperationException MissingAudit() =>
        new("AuditContextAccessor.Current is null. Set it in your controller/service OR call SaveChangesAsync(BaseAudit audit, ...) explicitly.");

    public async Task<int> SaveChangesAsync(AuditValues audit, CancellationToken ct = default) =>
        await SaveChangesAsync(audit, acceptAllChangesOnSuccess: true, ct);

    public async Task<int> SaveChangesAsync(AuditValues audit, bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ValidateAuditValues(audit);

        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            if (entry.Entity is BaseAudit auditable)
            {
                auditable.SetAuditValues(audit);
            }
        }

        var currentTransaction = Database.CurrentTransaction is not null;
        using var transaction = currentTransaction ? null : await Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await Database.ExecuteSqlInterpolatedAsync(AuditContextSql(audit), cancellationToken);
            var affected = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return affected;
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            throw;
        }
    }

    private void ValidateAuditValues(AuditValues audit)
    {
        if (audit == null || audit.ChangedBy == Guid.Empty || audit.ChangedBySystem == Guid.Empty || string.IsNullOrWhiteSpace(audit.OperationId))
        {
            throw new InvalidOperationException("Audit fields are required.");
        }
    }

    private static FormattableString AuditContextSql(AuditValues a) => $@"
    SELECT set_config('app.changed_by',        {a.ChangedBy.ToString()},        true);
    SELECT set_config('app.changed_by_system', {a.ChangedBySystem.ToString()},  true);
    SELECT set_config('app.change_operation_id', {a.OperationId},               true);

    CREATE TEMP TABLE IF NOT EXISTS session_audit_context(
        changed_by uuid,
        changed_by_system uuid,
        change_operation_id text
    ) ON COMMIT DROP;

    TRUNCATE session_audit_context;

    INSERT INTO session_audit_context (changed_by, changed_by_system, change_operation_id)
    VALUES ({a.ChangedBy}, {a.ChangedBySystem}, {a.OperationId});
    ";

    private static FormattableString AuditContextSqlOld(AuditValues a) => $"""
    -- SET LOCAL expects text
    SET LOCAL app.changed_by = '{a.ChangedBy.ToString()}';
    SET LOCAL app.changed_by_system = '{a.ChangedBySystem.ToString()}';
    SET LOCAL app.change_operation_id = '{a.OperationId}';

    -- Temp table to carry values through ON DELETE CASCADE
    CREATE TEMP TABLE IF NOT EXISTS session_audit_context(
        changed_by uuid,
        changed_by_system uuid,
        change_operation_id text
    ) ON COMMIT DROP;

    TRUNCATE session_audit_context;

    INSERT INTO session_audit_context (changed_by, changed_by_system, change_operation_id)
    VALUES ({a.ChangedBy}, {a.ChangedBySystem}, {a.OperationId});
    """;
    #endregion
}
