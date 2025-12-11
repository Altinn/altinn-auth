using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class AddLegacyDelegationChangeId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "delegationchangeid",
                schema: "dbo_history",
                table: "auditassignmentresource",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "delegationchangeid",
                schema: "dbo",
                table: "assignmentresource",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "assignmentinstance",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assignmentid = table.Column<Guid>(type: "uuid", nullable: false),
                    resourceid = table.Column<Guid>(type: "uuid", nullable: false),
                    policypath = table.Column<string>(type: "text", nullable: true),
                    policyversion = table.Column<string>(type: "text", nullable: true),
                    instanceid = table.Column<string>(type: "text", nullable: true),
                    delegationchangeid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assignmentinstance", x => x.id);
                    table.ForeignKey(
                        name: "fk_assignmentinstance_assignment_assignmentid",
                        column: x => x.assignmentid,
                        principalSchema: "dbo",
                        principalTable: "assignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_assignmentinstance_resource_resourceid",
                        column: x => x.resourceid,
                        principalSchema: "dbo",
                        principalTable: "resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "auditassignmentinstance",
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
                    resourceid = table.Column<Guid>(type: "uuid", nullable: false),
                    policypath = table.Column<string>(type: "text", nullable: true),
                    policyversion = table.Column<string>(type: "text", nullable: true),
                    instanceid = table.Column<string>(type: "text", nullable: true),
                    delegationchangeid = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditassignmentinstance", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateIndex(
                name: "ix_assignmentinstance_assignmentid",
                schema: "dbo",
                table: "assignmentinstance",
                column: "assignmentid");

            migrationBuilder.CreateIndex(
                name: "ix_assignmentinstance_assignmentid_resourceid_instanceid",
                schema: "dbo",
                table: "assignmentinstance",
                columns: new[] { "assignmentid", "resourceid", "instanceid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assignmentinstance_resourceid",
                schema: "dbo",
                table: "assignmentinstance",
                column: "resourceid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "assignmentinstance",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "auditassignmentinstance",
                schema: "dbo_history");

            migrationBuilder.DropColumn(
                name: "delegationchangeid",
                schema: "dbo_history",
                table: "auditassignmentresource");

            migrationBuilder.DropColumn(
                name: "delegationchangeid",
                schema: "dbo",
                table: "assignmentresource");
        }
    }
}
