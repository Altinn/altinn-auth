using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class PackageServiceOwnerAvailable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "isavailableforserviceowners",
                schema: "dbo",
                table: "role",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "code",
                schema: "dbo",
                table: "package",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isavailableforserviceowners",
                schema: "dbo_history",
                table: "auditrole",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "code",
                schema: "dbo_history",
                table: "auditpackage",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "isavailableforserviceowners",
                schema: "dbo",
                table: "role");

            migrationBuilder.DropColumn(
                name: "code",
                schema: "dbo",
                table: "package");

            migrationBuilder.DropColumn(
                name: "isavailableforserviceowners",
                schema: "dbo_history",
                table: "auditrole");

            migrationBuilder.DropColumn(
                name: "code",
                schema: "dbo_history",
                table: "auditpackage");
        }
    }
}
