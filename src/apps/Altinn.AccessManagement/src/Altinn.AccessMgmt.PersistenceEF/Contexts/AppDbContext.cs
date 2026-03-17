using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Configurations;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Base;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

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

    public DbSet<AssignmentInstance> AssignmentInstances => Set<AssignmentInstance>();

    public DbSet<Delegation> Delegations => Set<Delegation>();

    public DbSet<DelegationPackage> DelegationPackages => Set<DelegationPackage>();

    public DbSet<DelegationResource> DelegationResources => Set<DelegationResource>();

    public DbSet<Entity> Entities => Set<Entity>();

    public DbSet<EntityLookup> EntityLookups => Set<EntityLookup>();

    public DbSet<EntityType> EntityTypes => Set<EntityType>();

    public DbSet<EntityVariant> EntityVariants => Set<EntityVariant>();

    public DbSet<EntityVariantRole> EntityVariantRoles => Set<EntityVariantRole>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<Package> Packages => Set<Package>();

    public DbSet<PackageResource> PackageResources => Set<PackageResource>();

    public DbSet<Provider> Providers => Set<Provider>();

    public DbSet<ProviderType> ProviderTypes => Set<ProviderType>();

    public DbSet<Resource> Resources => Set<Resource>();

    public DbSet<ResourceType> ResourceTypes => Set<ResourceType>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RoleMap> RoleMaps => Set<RoleMap>();

    public DbSet<RolePackage> RolePackages => Set<RolePackage>();

    public DbSet<RoleResource> RoleResources => Set<RoleResource>();

    public DbSet<RequestAssignment> RequestAssignments => Set<RequestAssignment>();

    public DbSet<RequestAssignmentPackage> RequestAssignmentPackages => Set<RequestAssignmentPackage>();

    public DbSet<RequestAssignmentResource> RequestAssignmentResources => Set<RequestAssignmentResource>();

    public DbSet<ErrorQueue> ErrorQueue => Set<ErrorQueue>();

    public DbSet<RightImportProgress> RightImportProgress => Set<RightImportProgress>();

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

    public DbSet<AuditRoleMap> AuditRoleMaps => Set<AuditRoleMap>();

    public DbSet<AuditRolePackage> AuditRolePackages => Set<AuditRolePackage>();

    public DbSet<AuditRoleResource> AuditRoleResources => Set<AuditRoleResource>();

    public DbSet<AuditRequestAssignment> AuditRequestAssignments => Set<AuditRequestAssignment>();

    public DbSet<AuditRequestAssignmentPackage> AuditRequestAssignmentPackages => Set<AuditRequestAssignmentPackage>();

    public DbSet<AuditRequestAssignmentResource> AuditRequestAssignmentResources => Set<AuditRequestAssignmentResource>();

    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ApplyAuditConfiguration(modelBuilder);
        ApplyConfiguration(modelBuilder);
        ApplyViewConfiguration(modelBuilder);
        modelBuilder.UseLowerCaseNamingConvention();
        modelBuilder.HasAnnotation(AuditExtensions.AnnotationName, AuditEFConfiguration.Version);
    }

    private void ApplyViewConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration<Connection>(new ConnectionConfiguration());

        /*
        modelBuilder.ApplyConfiguration<CompactEntity>(new CompactEntityConfiguration());
        modelBuilder.ApplyConfiguration<CompactRole>(new CompactRoleConfiguration());
        modelBuilder.ApplyConfiguration<CompactPackage>(new CompactPackageConfiguration());
        modelBuilder.ApplyConfiguration<CompactResource>(new CompactResourceConfiguration());
        
        modelBuilder.ApplyConfiguration<Relation>(new RelationConfiguration2());
        */
    }

    private void ApplyAuditConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration<AuditArea>(new AuditAreaConfiguration());
        modelBuilder.ApplyConfiguration<AuditAreaGroup>(new AuditAreaGroupConfiguration());
        modelBuilder.ApplyConfiguration<AuditAssignment>(new AuditAssignmentConfiguration());
        modelBuilder.ApplyConfiguration<AuditAssignmentPackage>(new AuditAssignmentPackageConfiguration());
        modelBuilder.ApplyConfiguration<AuditAssignmentResource>(new AuditAssignmentResourceConfiguration());
        modelBuilder.ApplyConfiguration<AuditAssignmentInstance>(new AuditAssignmentInstanceConfiguration());
        modelBuilder.ApplyConfiguration<AuditDelegation>(new AuditDelegationConfiguration());
        modelBuilder.ApplyConfiguration<AuditDelegationPackage>(new AuditDelegationPackageConfiguration());
        modelBuilder.ApplyConfiguration<AuditDelegationResource>(new AuditDelegationResourceConfiguration());
        modelBuilder.ApplyConfiguration<AuditEntity>(new AuditEntityConfiguration());
        modelBuilder.ApplyConfiguration<AuditEntityLookup>(new AuditEntityLookupConfiguration());
        modelBuilder.ApplyConfiguration<AuditEntityType>(new AuditEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration<AuditEntityVariant>(new AuditEntityVariantConfiguration());
        modelBuilder.ApplyConfiguration<AuditEntityVariantRole>(new AuditEntityVariantRoleConfiguration());
        modelBuilder.ApplyConfiguration<OutboxMessage>(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration<AuditPackage>(new AuditPackageConfiguration());
        modelBuilder.ApplyConfiguration<AuditPackageResource>(new AuditPackageResourceConfiguration());
        modelBuilder.ApplyConfiguration<AuditProvider>(new AuditProviderConfiguration());
        modelBuilder.ApplyConfiguration<AuditProviderType>(new AuditProviderTypeConfiguration());
        modelBuilder.ApplyConfiguration<AuditResource>(new AuditResourceConfiguration());
        modelBuilder.ApplyConfiguration<AuditResourceType>(new AuditResourceTypeConfiguration());
        modelBuilder.ApplyConfiguration<AuditRole>(new AuditRoleConfiguration());
        modelBuilder.ApplyConfiguration<AuditRoleMap>(new AuditRoleMapConfiguration());
        modelBuilder.ApplyConfiguration<AuditRolePackage>(new AuditRolePackageConfiguration());
        modelBuilder.ApplyConfiguration<AuditRoleResource>(new AuditRoleResourceConfiguration());
        modelBuilder.ApplyConfiguration<AuditRequestAssignment>(new AuditRequestAssignmentConfiguration());
        modelBuilder.ApplyConfiguration<AuditRequestAssignmentPackage>(new AuditRequestAssignmentPackageConfiguration());
        modelBuilder.ApplyConfiguration<AuditRequestAssignmentResource>(new AuditRequestAssignmentResourceConfiguration());
    }

    private void ApplyConfiguration(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration<TranslationEntry>(new TranslationEntryConfiguration());

        modelBuilder.ApplyConfiguration<Area>(new AreaConfiguration());
        modelBuilder.ApplyConfiguration<AreaGroup>(new AreaGroupConfiguration());
        modelBuilder.ApplyConfiguration<Assignment>(new AssignmentConfiguration());
        modelBuilder.ApplyConfiguration<AssignmentPackage>(new AssignmentPackageConfiguration());
        modelBuilder.ApplyConfiguration<AssignmentResource>(new AssignmentResourceConfiguration());
        modelBuilder.ApplyConfiguration<AssignmentInstance>(new AssignmentInstanceConfiguration());
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
        modelBuilder.ApplyConfiguration<Role>(new RoleConfiguration());
        modelBuilder.ApplyConfiguration<RoleMap>(new RoleMapConfiguration());
        modelBuilder.ApplyConfiguration<RolePackage>(new RolePackageConfiguration());
        modelBuilder.ApplyConfiguration<RoleResource>(new RoleResourceConfiguration());
        modelBuilder.ApplyConfiguration<RequestAssignment>(new RequestAssignmentConfiguration());
        modelBuilder.ApplyConfiguration<RequestAssignmentPackage>(new RequestAssignmentPackageConfiguration());
        modelBuilder.ApplyConfiguration<RequestAssignmentResource>(new RequestAssignmentResourceConfiguration());
        modelBuilder.ApplyConfiguration<ErrorQueue>(new ErrorQueueConfiguration());
        modelBuilder.ApplyConfiguration<RightImportProgress>(new RightImportProgressConfiguration());
        modelBuilder.ApplyConfiguration<InstanceSourceType>(new InstanceSourceTypeConfiguration());
    }

    #region Extensions

    public override Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        SaveChangesAsync(AuditAccessor.AuditValues ?? throw MissingAudit(), ct);

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken ct = default) =>
        SaveChangesAsync(AuditAccessor.AuditValues ?? throw MissingAudit(), acceptAllChangesOnSuccess, ct);

    public async Task<int> SaveChangesAsync(AuditValues audit, CancellationToken ct = default) =>
        await SaveChangesAsync(audit, acceptAllChangesOnSuccess: true, ct);

    public override int SaveChanges() =>
        SaveChanges(AuditAccessor.AuditValues ?? throw MissingAudit());

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        SaveChanges(AuditAccessor.AuditValues ?? throw MissingAudit(), acceptAllChangesOnSuccess);

    public int SaveChanges(AuditValues audit) => SaveChanges(audit, acceptAllChangesOnSuccess: true);

    private static InvalidOperationException MissingAudit() =>
        new("AuditContextAccessor.Current is null. Set it in your controller/service OR call SaveChangesAsync(BaseAudit audit, ...) explicitly.");

    public int SaveChanges(AuditValues audit, bool acceptAllChangesOnSuccess)
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
        using var transaction = currentTransaction ? null : Database.BeginTransaction();

        try
        {
            Database.ExecuteSqlInterpolated(AuditContextSql(audit));
            var affected = base.SaveChanges(acceptAllChangesOnSuccess);

            if (transaction is { })
            {
                transaction.Commit();
            }

            return affected;
        }
        catch
        {
            if (transaction is { })
            {
                transaction.Rollback();
            }

            throw;
        }
    }

    /// <summary>
    /// Adds or updates an outbox message associated with the specified reference identifier.
    /// </summary>
    /// <remarks>
    /// This method implements an <c>upsert</c> pattern for outbox messages:
    /// <list type="bullet">
    /// <item>
    /// If no existing outbox entry is found for the provided <paramref name="refId"/>,
    /// a new value is created using <paramref name="addValueFactory"/>.
    /// </item>
    /// <item>
    /// If an existing outbox entry is found, its value is updated using
    /// <paramref name="updateValueFactory"/>, which receives both the current stored data
    /// and the incoming outbox data.
    /// </item>
    /// </list>
    /// 
    /// The method is typically used to ensure that a single logical message or event
    /// associated with a given reference identifier is maintained in the outbox.
    /// </remarks>
    /// <typeparam name="T">
    /// The type of the data to be stored or updated in the outbox message.
    /// </typeparam>
    /// <param name="refId">
    /// A reference identifier used to locate an existing outbox message.
    /// This typically represents a domain entity identifier or correlation key.
    /// </param>
    /// <param name="handler">handler that should process the message.</param>
    /// <param name="addValueFactory">
    /// A factory function used to create the initial value when no existing
    /// outbox message is found for the given <paramref name="refId"/>.
    /// </param>
    /// <param name="updateValueFactory">
    /// A function used to update the value when an existing outbox message is found.
    /// The function receives the current stored value and the existing outbox data,
    /// and returns the updated value to be stored.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A task that represents the asynchronous upsert operation.
    /// </returns>
    public async Task UpsertOutboxAsync<T>(
        string refId,
        string handler,
        Func<OutboxMessage, T> addValueFactory,
        Func<OutboxMessage, T, T> updateValueFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(refId);
        ArgumentNullException.ThrowIfNull(addValueFactory);

        var message = await OutboxMessages
            .AsTracking()
            .FirstOrDefaultAsync(
                o =>
                o.RefId == refId &&
                o.Handler == handler &&
                o.Status == OutboxStatus.Pending,
                cancellationToken);

        UpsertOutbox(refId, handler, addValueFactory, updateValueFactory, message);
    }

    /// <summary>
    /// Adds or updates an outbox message associated with the specified reference identifier.
    /// </summary>
    /// <remarks>
    /// This method implements an <c>upsert</c> pattern for outbox messages:
    /// <list type="bullet">
    /// <item>
    /// If no existing outbox entry is found for the provided <paramref name="refId"/>,
    /// a new value is created using <paramref name="addValueFactory"/>.
    /// </item>
    /// <item>
    /// If an existing outbox entry is found, its value is updated using
    /// <paramref name="updateValueFactory"/>, which receives both the current stored data
    /// and the incoming outbox data.
    /// </item>
    /// </list>
    /// 
    /// The method is typically used to ensure that a single logical message or event
    /// associated with a given reference identifier is maintained in the outbox.
    /// </remarks>
    /// <typeparam name="T">
    /// The type of the data to be stored or updated in the outbox message.
    /// </typeparam>
    /// <param name="refId">
    /// A reference identifier used to locate an existing outbox message.
    /// This typically represents a domain entity identifier or correlation key.
    /// </param>
    /// <param name="handler">handler that should process the message.</param>
    /// <param name="addValueFactory">
    /// A factory function used to create the initial value when no existing
    /// outbox message is found for the given <paramref name="refId"/>.
    /// </param>
    /// <param name="updateValueFactory">
    /// A function used to update the value when an existing outbox message is found.
    /// The function receives the current stored value and the existing outbox data,
    /// and returns the updated value to be stored.
    /// </param>
    public void UpsertOutbox<T>(
        string refId,
        string handler,
        Func<OutboxMessage, T> addValueFactory,
        Func<OutboxMessage, T, T> updateValueFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(refId);
        ArgumentNullException.ThrowIfNull(addValueFactory);

        var message = OutboxMessages
            .AsTracking()
            .FirstOrDefault(o =>
                o.RefId == refId &&
                o.Handler == handler &&
                o.Status == OutboxStatus.Pending);

        UpsertOutbox(refId, handler, addValueFactory, updateValueFactory, message);
    }

    private void UpsertOutbox<T>(string refId, string handler, Func<OutboxMessage, T> addValueFactory, Func<OutboxMessage, T, T> updateValueFactory, OutboxMessage message)
    {
        if (message is { })
        {
            if (updateValueFactory is { })
            {
                var data = JsonSerializer.Deserialize<T>(message.Data);
                var updatedValue = updateValueFactory(message, data);
                message.Data = JsonSerializer.Serialize(updatedValue);
            }
        }
        else
        {
            message = new OutboxMessage()
            {
                CorrelationId = Activity.Current?.TraceId.ToString(),
                Status = OutboxStatus.Pending,
                RefId = refId,
                Handler = handler,
            };

            var data = addValueFactory(message);
            message.Data = JsonSerializer.Serialize(data);
            OutboxMessages.Add(message);
        }
    }

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

            if (transaction is { })
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return affected;
        }
        catch
        {
            if (transaction is { })
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
