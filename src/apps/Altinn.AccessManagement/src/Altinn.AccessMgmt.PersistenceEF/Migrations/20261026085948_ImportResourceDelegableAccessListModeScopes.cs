using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class ImportResourceDelegableAccessListModeScopes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "accesslistmode",
                schema: "dbo",
                table: "resource",
                type: "text",
                nullable: false,
                defaultValue: "Disabled");

            migrationBuilder.AddColumn<bool>(
                name: "delegable",
                schema: "dbo",
                table: "resource",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "accesslistmode",
                schema: "dbo_history",
                table: "auditresource",
                type: "text",
                nullable: false,
                defaultValue: "Disabled");

            migrationBuilder.AddColumn<bool>(
                name: "delegable",
                schema: "dbo_history",
                table: "auditresource",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "auditresourcescope",
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
                    resourceid = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditresourcescope", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "resourcescope",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    resourceid = table.Column<Guid>(type: "uuid", nullable: false),
                    scope = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_resourcescope", x => x.id);
                    table.ForeignKey(
                        name: "fk_resourcescope_resource_resourceid",
                        column: x => x.resourceid,
                        principalSchema: "dbo",
                        principalTable: "resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_resourcescope_resourceid",
                schema: "dbo",
                table: "resourcescope",
                column: "resourceid");

            migrationBuilder.CreateIndex(
                name: "ix_resourcescope_resourceid_scope",
                schema: "dbo",
                table: "resourcescope",
                columns: new[] { "resourceid", "scope" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditresourcescope",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "resourcescope",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "accesslistmode",
                schema: "dbo",
                table: "resource");

            migrationBuilder.DropColumn(
                name: "delegable",
                schema: "dbo",
                table: "resource");

            migrationBuilder.DropColumn(
                name: "accesslistmode",
                schema: "dbo_history",
                table: "auditresource");

            migrationBuilder.DropColumn(
                name: "delegable",
                schema: "dbo_history",
                table: "auditresource");
        }
    }
}
