using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class AddOutboxMessageLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "attempt",
                schema: "dbo",
                table: "outboxmessage",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "outboxmessagelog",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    outboxmessageid = table.Column<Guid>(type: "uuid", nullable: false),
                    log = table.Column<string>(type: "text", nullable: true),
                    attempt = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outboxmessagelog", x => x.id);
                    table.ForeignKey(
                        name: "fk_outboxmessagelog_outboxmessage_outboxmessageid",
                        column: x => x.outboxmessageid,
                        principalSchema: "dbo",
                        principalTable: "outboxmessage",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_outboxmessagelog_outboxmessageid",
                schema: "dbo",
                table: "outboxmessagelog",
                column: "outboxmessageid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outboxmessagelog",
                schema: "dbo");

            migrationBuilder.DropColumn(
                name: "attempt",
                schema: "dbo",
                table: "outboxmessage");
        }
    }
}
