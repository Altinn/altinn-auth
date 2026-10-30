using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class ImportResourceRightDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "rightdescription",
                schema: "dbo",
                table: "resource",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rightdescription",
                schema: "dbo_history",
                table: "auditresource",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "rightdescription",
                schema: "dbo",
                table: "resource");

            migrationBuilder.DropColumn(
                name: "rightdescription",
                schema: "dbo_history",
                table: "auditresource");
        }
    }
}
