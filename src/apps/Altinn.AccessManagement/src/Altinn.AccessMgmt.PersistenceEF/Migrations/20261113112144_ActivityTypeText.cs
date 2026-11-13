using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class ActivityTypeText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "text",
                schema: "dbo_history",
                table: "auditactivitytype",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "text",
                schema: "dbo",
                table: "activitytype",
                type: "text",
                nullable: false,
                defaultValue: string.Empty);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "text",
                schema: "dbo_history",
                table: "auditactivitytype");

            migrationBuilder.DropColumn(
                name: "text",
                schema: "dbo",
                table: "activitytype");
        }
    }
}
