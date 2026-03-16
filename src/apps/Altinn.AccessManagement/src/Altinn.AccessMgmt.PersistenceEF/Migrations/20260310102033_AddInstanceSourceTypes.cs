using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class AddInstanceSourceTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "instancesourcetype",
                schema: "dbo",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    audit_changedby = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changedbysystem = table.Column<Guid>(type: "uuid", nullable: true),
                    audit_changeoperation = table.Column<string>(type: "text", nullable: true),
                    audit_validfrom = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_instancesourcetype", x => x.id);
                });

            migrationBuilder.Sql("""
                SET LOCAL app.changed_by = '0195efb8-7c80-7262-b616-7d9eb843bcaa';
                SET LOCAL app.changed_by_system = 'f1be3999-68f6-4757-92b4-d3f3d33345e1';
                SET LOCAL app.change_operation_id = 'Add Instance Source Type';
                INSERT INTO dbo.instancesourcetype(id, audit_validfrom, name) values('019cd6c4-a340-776e-a63a-2370a05db6c7', '2026-03-10 12:37:44.540 +0100', 'Altinn App');
                INSERT INTO dbo.instancesourcetype(id, audit_validfrom, name) values('019cd6c4-a340-7f7a-94af-f1181ec4a132', '2026-03-10 12:37:44.540 +0100', 'Sluttbruker');
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "instancesourcetypeid",
                schema: "dbo_history",
                table: "auditassignmentinstance",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddColumn<Guid>(
                name: "instancesourcetypeid",
                schema: "dbo",
                table: "assignmentinstance",
                type: "uuid",
                nullable: false);

            migrationBuilder.AddForeignKey(
                name: "fk_assignmentinstance_instancesourcetype_instancesourcetypeid",
                schema: "dbo",
                table: "assignmentinstance",
                column: "instancesourcetypeid",
                principalSchema: "dbo",
                principalTable: "instancesourcetype",
                principalColumn: "id");

            migrationBuilder.CreateIndex(
                name: "ix_assignmentinstance_instancesourcetypeid",
                schema: "dbo",
                table: "assignmentinstance",
                column: "instancesourcetypeid");

            migrationBuilder.CreateIndex(
                name: "ix_instancesourcetype_name",
                schema: "dbo",
                table: "instancesourcetype",
                column: "name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_assignmentinstance_instancesourcetype_instancesourcetypeid",
                schema: "dbo",
                table: "assignmentinstance");

            migrationBuilder.DropTable(
                name: "instancesourcetype",
                schema: "dbo");

            migrationBuilder.DropIndex(
                name: "ix_assignmentinstance_instancesourcetypeid",
                schema: "dbo",
                table: "assignmentinstance");

            migrationBuilder.DropColumn(
                name: "instancesourcetypeid",
                schema: "dbo_history",
                table: "auditassignmentinstance");

            migrationBuilder.DropColumn(
                name: "instancesourcetypeid",
                schema: "dbo",
                table: "assignmentinstance");
        }
    }
}
