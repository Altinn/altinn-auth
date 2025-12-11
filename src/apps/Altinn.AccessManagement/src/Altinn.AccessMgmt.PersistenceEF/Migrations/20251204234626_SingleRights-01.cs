using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class SingleRights01 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "policypath",
                schema: "dbo_history",
                table: "auditassignmentresource",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "policyversion",
                schema: "dbo_history",
                table: "auditassignmentresource",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "policypath",
                schema: "dbo",
                table: "assignmentresource",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "policyversion",
                schema: "dbo",
                table: "assignmentresource",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "policypath",
                schema: "dbo_history",
                table: "auditassignmentresource");

            migrationBuilder.DropColumn(
                name: "policyversion",
                schema: "dbo_history",
                table: "auditassignmentresource");

            migrationBuilder.DropColumn(
                name: "policypath",
                schema: "dbo",
                table: "assignmentresource");

            migrationBuilder.DropColumn(
                name: "policyversion",
                schema: "dbo",
                table: "assignmentresource");
        }
    }
}
