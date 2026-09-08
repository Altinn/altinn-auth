using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <summary>
    /// Indexes the two party columns the consent events feed filters on. The consent schema is
    /// provisioned by raw SQL (<c>ConsentSchema.sql</c>) rather than modelled as EF entities, so
    /// the indexes are created with SQL here and mirrored in that script for fresh provisioning.
    /// </summary>
    public partial class ConsentRequestPartyIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS idx_consentrequest_topartyuuid ON consent.consentrequest USING btree (topartyuuid);
                CREATE INDEX IF NOT EXISTS idx_consentrequest_handledbypartyuuid ON consent.consentrequest USING btree (handledbypartyuuid);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS consent.idx_consentrequest_topartyuuid;
                DROP INDEX IF EXISTS consent.idx_consentrequest_handledbypartyuuid;
                """);
        }
    }
}
