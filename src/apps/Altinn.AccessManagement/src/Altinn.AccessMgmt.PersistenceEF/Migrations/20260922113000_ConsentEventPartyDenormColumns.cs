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
            // ADD COLUMN takes an ACCESS EXCLUSIVE lock on consentevent, and the init job applies this
            // while old pods still serve traffic. If a slow feed query holds the table, the ALTER — and
            // every read/insert queued behind it — would wait for that query to finish. lock_timeout
            // bounds the wait so the migration fails fast and is retried instead of stalling consent
            // writes. SET LOCAL scopes it to this migration's transaction.
            migrationBuilder.Sql(
                """
                SET LOCAL lock_timeout = '5s';
                ALTER TABLE consent.consentevent ADD COLUMN IF NOT EXISTS topartyuuid uuid;
                ALTER TABLE consent.consentevent ADD COLUMN IF NOT EXISTS handledbypartyuuid uuid;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty. On fresh databases the columns come from ConsentSchema.sql,
            // so dropping them here would leave the schema out of line with the baseline.
            // Leaving them on rollback is harmless: they are nullable and older code never writes them.
        }
    }
}
