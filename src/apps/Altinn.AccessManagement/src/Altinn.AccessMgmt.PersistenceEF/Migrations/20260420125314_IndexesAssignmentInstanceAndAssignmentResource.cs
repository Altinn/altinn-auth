using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class IndexesAssignmentInstanceAndAssignmentResource : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_assignmentresource_assignmentid",
                schema: "dbo",
                table: "assignmentresource");

            migrationBuilder.DropIndex(
                name: "ix_assignmentinstance_assignmentid",
                schema: "dbo",
                table: "assignmentinstance");

            migrationBuilder.CreateIndex(
                name: "ix_assignmentresource_assignmentid",
                schema: "dbo",
                table: "assignmentresource",
                column: "assignmentid")
                .Annotation("Npgsql:IndexInclude", new[] { "id", "resourceid" });

            migrationBuilder.CreateIndex(
                name: "ix_assignmentinstance_assignmentid",
                schema: "dbo",
                table: "assignmentinstance",
                column: "assignmentid")
                .Annotation("Npgsql:IndexInclude", new[] { "id", "resourceid", "instanceid" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_assignmentresource_assignmentid",
                schema: "dbo",
                table: "assignmentresource");

            migrationBuilder.DropIndex(
                name: "ix_assignmentinstance_assignmentid",
                schema: "dbo",
                table: "assignmentinstance");

            migrationBuilder.CreateIndex(
                name: "ix_assignmentresource_assignmentid",
                schema: "dbo",
                table: "assignmentresource",
                column: "assignmentid");

            migrationBuilder.CreateIndex(
                name: "ix_assignmentinstance_assignmentid",
                schema: "dbo",
                table: "assignmentinstance",
                column: "assignmentid");
        }
    }
}
