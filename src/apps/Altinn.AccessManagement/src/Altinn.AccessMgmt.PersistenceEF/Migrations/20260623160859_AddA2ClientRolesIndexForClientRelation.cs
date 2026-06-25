using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class AddA2ClientRolesIndexForClientRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_a2clientrole_facilitatorid_fromid",
                schema: "dbo",
                table: "a2clientrole",
                columns: new[] { "facilitatorid", "fromid" })
                .Annotation("Npgsql:IndexInclude", new[] { "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_a2clientrole_facilitatorid_fromid",
                schema: "dbo",
                table: "a2clientrole");
        }
    }
}
