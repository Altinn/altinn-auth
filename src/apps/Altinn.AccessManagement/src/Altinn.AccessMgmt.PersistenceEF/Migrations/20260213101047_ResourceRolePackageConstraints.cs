using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class ResourceRolePackageConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_role_providerid_code",
                schema: "dbo",
                table: "role");

            migrationBuilder.AlterColumn<string>(
                name: "refid",
                schema: "dbo",
                table: "resource",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "urn",
                schema: "dbo",
                table: "package",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "code",
                schema: "dbo",
                table: "package",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_role_code",
                schema: "dbo",
                table: "role",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_resource_refid",
                schema: "dbo",
                table: "resource",
                column: "refid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_package_code",
                schema: "dbo",
                table: "package",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_package_urn",
                schema: "dbo",
                table: "package",
                column: "urn",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_role_code",
                schema: "dbo",
                table: "role");

            migrationBuilder.DropIndex(
                name: "ix_resource_refid",
                schema: "dbo",
                table: "resource");

            migrationBuilder.DropIndex(
                name: "ix_package_code",
                schema: "dbo",
                table: "package");

            migrationBuilder.DropIndex(
                name: "ix_package_urn",
                schema: "dbo",
                table: "package");

            migrationBuilder.AlterColumn<string>(
                name: "refid",
                schema: "dbo",
                table: "resource",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "urn",
                schema: "dbo",
                table: "package",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                schema: "dbo",
                table: "package",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateIndex(
                name: "ix_role_providerid_code",
                schema: "dbo",
                table: "role",
                columns: new[] { "providerid", "code" },
                unique: true);
        }
    }
}
