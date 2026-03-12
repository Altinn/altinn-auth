using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class Requests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "auditrequestassignment",
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
                    roleid = table.Column<Guid>(type: "uuid", nullable: false),
                    fromid = table.Column<Guid>(type: "uuid", nullable: false),
                    toid = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditrequestassignment", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditrequestassignmentpackage",
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
                    assignmentid = table.Column<Guid>(type: "uuid", nullable: false),
                    packageid = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditrequestassignmentpackage", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "auditrequestassignmentresource",
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
                    assignmentid = table.Column<Guid>(type: "uuid", nullable: false),
                    resourceid = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_auditrequestassignmentresource", x => new { x.id, x.audit_validfrom, x.audit_validto });
                });

            migrationBuilder.CreateTable(
                name: "requestassignment",
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
                    table.PrimaryKey("pk_requestassignment", x => x.id);
                    table.ForeignKey(
                        name: "fk_requestassignment_entity_fromid",
                        column: x => x.fromid,
                        principalSchema: "dbo",
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_requestassignment_entity_toid",
                        column: x => x.toid,
                        principalSchema: "dbo",
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_requestassignment_role_roleid",
                        column: x => x.roleid,
                        principalSchema: "dbo",
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "requestassignmentpackage",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    assignmentid = table.Column<Guid>(type: "uuid", nullable: false),
                    packageid = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_requestassignmentpackage", x => x.id);
                    table.ForeignKey(
                        name: "fk_requestassignmentpackage_package_packageid",
                        column: x => x.packageid,
                        principalSchema: "dbo",
                        principalTable: "package",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_requestassignmentpackage_requestassignment_assignmentid",
                        column: x => x.assignmentid,
                        principalSchema: "dbo",
                        principalTable: "requestassignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "requestassignmentresource",
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
                    action = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_requestassignmentresource", x => x.id);
                    table.ForeignKey(
                        name: "fk_requestassignmentresource_requestassignment_assignmentid",
                        column: x => x.assignmentid,
                        principalSchema: "dbo",
                        principalTable: "requestassignment",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_requestassignmentresource_resource_resourceid",
                        column: x => x.resourceid,
                        principalSchema: "dbo",
                        principalTable: "resource",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_requestassignment_fromid",
                schema: "dbo",
                table: "requestassignment",
                column: "fromid");

            migrationBuilder.CreateIndex(
                name: "ix_requestassignment_roleid",
                schema: "dbo",
                table: "requestassignment",
                column: "roleid");

            migrationBuilder.CreateIndex(
                name: "ix_requestassignment_toid",
                schema: "dbo",
                table: "requestassignment",
                column: "toid");

            migrationBuilder.CreateIndex(
                name: "ix_requestassignmentpackage_assignmentid",
                schema: "dbo",
                table: "requestassignmentpackage",
                column: "assignmentid");

            migrationBuilder.CreateIndex(
                name: "ix_requestassignmentpackage_assignmentid_packageid_status",
                schema: "dbo",
                table: "requestassignmentpackage",
                columns: new[] { "assignmentid", "packageid", "status" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_requestassignmentpackage_packageid",
                schema: "dbo",
                table: "requestassignmentpackage",
                column: "packageid");

            migrationBuilder.CreateIndex(
                name: "ix_requestassignmentresource_assignmentid",
                schema: "dbo",
                table: "requestassignmentresource",
                column: "assignmentid");

            migrationBuilder.CreateIndex(
                name: "ix_requestassignmentresource_assignmentid_resourceid_action_st~",
                schema: "dbo",
                table: "requestassignmentresource",
                columns: new[] { "assignmentid", "resourceid", "action", "status" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_requestassignmentresource_resourceid",
                schema: "dbo",
                table: "requestassignmentresource",
                column: "resourceid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "auditrequestassignment",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditrequestassignmentpackage",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "auditrequestassignmentresource",
                schema: "dbo_history");

            migrationBuilder.DropTable(
                name: "requestassignmentpackage",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "requestassignmentresource",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "requestassignment",
                schema: "dbo");
        }
    }
}
