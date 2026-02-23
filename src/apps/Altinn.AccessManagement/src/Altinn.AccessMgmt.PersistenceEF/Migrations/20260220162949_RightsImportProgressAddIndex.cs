using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class RightsImportProgressAddIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_rightimportprogress_delegationchangeid_origintype",
                schema: "dbo",
                table: "rightimportprogress",
                columns: new[] { "delegationchangeid", "origintype" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_rightimportprogress_delegationchangeid_origintype",
                schema: "dbo",
                table: "rightimportprogress");
        }
    }
}
