using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class DelegationIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_delegation_toid",
                schema: "dbo",
                table: "delegation");

            migrationBuilder.CreateIndex(
                name: "ix_delegation_toid",
                schema: "dbo",
                table: "delegation",
                column: "toid")
                .Annotation("Npgsql:IndexInclude", new[] { "id", "fromid" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_delegation_toid",
                schema: "dbo",
                table: "delegation");

            migrationBuilder.CreateIndex(
                name: "ix_delegation_toid",
                schema: "dbo",
                table: "delegation",
                column: "toid");
        }
    }
}
