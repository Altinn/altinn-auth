using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class AddedUniqueWithFilterStatusPendingConstraintForOutboxMesssage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_outboxmessage_refid",
                schema: "dbo",
                table: "outboxmessage");

            migrationBuilder.CreateIndex(
                name: "uq_outboxmessage_refid_pending",
                schema: "dbo",
                table: "outboxmessage",
                column: "refid",
                unique: true,
                filter: "status = 'Pending'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_outboxmessage_refid_pending",
                schema: "dbo",
                table: "outboxmessage");

            migrationBuilder.CreateIndex(
                name: "ix_outboxmessage_refid",
                schema: "dbo",
                table: "outboxmessage",
                column: "refid");
        }
    }
}
