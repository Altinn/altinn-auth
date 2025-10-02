using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class AddView : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "delegationid",
                table: "connections",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "packagedelegationchecks",
                columns: table => new
                {
                    packageid = table.Column<Guid>(type: "uuid", nullable: false),
                    packageurn = table.Column<string>(type: "text", nullable: true),
                    areaid = table.Column<Guid>(type: "uuid", nullable: false),
                    isassignable = table.Column<bool>(type: "boolean", nullable: false),
                    isdelegable = table.Column<bool>(type: "boolean", nullable: false),
                    roleid = table.Column<Guid>(type: "uuid", nullable: true),
                    roleurn = table.Column<string>(type: "text", nullable: true),
                    fromid = table.Column<Guid>(type: "uuid", nullable: true),
                    fromname = table.Column<string>(type: "text", nullable: true),
                    toid = table.Column<Guid>(type: "uuid", nullable: true),
                    toname = table.Column<string>(type: "text", nullable: true),
                    viaid = table.Column<Guid>(type: "uuid", nullable: true),
                    vianame = table.Column<string>(type: "text", nullable: true),
                    viaroleid = table.Column<Guid>(type: "uuid", nullable: true),
                    viaroleurn = table.Column<string>(type: "text", nullable: true),
                    hasaccess = table.Column<bool>(type: "boolean", nullable: true),
                    candelegate = table.Column<bool>(type: "boolean", nullable: true),
                    reason = table.Column<string>(type: "text", nullable: true),
                    isassignmentpackage = table.Column<bool>(type: "boolean", nullable: true),
                    isrolepackage = table.Column<bool>(type: "boolean", nullable: true),
                    iskeyrolepackage = table.Column<bool>(type: "boolean", nullable: true),
                    ismainunitpackage = table.Column<bool>(type: "boolean", nullable: true),
                    ismainadminpackage = table.Column<bool>(type: "boolean", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateIndex(
                name: "ix_connections_delegationid",
                table: "connections",
                column: "delegationid");

            migrationBuilder.AddForeignKey(
                name: "fk_connections_delegation_delegationid",
                table: "connections",
                column: "delegationid",
                principalSchema: "dbo",
                principalTable: "delegation",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_connections_delegation_delegationid",
                table: "connections");

            migrationBuilder.DropTable(
                name: "packagedelegationchecks");

            migrationBuilder.DropIndex(
                name: "ix_connections_delegationid",
                table: "connections");

            migrationBuilder.DropColumn(
                name: "delegationid",
                table: "connections");
        }
    }
}
