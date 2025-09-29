using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Configurations;

public class ConnectionConfiguration : IEntityTypeConfiguration<Connection>
{
    public void Configure(EntityTypeBuilder<Connection> builder)
    {
        //// builder.ToDefaultView();

        builder.ToView("connectionef", BaseConfiguration.BaseSchema);
        builder.HasKey(x => new { x.FromId, x.ToId, x.RoleId, x.Reason });

        builder.PropertyWithReference(navKey: t => t.From, foreignKey: t => t.FromId, principalKey: t => t.Id);
        builder.PropertyWithReference(navKey: t => t.To, foreignKey: t => t.ToId, principalKey: t => t.Id);
        builder.PropertyWithReference(navKey: t => t.Via, foreignKey: t => t.ViaId, principalKey: t => t.Id, required: false);

        builder.PropertyWithReference(navKey: t => t.Role, foreignKey: t => t.RoleId, principalKey: t => t.Id, required: false);
        builder.PropertyWithReference(navKey: t => t.ViaRole, foreignKey: t => t.ViaRoleId, principalKey: t => t.Id, required: false);

        builder.PropertyWithReference(navKey: t => t.Package, foreignKey: t => t.PackageId, principalKey: t => t.Id, required: false);
        builder.PropertyWithReference(navKey: t => t.Resource, foreignKey: t => t.ResourceId, principalKey: t => t.Id, required: false);
        //builder.PropertyWithReference(navKey: t => t.Instance, foreignKey: t => t.InstanceId, principalKey: t => t.Id, required: false);
    }

    private string ViewCode()
    {
        return """
        SELECT a.fromid,
               a.roleid,
               NULL::uuid     AS viaid,
               NULL::uuid     AS viaroleid,
               a.toid,
               ap.packageid,
               NULL::uuid     AS resourceid,
               'Direct'::text AS reason
        FROM dbo.assignment a
                 JOIN dbo.assignmentpackage ap ON ap.assignmentid = a.id
        UNION ALL
        SELECT a.fromid,
               a.roleid,
               NULL::uuid     AS viaid,
               NULL::uuid     AS viaroleid,
               a.toid,
               rp.packageid,
               NULL::uuid     AS resourceid,
               'Direct'::text AS reason
        FROM dbo.assignment a
                 JOIN dbo.rolepackage rp ON rp.roleid = a.roleid and rp.hasaccess = true
        UNION ALL
        SELECT a.fromid,
               a.roleid,
               NULL::uuid     AS viaid,
               NULL::uuid     AS viaroleid,
               a.toid,
               rp.packageid,
               NULL::uuid     AS resourceid,
               'Direct'::text AS reason
        FROM dbo.assignment a
                 JOIN dbo.rolemap rm ON a.roleid = rm.hasroleid
                 JOIN dbo.rolepackage rp ON rp.roleid = rm.getroleid AND rp.hasaccess = true
        UNION ALL
        SELECT a.fromid,
               a.roleid,
               a.toid          AS viaid,
               a2.roleid       AS viaroleid,
               a2.toid,
               ap.packageid,
               NULL::uuid      AS resourceid,
               'KeyRole'::text AS reason
        FROM dbo.assignment a
                 JOIN dbo.assignment a2 ON a.toid = a2.fromid
                 JOIN dbo.role r ON a2.roleid = r.id AND r.iskeyrole = true
                 JOIN dbo.assignmentpackage ap ON ap.assignmentid = a.id
        UNION ALL
        SELECT a.fromid,
               a.roleid,
               a.toid          AS viaid,
               a2.roleid       AS viaroleid,
               a2.toid,
               rp.packageid,
               NULL::uuid      AS resourceid,
               'KeyRole'::text AS reason
        FROM dbo.assignment a
                 JOIN dbo.assignment a2 ON a.toid = a2.fromid
                 JOIN dbo.role r ON a2.roleid = r.id AND r.iskeyrole = true
                 JOIN dbo.rolepackage rp ON rp.roleid = a.roleid AND rp.hasaccess = true
        UNION ALL
        SELECT fa.fromid,
               fa.roleid,
               fa.toid            AS viaid,
               ta.roleid          AS viaroleid,
               ta.toid,
               dp.packageid,
               NULL::uuid         AS resourceid,
               'Delegation'::text AS reason
        FROM dbo.delegation d
                 JOIN dbo.assignment fa ON fa.id = d.fromid
                 JOIN dbo.assignment ta ON ta.id = d.toid
                 JOIN dbo.delegationpackage dp ON dp.delegationid = d.id
        """;
    }
}

/*

//// Test versjon for å bruke Json i resultatet og mappe til Compact modeller

public class RelationJsonConfiguration : IEntityTypeConfiguration<Relation>
{
    public void Configure(EntityTypeBuilder<Relation> builder)
    {
        builder.ConfigureAsView("relation_view", "dbo");

        builder.Property(x => x.Reason).HasColumnName("reason");

        builder.Property(x => x.FromId).HasColumnName("fromid");
        builder.Property(x => x.RoleId).HasColumnName("roleid");
        builder.Property(x => x.ViaId).HasColumnName("viaid");
        builder.Property(x => x.ViaRoleId).HasColumnName("viaroleid");
        builder.Property(x => x.ToId).HasColumnName("toid");

        builder.Property(x => x.PackageId).HasColumnName("packageid");
        builder.Property(x => x.ResourceId).HasColumnName("resourceid");

        builder.Property(x => x.From).HasColumnName("from_json").HasColumnType("jsonb");
        builder.Property(x => x.Role).HasColumnName("role_json").HasColumnType("jsonb");
        builder.Property(x => x.Via).HasColumnName("via_json").HasColumnType("jsonb");
        builder.Property(x => x.ViaRole).HasColumnName("viarole_json").HasColumnType("jsonb");
        builder.Property(x => x.To).HasColumnName("to_json").HasColumnType("jsonb");

        builder.Property(x => x.Package).HasColumnName("package_json").HasColumnType("jsonb");
        builder.Property(x => x.Resource).HasColumnName("resource_json").HasColumnType("jsonb");
    }
}

public class CompactEntityConfiguration : IEntityTypeConfiguration<CompactEntity>
{
    public void Configure(EntityTypeBuilder<CompactEntity> builder)
    {
        builder.ToView("compactentity", "dbo");
        builder.HasKey(x => x.Id);

        builder.Property(t => t.Name);
        builder.Property(t => t.Type);
        builder.Property(t => t.Variant);

        builder.HasOne<CompactEntity>(t => t.Parent).WithMany().HasForeignKey(t => t.Id).HasPrincipalKey(t => t.ParentId);
        builder.HasMany<CompactEntity>(t => t.Children).WithOne().HasForeignKey(t => t.ParentId).HasPrincipalKey(t => t.Id);
    }
}

public class CompactRoleConfiguration : IEntityTypeConfiguration<CompactRole>
{
    public void Configure(EntityTypeBuilder<CompactRole> builder)
    {
        builder.ToView("compactrole", "dbo");
        builder.HasKey(x => x.Id);

        builder.Property(t => t.Code);

        // builder.HasMany<CompactRole>(t => t.Children).WithOne().HasForeignKey(t => t.ParentId).HasPrincipalKey(t => t.Id);
    }
}

public class CompactPackageConfiguration : IEntityTypeConfiguration<CompactPackage>
{
    public void Configure(EntityTypeBuilder<CompactPackage> builder)
    {
        builder.ToView("compactpackage", "dbo");
        builder.HasKey(x => x.Id);

        builder.Property(t => t.Urn);

        // builder.HasMany<CompactRole>(t => t.Children).WithOne().HasForeignKey(t => t.ParentId).HasPrincipalKey(t => t.Id);
    }
}

public class CompactResourceConfiguration : IEntityTypeConfiguration<CompactResource>
{
    public void Configure(EntityTypeBuilder<CompactResource> builder)
    {
        builder.ToView("compactresource", "dbo");
        builder.HasKey(x => x.Id);

        builder.Property(t => t.Value);

        // builder.HasMany<CompactRole>(t => t.Children).WithOne().HasForeignKey(t => t.ParentId).HasPrincipalKey(t => t.Id);
    }
}
*/
