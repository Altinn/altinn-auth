using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class AddCascadeDeleteForAssignmentPackageToDelegationPackage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_delegationpackage_assignmentpackageid",
                schema: "dbo",
                table: "delegationpackage",
                column: "assignmentpackageid");

            migrationBuilder.AddForeignKey(
                name: "fk_delegationpackage_assignmentpackage_assignmentpackageid",
                schema: "dbo",
                table: "delegationpackage",
                column: "assignmentpackageid",
                principalSchema: "dbo",
                principalTable: "assignmentpackage",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_delegationpackage_assignmentpackage_assignmentpackageid",
                schema: "dbo",
                table: "delegationpackage");

            migrationBuilder.DropIndex(
                name: "ix_delegationpackage_assignmentpackageid",
                schema: "dbo",
                table: "delegationpackage");
        }
    }
}
