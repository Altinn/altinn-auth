using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class AssignmentChangedByEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_assignmentresource_audit_changedby",
                schema: "dbo",
                table: "assignmentresource",
                column: "audit_changedby");

            migrationBuilder.AddForeignKey(
                name: "fk_assignmentresource_entity_audit_changedby",
                schema: "dbo",
                table: "assignmentresource",
                column: "audit_changedby",
                principalSchema: "dbo",
                principalTable: "entity",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_assignmentresource_entity_audit_changedby",
                schema: "dbo",
                table: "assignmentresource");

            migrationBuilder.DropIndex(
                name: "ix_assignmentresource_audit_changedby",
                schema: "dbo",
                table: "assignmentresource");
        }
    }
}
