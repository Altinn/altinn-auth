using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class RequestedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "byid",
                schema: "dbo",
                table: "requestassignment",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "byid",
                schema: "dbo_history",
                table: "auditrequestassignment",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("""
            SET LOCAL app.changed_by = '0195efb8-7c80-7262-b616-7d9eb843bcaa';
            SET LOCAL app.changed_by_system = '0195efb8-7c80-7262-b616-7d9eb843bcaa';
            SET LOCAL app.change_operation_id = 'Add RequestedBy';
            UPDATE dbo.requestassignment SET byid = audit_changedby WHERE audit_changedby IS NOT NULL
            """);

            migrationBuilder.CreateIndex(
                name: "ix_requestassignment_byid",
                schema: "dbo",
                table: "requestassignment",
                column: "byid");

            migrationBuilder.AddForeignKey(
                name: "fk_requestassignment_entity_byid",
                schema: "dbo",
                table: "requestassignment",
                column: "byid",
                principalSchema: "dbo",
                principalTable: "entity",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_requestassignment_entity_byid",
                schema: "dbo",
                table: "requestassignment");

            migrationBuilder.DropIndex(
                name: "ix_requestassignment_byid",
                schema: "dbo",
                table: "requestassignment");

            migrationBuilder.DropColumn(
                name: "byid",
                schema: "dbo",
                table: "requestassignment");

            migrationBuilder.DropColumn(
                name: "byid",
                schema: "dbo_history",
                table: "auditrequestassignment");
        }
    }
}
