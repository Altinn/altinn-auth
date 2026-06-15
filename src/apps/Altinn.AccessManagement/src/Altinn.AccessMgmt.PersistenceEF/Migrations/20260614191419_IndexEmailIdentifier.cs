using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class IndexEmailIdentifier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_entity_emailidentifier",
                schema: "dbo",
                table: "entity",
                column: "emailidentifier",
                unique: true,
                filter: "EmailIdentifier IS NOT NULL")
                .Annotation("Npgsql:CreatedConcurrently", true)
                .Annotation("Npgsql:IndexInclude", new[] { "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_entity_emailidentifier",
                schema: "dbo",
                table: "entity");
        }
    }
}
