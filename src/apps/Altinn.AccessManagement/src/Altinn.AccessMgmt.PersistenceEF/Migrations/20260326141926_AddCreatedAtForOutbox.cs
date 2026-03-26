using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedAtForOutbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "handlermessage",
                schema: "dbo",
                table: "outboxmessage");

            migrationBuilder.AddColumn<DateTime>(
                name: "createdat",
                schema: "dbo",
                table: "outboxmessage",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "createdat",
                schema: "dbo",
                table: "outboxmessage");

            migrationBuilder.AddColumn<string>(
                name: "handlermessage",
                schema: "dbo",
                table: "outboxmessage",
                type: "text",
                nullable: true);
        }
    }
}
