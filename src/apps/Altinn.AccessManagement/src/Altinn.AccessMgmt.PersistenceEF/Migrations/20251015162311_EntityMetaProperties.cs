using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class EntityMetaProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "dateofbirth",
                schema: "dbo",
                table: "entity",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deletedat",
                schema: "dbo",
                table: "entity",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "organizationidentifier",
                schema: "dbo",
                table: "entity",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "partyid",
                schema: "dbo",
                table: "entity",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "personidentifier",
                schema: "dbo",
                table: "entity",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "userid",
                schema: "dbo",
                table: "entity",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "username",
                schema: "dbo",
                table: "entity",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "dateofbirth",
                schema: "dbo_history",
                table: "auditentity",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "deletedat",
                schema: "dbo_history",
                table: "auditentity",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "organizationidentifier",
                schema: "dbo_history",
                table: "auditentity",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "partyid",
                schema: "dbo_history",
                table: "auditentity",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "personidentifier",
                schema: "dbo_history",
                table: "auditentity",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "userid",
                schema: "dbo_history",
                table: "auditentity",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "username",
                schema: "dbo_history",
                table: "auditentity",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "dateofbirth",
                schema: "dbo",
                table: "entity");

            migrationBuilder.DropColumn(
                name: "deletedat",
                schema: "dbo",
                table: "entity");

            migrationBuilder.DropColumn(
                name: "organizationidentifier",
                schema: "dbo",
                table: "entity");

            migrationBuilder.DropColumn(
                name: "partyid",
                schema: "dbo",
                table: "entity");

            migrationBuilder.DropColumn(
                name: "personidentifier",
                schema: "dbo",
                table: "entity");

            migrationBuilder.DropColumn(
                name: "userid",
                schema: "dbo",
                table: "entity");

            migrationBuilder.DropColumn(
                name: "username",
                schema: "dbo",
                table: "entity");

            migrationBuilder.DropColumn(
                name: "dateofbirth",
                schema: "dbo_history",
                table: "auditentity");

            migrationBuilder.DropColumn(
                name: "deletedat",
                schema: "dbo_history",
                table: "auditentity");

            migrationBuilder.DropColumn(
                name: "organizationidentifier",
                schema: "dbo_history",
                table: "auditentity");

            migrationBuilder.DropColumn(
                name: "partyid",
                schema: "dbo_history",
                table: "auditentity");

            migrationBuilder.DropColumn(
                name: "personidentifier",
                schema: "dbo_history",
                table: "auditentity");

            migrationBuilder.DropColumn(
                name: "userid",
                schema: "dbo_history",
                table: "auditentity");

            migrationBuilder.DropColumn(
                name: "username",
                schema: "dbo_history",
                table: "auditentity");
        }
    }
}
