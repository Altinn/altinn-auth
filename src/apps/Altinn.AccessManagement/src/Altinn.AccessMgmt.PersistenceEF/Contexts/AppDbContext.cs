using Altinn.AccessMgmt.PersistenceEF.Configurations;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.PersistenceEF.Contexts;

/// <inheritdoc />
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
        modelBuilder.ApplyConfiguration<Resource>(new ResourceConfiguration());
        modelBuilder.ApplyConfiguration<Role>(new RoleConfiguration());
        modelBuilder.ApplyConfiguration<RoleLookup>(new RoleLookupConfiguration());
        modelBuilder.ApplyConfiguration<RoleMap>(new RoleMapConfiguration());
        modelBuilder.ApplyConfiguration<RolePackage>(new RolePackageConfiguration());
        modelBuilder.ApplyConfiguration<RoleResource>(new RoleResourceConfiguration());
    }
}
