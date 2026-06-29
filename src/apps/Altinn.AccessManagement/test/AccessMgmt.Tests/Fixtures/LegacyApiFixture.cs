using Altinn.AccessManagement.TestUtils.Fixtures;
using Microsoft.Extensions.Configuration;

namespace Altinn.AccessManagement.Tests.Fixtures;

/// <summary>
/// Variant of <see cref="ApiFixture"/> that stands up the <em>full</em> production
/// database schema — the EF <c>dbo</c>/<c>dbo_history</c> and <c>consent</c> schemas
/// provisioned by <see cref="Altinn.AccessManagement.TestUtils.Factories.EFPostgresFactory"/>
/// (EF Core migrations), plus the <c>accessmanagement.*</c> and <c>delegation.*</c>
/// schemas (and their enum types) still provisioned by the Yuniql pipeline at host
/// startup.
/// </summary>
/// <remarks>
/// <para>
/// Use this fixture for test classes that exercise code paths backed by the
/// still-extant Dapper / raw-Npgsql repositories — <c>ResourceMetadataRepo</c> and
/// <c>DelegationMetadataRepo</c> (on the Yuniql <c>accessmanagement</c> /
/// <c>delegation</c> schemas) and <c>ConsentRepository</c> (on the EF-provisioned
/// <c>consent</c> schema) — or any component that depends on the Yuniql-provisioned
/// enum types bound via <c>PersistenceDependencyInjectionExtensions.AddDatabase</c>.
/// For pure EF tests that need none of the Yuniql schemas, keep using
/// <see cref="ApiFixture"/> directly — it boots faster because it skips the Yuniql
/// pipeline.
/// </para>
/// <para>
/// How it works: the base <see cref="ApiFixture"/> provisions a per-test
/// database that already has the EF schemas applied via
/// <see cref="Altinn.AccessManagement.TestUtils.Factories.EFPostgresFactory"/>.
/// This subclass additionally loads <c>appsettings.test.json</c> (for
/// non-database test configuration) and sets
/// <c>PostgreSQLSettings:EnableDBConnection=true</c> so the production
/// <c>AccessManagementHost.ConfigurePostgreSqlConfiguration</c> flips on the
/// <c>Altinn:Npgsql:*:Migrate:Enabled</c> switch that triggers Yuniql
/// migrations during host startup. It deliberately does <em>not</em> set
/// <c>RunIntegrationTests</c>: that would run <c>Program.Init()</c> (EF migrate +
/// static-data ingest) again on a database already cloned from the migrated and
/// seeded template, so the template stays the single seed source.
/// </para>
/// <para>
/// Currently the Yuniql pipeline runs once per test database (on host build),
/// not once per template. If that cost ever becomes a bottleneck, moving the
/// Yuniql migration into the template build is possible without changing this
/// fixture's public surface.
/// </para>
/// </remarks>
public class LegacyApiFixture : AccessMgmtApiFixture
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
            // Enables the production Yuniql migration pipeline at host startup so
            // the per-test database receives the legacy accessmanagement.* and
            // delegation.* schemas + enum types. The consent schema is provisioned
            // by EF Core (ConsentSchema_Baseline) as part of the template, alongside
            // the dbo schemas. Yuniql is gated by EnableDBConnection alone (see
            // AccessManagementHost.ConfigurePostgreSqlConfiguration).
            //
            // RunIntegrationTests is intentionally NOT set: it would trigger
            // Program.Init() (EF migrate + StaticDataIngest + register import) on a
            // clone that already has the migrated, seeded schema, so it would only
            // repeat work (the register import is feature-flag gated off in tests).
            dict["PostgreSQLSettings:EnableDBConnection"] = "true";
        });
    }
}
