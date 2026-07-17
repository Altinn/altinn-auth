using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class AddedAssignmentResourceFKToDelegationResourceAndCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "assignmentresourceid",
                schema: "dbo",
                table: "delegationresource",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "assignmentresourceid",
                schema: "dbo_history",
                table: "auditdelegationresource",
                type: "uuid",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "ix_delegationresource_assignmentresourceid",
                schema: "dbo",
                table: "delegationresource",
                column: "assignmentresourceid");

            migrationBuilder.AddForeignKey(
                name: "fk_delegationresource_assignmentresource_assignmentresourceid",
                schema: "dbo",
                table: "delegationresource",
                column: "assignmentresourceid",
                principalSchema: "dbo",
                principalTable: "assignmentresource",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_delegationresource_assignmentresource_assignmentresourceid",
                schema: "dbo",
                table: "delegationresource");

            migrationBuilder.DropIndex(
                name: "ix_delegationresource_assignmentresourceid",
                schema: "dbo",
                table: "delegationresource");

            migrationBuilder.DropColumn(
                name: "assignmentresourceid",
                schema: "dbo",
                table: "delegationresource");

            migrationBuilder.DropColumn(
                name: "assignmentresourceid",
                schema: "dbo_history",
                table: "auditdelegationresource");
        }
    }
}
