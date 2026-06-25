using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class AddA2ClientRolesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "a2clientrole",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    fromid = table.Column<Guid>(type: "uuid", nullable: false),
                    toid = table.Column<Guid>(type: "uuid", nullable: false),
                    facilitatorid = table.Column<Guid>(type: "uuid", nullable: false),
                    performedby = table.Column<Guid>(type: "uuid", nullable: false),
                    rolecode = table.Column<string>(type: "text", nullable: true),
                    createddate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_a2clientrole", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "a2clientrole",
                schema: "dbo");
        }
    }
}
