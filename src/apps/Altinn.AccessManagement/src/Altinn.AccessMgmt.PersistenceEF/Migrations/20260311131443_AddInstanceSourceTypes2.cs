using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class AddInstanceSourceTypes2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "instancesourcetypeid",
                schema: "dbo",
                table: "assignmentinstance",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("019cd6c4-a340-776e-a63a-2370a05db6c7"),
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "instancesourcetypeid",
                schema: "dbo",
                table: "assignmentinstance",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValue: new Guid("019cd6c4-a340-776e-a63a-2370a05db6c7"));
        }
    }
}
