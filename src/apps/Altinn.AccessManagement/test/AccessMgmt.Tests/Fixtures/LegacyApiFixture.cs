using Altinn.AccessManagement.TestUtils.Fixtures;
using Microsoft.Extensions.Configuration;

namespace Altinn.AccessManagement.Tests.Fixtures;

/// <summary>
/// Variant of <see cref="ApiFixture"/> that stands up the <em>full</em> production
/// database schema — both the EF <c>dbo</c>/<c>dbo_history</c> schemas provisioned
/// by <see cref="Altinn.AccessManagement.TestUtils.Factories.EFPostgresFactory"/>
/// and the Yuniql <c>accessmanagement.*</c>, <c>consent.*</c>, and
/// <c>delegation.*</c> schemas (plus enum types) provisioned by the production
/// migration pipeline at host startup.
/// </summary>
/// <remarks>
/// <para>
/// Use this fixture for test classes that exercise code paths backed by the
/// still-extant Dapper repositories (<c>ResourceMetadataRepo</c>,
/// <c>ConsentRepository</c>, <c>DelegationMetadataRepo</c>) or any component
/// that depends on the Yuniql-provisioned enum types bound via
/// <c>PersistenceDependencyInjectionExtensions.AddDatabase</c>. For pure EF
/// tests, keep using <see cref="ApiFixture"/> directly — it boots faster
/// because it skips the Yuniql migration pipeline.
/// </para>
/// <para>
/// How it works: the base <see cref="ApiFixture"/> provisions a per-test
/// database that already has the EF schemas applied via
/// <see cref="Altinn.AccessManagement.TestUtils.Factories.EFPostgresFactory"/>.
/// This subclass additionally loads <c>appsettings.test.json</c> (for
/// non-database test configuration) and sets
/// <c>PostgreSQLSettings:EnableDBConnection=true</c> +
/// <c>RunIntegrationTests=true</c> — the same flags the legacy
/// <c>WebApplicationFixture</c> used — so the production
/// <c>AccessManagementHost.ConfigurePostgreSqlConfiguration</c> flips on the
/// <c>Altinn:Npgsql:*:Migrate:Enabled</c> switch that triggers Yuniql
/// migrations during host startup.
/// </para>
/// <para>
/// Currently the Yuniql pipeline runs once per test database (on host build),
/// not once per template. If that cost ever becomes a bottleneck, the
/// template-level alternative (Option 1 in the 16.4 investigation doc) can
/// be adopted without changing this fixture's public surface.
/// </para>
/// </remarks>
public class LegacyApiFixture : ApiFixture
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyApiFixture"/> class.
    /// </summary>
    public LegacyApiFixture()
    {
        WithAppsettings(builder =>
        {
            builder.AddJsonFile("appsettings.test.json", optional: false, reloadOnChange: false);
        });

        WithInMemoryAppsettings(dict =>
        {
            // Enables the production Yuniql migration pipeline at host startup
            // so the per-test database receives the legacy accessmanagement.*,
            // consent.*, and delegation.* schemas + enum types on top of the
            // EF-provisioned dbo schemas.
            dict["PostgreSQLSettings:EnableDBConnection"] = "true";
            dict["RunIntegrationTests"] = "true";
        });
    }
}
