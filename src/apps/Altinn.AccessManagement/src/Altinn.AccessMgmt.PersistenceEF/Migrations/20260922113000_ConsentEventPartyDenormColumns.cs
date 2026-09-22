using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Altinn.AccessMgmt.PersistenceEF.Migrations
{
    /// <summary>
    /// Denormalizes the two party columns the consent events feed filters on
    /// (<c>topartyuuid</c> and <c>handledbypartyuuid</c>) from <c>consent.consentrequest</c> onto
    /// <c>consent.consentevent</c>, so <c>GetConsentEventsForParty</c> can filter + order + paginate
    /// on the event table alone once the rows are backfilled. Adding nullable columns is a
    /// metadata-only change (no table rewrite). The consent schema is provisioned by raw SQL
    /// (<c>ConsentSchema.sql</c>) rather than modelled as EF entities, so the columns are added with
    /// SQL here and mirrored in that script for fresh provisioning. The supporting feed indexes are
    /// built CONCURRENTLY outside this transactional migration — see the rollout runbook.
    /// </summary>
    public partial class ConsentEventPartyDenormColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE consent.consentevent ADD COLUMN IF NOT EXISTS topartyuuid uuid;
                ALTER TABLE consent.consentevent ADD COLUMN IF NOT EXISTS handledbypartyuuid uuid;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE consent.consentevent DROP COLUMN IF EXISTS handledbypartyuuid;
                ALTER TABLE consent.consentevent DROP COLUMN IF EXISTS topartyuuid;
                """);
        }
    }
}
