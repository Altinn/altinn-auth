using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class AddDeletedAndDateOfDeathForEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "dateofdeath",
                schema: "dbo",
                table: "entity",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isdeleted",
                schema: "dbo",
                table: "entity",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateOnly>(
                name: "dateofdeath",
                schema: "dbo_history",
                table: "auditentity",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "isdeleted",
                schema: "dbo_history",
                table: "auditentity",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dateofdeath",
                schema: "dbo",
                table: "entity");

            migrationBuilder.DropColumn(
                name: "isdeleted",
                schema: "dbo",
                table: "entity");

            migrationBuilder.DropColumn(
                name: "dateofdeath",
                schema: "dbo_history",
                table: "auditentity");

            migrationBuilder.DropColumn(
                name: "isdeleted",
                schema: "dbo_history",
                table: "auditentity");
        }
    }
}
