using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.EnsureSchema(
                name: "dbo_history");

            migrationBuilder.CreateTable(
                name: "auditarea",
                schema: "dbo_history",
                columns: table => new
                {
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    iconurl = table.Column<string>(type: "text", nullable: true),
                    groupid = table.Column<Guid>(type: "uuid", nullable: false),
                    urn = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditarea", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditareagroup",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    entitytypeid = table.Column<Guid>(type: "uuid", nullable: false),
                    urn = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditareagroup", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditassignment",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    roleid = table.Column<Guid>(type: "uuid", nullable: false),
                    fromid = table.Column<Guid>(type: "uuid", nullable: false),
                    toid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditassignment", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditassignmentpackage",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    assignmentid = table.Column<Guid>(type: "uuid", nullable: false),
                    packageid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditassignmentpackage", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditassignmentresource",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    assignmentid = table.Column<Guid>(type: "uuid", nullable: false),
                    resourceid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditassignmentresource", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditdelegation",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    fromid = table.Column<Guid>(type: "uuid", nullable: false),
                    toid = table.Column<Guid>(type: "uuid", nullable: false),
                    facilitatorid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditdelegation", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditdelegationpackage",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    delegationid = table.Column<Guid>(type: "uuid", nullable: false),
                    packageid = table.Column<Guid>(type: "uuid", nullable: false),
                    rolepackageid = table.Column<Guid>(type: "uuid", nullable: true),
                    assignmentpackageid = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditdelegationpackage", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditdelegationresource",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    delegationid = table.Column<Guid>(type: "uuid", nullable: false),
                    resourceid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditdelegationresource", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditentity",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    typeid = table.Column<Guid>(type: "uuid", nullable: false),
                    variantid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    refid = table.Column<string>(type: "text", nullable: true),
                    parentid = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditentity", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditentitylookup",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    entityid = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "text", nullable: true),
                    value = table.Column<string>(type: "text", nullable: true),
                    isprotected = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditentitylookup", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditentitytype",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    providerid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditentitytype", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditentityvariant",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    typeid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditentityvariant", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditentityvariantrole",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    variantid = table.Column<Guid>(type: "uuid", nullable: false),
                    roleid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditentityvariantrole", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditpackage",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    providerid = table.Column<Guid>(type: "uuid", nullable: false),
                    entitytypeid = table.Column<Guid>(type: "uuid", nullable: false),
                    areaid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    isassignable = table.Column<bool>(type: "boolean", nullable: false),
                    isdelegable = table.Column<bool>(type: "boolean", nullable: false),
                    hasresources = table.Column<bool>(type: "boolean", nullable: false),
                    urn = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditpackage", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditpackageresource",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    packageid = table.Column<Guid>(type: "uuid", nullable: false),
                    resourceid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditpackageresource", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditprovider",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true),
                    refid = table.Column<string>(type: "text", nullable: true),
                    logourl = table.Column<string>(type: "text", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    typeid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditprovider", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditprovidertype",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditprovidertype", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditresource",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    providerid = table.Column<Guid>(type: "uuid", nullable: false),
                    typeid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    refid = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditresource", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditresourcetype",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditresourcetype", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditrole",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    entitytypeid = table.Column<Guid>(type: "uuid", nullable: true),
                    providerid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    iskeyrole = table.Column<bool>(type: "boolean", nullable: false),
                    urn = table.Column<string>(type: "text", nullable: true),
                    isassignable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditrole", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditrolelookup",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    roleid = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "text", nullable: true),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditrolelookup", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditrolemap",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    hasroleid = table.Column<Guid>(type: "uuid", nullable: false),
                    getroleid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditrolemap", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditrolepackage",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    roleid = table.Column<Guid>(type: "uuid", nullable: false),
                    packageid = table.Column<Guid>(type: "uuid", nullable: false),
                    entityvariantid = table.Column<Guid>(type: "uuid", nullable: true),
                    hasaccess = table.Column<bool>(type: "boolean", nullable: false),
                    canassign = table.Column<bool>(type: "boolean", nullable: false),
                    candelegate = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditrolepackage", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditroleresource",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    roleid = table.Column<Guid>(type: "uuid", nullable: false),
                    resourceid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditroleresource", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "providertype",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_providertype", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "resourcetype",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resourcetype", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "translationentry",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    languagecode = table.Column<string>(type: "text", nullable: false),
                    fieldname = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_translationentry", x => new { x.id, x.type, x.languagecode, x.fieldname });
                });

            migrationBuilder.CreateTable(
                name: "provider",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    refid = table.Column<string>(type: "text", nullable: true),
                    logourl = table.Column<string>(type: "text", nullable: true),
                    code = table.Column<string>(type: "text", nullable: true),
                    typeid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_provider", x => x.id);
                    table.ForeignKey(
                        name: "fk_provider_providertype_typeid",
                        column: x => x.typeid,
                        principalSchema: "dbo",
                        principalTable: "providertype",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "entitytype",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    providerid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entitytype", x => x.id);
                    table.ForeignKey(
                        name: "fk_entitytype_provider_providerid",
                        column: x => x.providerid,
                        principalSchema: "dbo",
                        principalTable: "provider",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "resource",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    providerid = table.Column<Guid>(type: "uuid", nullable: false),
                    typeid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    refid = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resource", x => x.id);
                    table.ForeignKey(
                        name: "fk_resource_provider_providerid",
                        column: x => x.providerid,
                        principalSchema: "dbo",
                        principalTable: "provider",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_resource_resourcetype_typeid",
                        column: x => x.typeid,
                        principalSchema: "dbo",
                        principalTable: "resourcetype",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "areagroup",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    entitytypeid = table.Column<Guid>(type: "uuid", nullable: false),
                    urn = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_areagroup", x => x.id);
                    table.ForeignKey(
                        name: "fk_areagroup_entitytype_entitytypeid",
                        column: x => x.entitytypeid,
                        principalSchema: "dbo",
                        principalTable: "entitytype",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "entityvariant",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    typeid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entityvariant", x => x.id);
                    table.ForeignKey(
                        name: "fk_entityvariant_entitytype_typeid",
                        column: x => x.typeid,
                        principalSchema: "dbo",
                        principalTable: "entitytype",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    entitytypeid = table.Column<Guid>(type: "uuid", nullable: true),
                    providerid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    code = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    iskeyrole = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    urn = table.Column<string>(type: "text", nullable: false),
                    isassignable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_role", x => x.id);
                    table.ForeignKey(
                        name: "fk_role_entitytype_entitytypeid",
                        column: x => x.entitytypeid,
                        principalSchema: "dbo",
                        principalTable: "entitytype",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_role_provider_providerid",
                        column: x => x.providerid,
                        principalSchema: "dbo",
                        principalTable: "provider",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "area",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    iconurl = table.Column<string>(type: "text", nullable: true),
                    groupid = table.Column<Guid>(type: "uuid", nullable: false),
                    urn = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_area", x => x.id);
                    table.ForeignKey(
                        name: "fk_area_areagroup_groupid",
                        column: x => x.groupid,
                        principalSchema: "dbo",
                        principalTable: "areagroup",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entity",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    typeid = table.Column<Guid>(type: "uuid", nullable: false),
                    variantid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    refid = table.Column<string>(type: "text", nullable: true),
                    parentid = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entity", x => x.id);
                    table.ForeignKey(
                        name: "fk_entity_entity_parentid",
                        column: x => x.parentid,
                        principalSchema: "dbo",
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_entity_entitytype_typeid",
                        column: x => x.typeid,
                        principalSchema: "dbo",
                        principalTable: "entitytype",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_entity_entityvariant_variantid",
                        column: x => x.variantid,
                        principalSchema: "dbo",
                        principalTable: "entityvariant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "entityvariantrole",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    variantid = table.Column<Guid>(type: "uuid", nullable: false),
                    roleid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entityvariantrole", x => x.id);
                    table.ForeignKey(
                        name: "fk_entityvariantrole_entityvariant_variantid",
                        column: x => x.variantid,
                        principalSchema: "dbo",
                        principalTable: "entityvariant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_entityvariantrole_role_roleid",
                        column: x => x.roleid,
                        principalSchema: "dbo",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rolelookup",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    roleid = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rolelookup", x => x.id);
                    table.ForeignKey(
                        name: "fk_rolelookup_role_roleid",
                        column: x => x.roleid,
                        principalSchema: "dbo",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rolemap",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    hasroleid = table.Column<Guid>(type: "uuid", nullable: false),
                    getroleid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rolemap", x => x.id);
                    table.ForeignKey(
                        name: "fk_rolemap_role_getroleid",
                        column: x => x.getroleid,
                        principalSchema: "dbo",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rolemap_role_hasroleid",
                        column: x => x.hasroleid,
                        principalSchema: "dbo",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roleresource",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    roleid = table.Column<Guid>(type: "uuid", nullable: false),
                    resourceid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roleresource", x => x.id);
                    table.ForeignKey(
                        name: "fk_roleresource_resource_resourceid",
                        column: x => x.resourceid,
                        principalSchema: "dbo",
                        principalTable: "resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_roleresource_role_roleid",
                        column: x => x.roleid,
                        principalSchema: "dbo",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "package",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    providerid = table.Column<Guid>(type: "uuid", nullable: false),
                    entitytypeid = table.Column<Guid>(type: "uuid", nullable: false),
                    areaid = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    isassignable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    isdelegable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    hasresources = table.Column<bool>(type: "boolean", nullable: false),
                    urn = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_package", x => x.id);
                    table.ForeignKey(
                        name: "fk_package_area_areaid",
                        column: x => x.areaid,
                        principalSchema: "dbo",
                        principalTable: "area",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_package_entitytype_entitytypeid",
                        column: x => x.entitytypeid,
                        principalSchema: "dbo",
                        principalTable: "entitytype",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_package_provider_providerid",
                        column: x => x.providerid,
                        principalSchema: "dbo",
                        principalTable: "provider",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assignment",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    roleid = table.Column<Guid>(type: "uuid", nullable: false),
                    fromid = table.Column<Guid>(type: "uuid", nullable: false),
                    toid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignment", x => x.id);
                    table.ForeignKey(
                        name: "fk_assignment_entity_fromid",
                        column: x => x.fromid,
                        principalSchema: "dbo",
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_assignment_entity_toid",
                        column: x => x.toid,
                        principalSchema: "dbo",
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_assignment_role_roleid",
                        column: x => x.roleid,
                        principalSchema: "dbo",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "entitylookup",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    entityid = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false),
                    isprotected = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_entitylookup", x => x.id);
                    table.ForeignKey(
                        name: "fk_entitylookup_entity_entityid",
                        column: x => x.entityid,
                        principalSchema: "dbo",
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "connections",
                columns: table => new
                {
                    fromid = table.Column<Guid>(type: "uuid", nullable: false),
                    roleid = table.Column<Guid>(type: "uuid", nullable: false),
                    toid = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "text", nullable: false),
                    viaid = table.Column<Guid>(type: "uuid", nullable: true),
                    viaroleid = table.Column<Guid>(type: "uuid", nullable: true),
                    packageid = table.Column<Guid>(type: "uuid", nullable: true),
                    resourceid = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_connections", x => new { x.fromid, x.toid, x.roleid, x.reason });
                    table.ForeignKey(
                        name: "fk_connections_entity_fromid",
                        column: x => x.fromid,
                        principalSchema: "dbo",
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_connections_entity_toid",
                        column: x => x.toid,
                        principalSchema: "dbo",
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_connections_entity_viaid",
                        column: x => x.viaid,
                        principalSchema: "dbo",
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_connections_package_packageid",
                        column: x => x.packageid,
                        principalSchema: "dbo",
                        principalTable: "package",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_connections_resource_resourceid",
                        column: x => x.resourceid,
                        principalSchema: "dbo",
                        principalTable: "resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_connections_role_roleid",
                        column: x => x.roleid,
                        principalSchema: "dbo",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_connections_role_viaroleid",
                        column: x => x.viaroleid,
                        principalSchema: "dbo",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "packageresource",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    packageid = table.Column<Guid>(type: "uuid", nullable: false),
                    resourceid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_packageresource", x => x.id);
                    table.ForeignKey(
                        name: "fk_packageresource_package_packageid",
                        column: x => x.packageid,
                        principalSchema: "dbo",
                        principalTable: "package",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_packageresource_resource_resourceid",
                        column: x => x.resourceid,
                        principalSchema: "dbo",
                        principalTable: "resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rolepackage",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    roleid = table.Column<Guid>(type: "uuid", nullable: false),
                    packageid = table.Column<Guid>(type: "uuid", nullable: false),
                    entityvariantid = table.Column<Guid>(type: "uuid", nullable: true),
                    hasaccess = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    canassign = table.Column<bool>(type: "boolean", nullable: false),
                    candelegate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rolepackage", x => x.id);
                    table.ForeignKey(
                        name: "fk_rolepackage_entityvariant_entityvariantid",
                        column: x => x.entityvariantid,
                        principalSchema: "dbo",
                        principalTable: "entityvariant",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rolepackage_package_packageid",
                        column: x => x.packageid,
                        principalSchema: "dbo",
                        principalTable: "package",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rolepackage_role_roleid",
                        column: x => x.roleid,
                        principalSchema: "dbo",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assignmentpackage",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assignmentid = table.Column<Guid>(type: "uuid", nullable: false),
                    packageid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignmentpackage", x => x.id);
                    table.ForeignKey(
                        name: "fk_assignmentpackage_assignment_assignmentid",
                        column: x => x.assignmentid,
                        principalSchema: "dbo",
                        principalTable: "assignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_assignmentpackage_package_packageid",
                        column: x => x.packageid,
                        principalSchema: "dbo",
                        principalTable: "package",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "assignmentresource",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assignmentid = table.Column<Guid>(type: "uuid", nullable: false),
                    resourceid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignmentresource", x => x.id);
                    table.ForeignKey(
                        name: "fk_assignmentresource_assignment_assignmentid",
                        column: x => x.assignmentid,
                        principalSchema: "dbo",
                        principalTable: "assignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_assignmentresource_resource_resourceid",
                        column: x => x.resourceid,
                        principalSchema: "dbo",
                        principalTable: "resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "delegation",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fromid = table.Column<Guid>(type: "uuid", nullable: false),
                    toid = table.Column<Guid>(type: "uuid", nullable: false),
                    facilitatorid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delegation", x => x.id);
                    table.ForeignKey(
                        name: "fk_delegation_assignment_fromid",
                        column: x => x.fromid,
                        principalSchema: "dbo",
                        principalTable: "assignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_delegation_assignment_toid",
                        column: x => x.toid,
                        principalSchema: "dbo",
                        principalTable: "assignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_delegation_entity_facilitatorid",
                        column: x => x.facilitatorid,
                        principalSchema: "dbo",
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "delegationpackage",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    delegationid = table.Column<Guid>(type: "uuid", nullable: false),
                    packageid = table.Column<Guid>(type: "uuid", nullable: false),
                    rolepackageid = table.Column<Guid>(type: "uuid", nullable: true),
                    assignmentpackageid = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delegationpackage", x => x.id);
                    table.ForeignKey(
                        name: "fk_delegationpackage_delegation_delegationid",
                        column: x => x.delegationid,
                        principalSchema: "dbo",
                        principalTable: "delegation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_delegationpackage_package_packageid",
                        column: x => x.packageid,
                        principalSchema: "dbo",
                        principalTable: "package",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "delegationresource",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    delegationid = table.Column<Guid>(type: "uuid", nullable: false),
                    resourceid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delegationresource", x => x.id);
                    table.ForeignKey(
                        name: "fk_delegationresource_delegation_delegationid",
                        column: x => x.delegationid,
                        principalSchema: "dbo",
                        principalTable: "delegation",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_delegationresource_resource_resourceid",
                        column: x => x.resourceid,
                        principalSchema: "dbo",
                        principalTable: "resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_area_groupid",
                schema: "dbo",
                table: "area",
                column: "groupid");

            migrationBuilder.CreateIndex(
                name: "ix_area_name",
                schema: "dbo",
                table: "area",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_areagroup_entitytypeid",
                schema: "dbo",
                table: "areagroup",
                column: "entitytypeid");

            migrationBuilder.CreateIndex(
                name: "ix_areagroup_name",
                schema: "dbo",
                table: "areagroup",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assignment_fromid",
                schema: "dbo",
                table: "assignment",
                column: "fromid");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_fromid_toid_roleid",
                schema: "dbo",
                table: "assignment",
                columns: new[] { "fromid", "toid", "roleid" },
                unique: true)
                .Annotation("Npgsql:IndexInclude", new[] { "id" });

            migrationBuilder.CreateIndex(
                name: "ix_assignment_roleid",
                schema: "dbo",
                table: "assignment",
                column: "roleid");

            migrationBuilder.CreateIndex(
                name: "ix_assignment_toid",
                schema: "dbo",
                table: "assignment",
                column: "toid");

            migrationBuilder.CreateIndex(
                name: "ix_assignmentpackage_assignmentid",
                schema: "dbo",
                table: "assignmentpackage",
                column: "assignmentid");

            migrationBuilder.CreateIndex(
                name: "ix_assignmentpackage_assignmentid_packageid",
                schema: "dbo",
                table: "assignmentpackage",
                columns: new[] { "assignmentid", "packageid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assignmentpackage_packageid",
                schema: "dbo",
                table: "assignmentpackage",
                column: "packageid");

            migrationBuilder.CreateIndex(
                name: "ix_assignmentresource_assignmentid",
                schema: "dbo",
                table: "assignmentresource",
                column: "assignmentid");

            migrationBuilder.CreateIndex(
                name: "ix_assignmentresource_assignmentid_resourceid",
                schema: "dbo",
                table: "assignmentresource",
                columns: new[] { "assignmentid", "resourceid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assignmentresource_resourceid",
                schema: "dbo",
                table: "assignmentresource",
                column: "resourceid");

            migrationBuilder.CreateIndex(
                name: "ix_connections_fromid",
                table: "connections",
                column: "fromid");

            migrationBuilder.CreateIndex(
                name: "ix_connections_packageid",
                table: "connections",
                column: "packageid");

            migrationBuilder.CreateIndex(
                name: "ix_connections_resourceid",
                table: "connections",
                column: "resourceid");

            migrationBuilder.CreateIndex(
                name: "ix_connections_roleid",
                table: "connections",
                column: "roleid");

            migrationBuilder.CreateIndex(
                name: "ix_connections_toid",
                table: "connections",
                column: "toid");

            migrationBuilder.CreateIndex(
                name: "ix_connections_viaid",
                table: "connections",
                column: "viaid");

            migrationBuilder.CreateIndex(
                name: "ix_connections_viaroleid",
                table: "connections",
                column: "viaroleid");

            migrationBuilder.CreateIndex(
                name: "ix_delegation_facilitatorid",
                schema: "dbo",
                table: "delegation",
                column: "facilitatorid");

            migrationBuilder.CreateIndex(
                name: "ix_delegation_fromid",
                schema: "dbo",
                table: "delegation",
                column: "fromid");

            migrationBuilder.CreateIndex(
                name: "ix_delegation_fromid_toid_facilitatorid",
                schema: "dbo",
                table: "delegation",
                columns: new[] { "fromid", "toid", "facilitatorid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_delegation_toid",
                schema: "dbo",
                table: "delegation",
                column: "toid");

            migrationBuilder.CreateIndex(
                name: "ix_delegationpackage_delegationid",
                schema: "dbo",
                table: "delegationpackage",
                column: "delegationid");

            migrationBuilder.CreateIndex(
                name: "ix_delegationpackage_delegationid_packageid",
                schema: "dbo",
                table: "delegationpackage",
                columns: new[] { "delegationid", "packageid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_delegationpackage_packageid",
                schema: "dbo",
                table: "delegationpackage",
                column: "packageid");

            migrationBuilder.CreateIndex(
                name: "ix_delegationresource_delegationid",
                schema: "dbo",
                table: "delegationresource",
                column: "delegationid");

            migrationBuilder.CreateIndex(
                name: "ix_delegationresource_delegationid_resourceid",
                schema: "dbo",
                table: "delegationresource",
                columns: new[] { "delegationid", "resourceid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_delegationresource_resourceid",
                schema: "dbo",
                table: "delegationresource",
                column: "resourceid");

            migrationBuilder.CreateIndex(
                name: "ix_entity_name_refid_typeid_variantid",
                schema: "dbo",
                table: "entity",
                columns: new[] { "name", "refid", "typeid", "variantid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_entity_parentid",
                schema: "dbo",
                table: "entity",
                column: "parentid");

            migrationBuilder.CreateIndex(
                name: "ix_entity_typeid",
                schema: "dbo",
                table: "entity",
                column: "typeid");

            migrationBuilder.CreateIndex(
                name: "ix_entity_variantid",
                schema: "dbo",
                table: "entity",
                column: "variantid");

            migrationBuilder.CreateIndex(
                name: "ix_entitylookup_entityid",
                schema: "dbo",
                table: "entitylookup",
                column: "entityid");

            migrationBuilder.CreateIndex(
                name: "ix_entitylookup_entityid_key",
                schema: "dbo",
                table: "entitylookup",
                columns: new[] { "entityid", "key" },
                unique: true)
                .Annotation("Npgsql:IndexInclude", new[] { "value", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_entitytype_name_providerid",
                schema: "dbo",
                table: "entitytype",
                columns: new[] { "name", "providerid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_entitytype_providerid",
                schema: "dbo",
                table: "entitytype",
                column: "providerid");

            migrationBuilder.CreateIndex(
                name: "ix_entityvariant_name_typeid",
                schema: "dbo",
                table: "entityvariant",
                columns: new[] { "name", "typeid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_entityvariant_typeid",
                schema: "dbo",
                table: "entityvariant",
                column: "typeid");

            migrationBuilder.CreateIndex(
                name: "ix_entityvariantrole_roleid",
                schema: "dbo",
                table: "entityvariantrole",
                column: "roleid");

            migrationBuilder.CreateIndex(
                name: "ix_entityvariantrole_variantid",
                schema: "dbo",
                table: "entityvariantrole",
                column: "variantid");

            migrationBuilder.CreateIndex(
                name: "ix_entityvariantrole_variantid_roleid",
                schema: "dbo",
                table: "entityvariantrole",
                columns: new[] { "variantid", "roleid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_package_areaid",
                schema: "dbo",
                table: "package",
                column: "areaid");

            migrationBuilder.CreateIndex(
                name: "ix_package_entitytypeid",
                schema: "dbo",
                table: "package",
                column: "entitytypeid");

            migrationBuilder.CreateIndex(
                name: "ix_package_providerid",
                schema: "dbo",
                table: "package",
                column: "providerid");

            migrationBuilder.CreateIndex(
                name: "ix_package_providerid_name",
                schema: "dbo",
                table: "package",
                columns: new[] { "providerid", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_packageresource_packageid",
                schema: "dbo",
                table: "packageresource",
                column: "packageid");

            migrationBuilder.CreateIndex(
                name: "ix_packageresource_packageid_resourceid",
                schema: "dbo",
                table: "packageresource",
                columns: new[] { "packageid", "resourceid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_packageresource_resourceid",
                schema: "dbo",
                table: "packageresource",
                column: "resourceid");

            migrationBuilder.CreateIndex(
                name: "ix_provider_name",
                schema: "dbo",
                table: "provider",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_provider_typeid",
                schema: "dbo",
                table: "provider",
                column: "typeid");

            migrationBuilder.CreateIndex(
                name: "ix_providertype_name",
                schema: "dbo",
                table: "providertype",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_resource_providerid",
                schema: "dbo",
                table: "resource",
                column: "providerid");

            migrationBuilder.CreateIndex(
                name: "ix_resource_typeid",
                schema: "dbo",
                table: "resource",
                column: "typeid");

            migrationBuilder.CreateIndex(
                name: "ix_resourcetype_name",
                schema: "dbo",
                table: "resourcetype",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_entitytypeid",
                schema: "dbo",
                table: "role",
                column: "entitytypeid");

            migrationBuilder.CreateIndex(
                name: "ix_role_providerid",
                schema: "dbo",
                table: "role",
                column: "providerid");

            migrationBuilder.CreateIndex(
                name: "ix_role_providerid_code",
                schema: "dbo",
                table: "role",
                columns: new[] { "providerid", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_providerid_name",
                schema: "dbo",
                table: "role",
                columns: new[] { "providerid", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_urn",
                schema: "dbo",
                table: "role",
                column: "urn",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rolelookup_roleid",
                schema: "dbo",
                table: "rolelookup",
                column: "roleid");

            migrationBuilder.CreateIndex(
                name: "ix_rolelookup_roleid_key",
                schema: "dbo",
                table: "rolelookup",
                columns: new[] { "roleid", "key" },
                unique: true)
                .Annotation("Npgsql:IndexInclude", new[] { "value", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_rolemap_getroleid",
                schema: "dbo",
                table: "rolemap",
                column: "getroleid");

            migrationBuilder.CreateIndex(
                name: "ix_rolemap_hasroleid",
                schema: "dbo",
                table: "rolemap",
                column: "hasroleid");

            migrationBuilder.CreateIndex(
                name: "ix_rolemap_hasroleid_getroleid",
                schema: "dbo",
                table: "rolemap",
                columns: new[] { "hasroleid", "getroleid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rolepackage_entityvariantid",
                schema: "dbo",
                table: "rolepackage",
                column: "entityvariantid");

            migrationBuilder.CreateIndex(
                name: "ix_rolepackage_packageid",
                schema: "dbo",
                table: "rolepackage",
                column: "packageid");

            migrationBuilder.CreateIndex(
                name: "ix_rolepackage_roleid",
                schema: "dbo",
                table: "rolepackage",
                column: "roleid");

            migrationBuilder.CreateIndex(
                name: "ix_rolepackage_roleid_packageid",
                schema: "dbo",
                table: "rolepackage",
                columns: new[] { "roleid", "packageid" },
                unique: true,
                filter: "entityvariantid IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_rolepackage_roleid_packageid_entityvariantid",
                schema: "dbo",
                table: "rolepackage",
                columns: new[] { "roleid", "packageid", "entityvariantid" },
                unique: true,
                filter: "entityvariantid IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_roleresource_resourceid",
                schema: "dbo",
                table: "roleresource",
                column: "resourceid");

            migrationBuilder.CreateIndex(
                name: "ix_roleresource_roleid",
                schema: "dbo",
                table: "roleresource",
                column: "roleid");

            migrationBuilder.CreateIndex(
                name: "ix_roleresource_roleid_resourceid",
                schema: "dbo",
                table: "roleresource",
                columns: new[] { "roleid", "resourceid" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignmentpackage",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "assignmentresource",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "auditarea",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditareagroup",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditassignment",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditassignmentpackage",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditassignmentresource",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditdelegation",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditdelegationpackage",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditdelegationresource",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditentity",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditentitylookup",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditentitytype",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditentityvariant",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditentityvariantrole",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditpackage",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditpackageresource",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditprovider",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditprovidertype",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditresource",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditresourcetype",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditrole",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditrolelookup",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditrolemap",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditrolepackage",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditroleresource",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "connections");

            migrationBuilder.DropTable(
                name: "delegationpackage",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "delegationresource",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "entitylookup",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "entityvariantrole",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "packageresource",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "rolelookup",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "rolemap",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "rolepackage",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "roleresource",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "translationentry",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "delegation",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "package",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "resource",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "assignment",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "area",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "resourcetype",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "entity",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "role",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "areagroup",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "entityvariant",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "entitytype",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "provider",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "providertype",
                schema: "dbo");
        }
    }
}
