using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <inheritdoc />
    public partial class EntityIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_entity_organizationidentifier",
                schema: "dbo",
                table: "entity",
                column: "organizationidentifier",
                unique: true,
                filter: "OrganizationIdentifier IS NOT NULL")
                .Annotation("Npgsql:CreatedConcurrently", true)
                .Annotation("Npgsql:IndexInclude", new[] { "id" });

            migrationBuilder.CreateIndex(
                name: "ix_entity_personidentifier",
                schema: "dbo",
                table: "entity",
                column: "personidentifier",
                unique: true,
                filter: "PersonIdentifier IS NOT NULL")
                .Annotation("Npgsql:CreatedConcurrently", true)
                .Annotation("Npgsql:IndexInclude", new[] { "id" });

            migrationBuilder.CreateIndex(
                name: "ix_entity_userid",
                schema: "dbo",
                table: "entity",
                column: "userid",
                unique: true,
                filter: "UserId IS NOT NULL")
                .Annotation("Npgsql:CreatedConcurrently", true)
                .Annotation("Npgsql:IndexInclude", new[] { "id" });

            migrationBuilder.CreateIndex(
                name: "ix_entity_username",
                schema: "dbo",
                table: "entity",
                column: "username",
                unique: true,
                filter: "Username IS NOT NULL")
                .Annotation("Npgsql:CreatedConcurrently", true)
                .Annotation("Npgsql:IndexInclude", new[] { "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_entity_organizationidentifier",
                schema: "dbo",
                table: "entity");

            migrationBuilder.DropIndex(
                name: "ix_entity_personidentifier",
                schema: "dbo",
                table: "entity");

            migrationBuilder.DropIndex(
                name: "ix_entity_userid",
                schema: "dbo",
                table: "entity");

            migrationBuilder.DropIndex(
                name: "ix_entity_username",
                schema: "dbo",
                table: "entity");
        }
    }
}
