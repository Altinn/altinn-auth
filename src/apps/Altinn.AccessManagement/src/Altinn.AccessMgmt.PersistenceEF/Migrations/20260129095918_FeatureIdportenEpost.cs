using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class FeatureIdportenEpost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "emailidentifier",
                schema: "dbo",
                table: "entity",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "emailidentifier",
                schema: "dbo_history",
                table: "auditentity",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "emailidentifier",
                schema: "dbo",
                table: "entity");

            migrationBuilder.DropColumn(
                name: "emailidentifier",
                schema: "dbo_history",
                table: "auditentity");
        }
    }
}
