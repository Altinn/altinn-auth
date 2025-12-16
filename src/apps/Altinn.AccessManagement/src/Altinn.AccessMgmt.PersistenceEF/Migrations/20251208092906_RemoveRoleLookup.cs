using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRoleLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditrolelookup",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "rolelookup",
                schema: "dbo");

            migrationBuilder.AddColumn<string>(
                name: "legacycode",
                schema: "dbo",
                table: "role",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "legacyurn",
                schema: "dbo",
                table: "role",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "legacycode",
                schema: "dbo_history",
                table: "auditrole",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "legacyurn",
                schema: "dbo_history",
                table: "auditrole",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "legacycode",
                schema: "dbo",
                table: "role");

            migrationBuilder.DropColumn(
                name: "legacyurn",
                schema: "dbo",
                table: "role");

            migrationBuilder.DropColumn(
                name: "legacycode",
                schema: "dbo_history",
                table: "auditrole");

            migrationBuilder.DropColumn(
                name: "legacyurn",
                schema: "dbo_history",
                table: "auditrole");

            migrationBuilder.CreateTable(
                name: "auditrolelookup",
                schema: "dbo_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_validto = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deleteoperation = table.Column<string>(type: "text", nullable: true),
                    audit_deletedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_deletedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    key = table.Column<string>(type: "text", nullable: true),
                    roleid = table.Column<Guid>(type: "uuid", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditrolelookup", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "rolelookup",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    roleid = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
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
        }
    }
}
