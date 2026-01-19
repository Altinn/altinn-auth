using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class PackageConfigIndexUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_package_providerid_name",
                schema: "dbo",
                table: "package");

            migrationBuilder.CreateIndex(
                name: "ix_package_providerid_name_entitytypeid",
                schema: "dbo",
                table: "package",
                columns: new[] { "providerid", "name", "entitytypeid" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_package_providerid_name_entitytypeid",
                schema: "dbo",
                table: "package");

            migrationBuilder.CreateIndex(
                name: "ix_package_providerid_name",
                schema: "dbo",
                table: "package",
                columns: new[] { "providerid", "name" },
                unique: true);
        }
    }
}
