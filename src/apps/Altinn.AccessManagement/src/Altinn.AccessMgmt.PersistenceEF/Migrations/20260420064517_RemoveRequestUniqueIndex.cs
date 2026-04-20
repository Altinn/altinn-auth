using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRequestUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_requestassignmentresource_assignmentid_resourceid_action_st~",
                schema: "dbo",
                table: "requestassignmentresource");

            migrationBuilder.DropIndex(
                name: "ix_requestassignmentpackage_assignmentid_packageid_status",
                schema: "dbo",
                table: "requestassignmentpackage");

            migrationBuilder.CreateIndex(
                name: "ix_requestassignmentresource_assignmentid_resourceid",
                schema: "dbo",
                table: "requestassignmentresource",
                columns: new[] { "assignmentid", "resourceid" })
                .Annotation("Npgsql:IndexInclude", new[] { "status" });

            migrationBuilder.CreateIndex(
                name: "ix_requestassignmentpackage_assignmentid_packageid",
                schema: "dbo",
                table: "requestassignmentpackage",
                columns: new[] { "assignmentid", "packageid" })
                .Annotation("Npgsql:IndexInclude", new[] { "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_requestassignmentresource_assignmentid_resourceid",
                schema: "dbo",
                table: "requestassignmentresource");

            migrationBuilder.DropIndex(
                name: "ix_requestassignmentpackage_assignmentid_packageid",
                schema: "dbo",
                table: "requestassignmentpackage");

            migrationBuilder.CreateIndex(
                name: "ix_requestassignmentresource_assignmentid_resourceid_action_st~",
                schema: "dbo",
                table: "requestassignmentresource",
                columns: new[] { "assignmentid", "resourceid", "action", "status" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_requestassignmentpackage_assignmentid_packageid_status",
                schema: "dbo",
                table: "requestassignmentpackage",
                columns: new[] { "assignmentid", "packageid", "status" },
                unique: true);
        }
    }
}
