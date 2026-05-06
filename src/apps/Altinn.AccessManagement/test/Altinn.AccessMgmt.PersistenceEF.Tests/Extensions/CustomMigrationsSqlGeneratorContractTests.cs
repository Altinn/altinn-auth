using System.Reflection;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Migrations;

namespace Altinn.AccessMgmt.PersistenceEF.Tests.Extensions;

/// <summary>
/// Documents the EF1001 contract that <see cref="CustomMigrationsSqlGenerator"/>
/// depends on. The base class' constructor takes
/// <c>Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal.INpgsqlSingletonOptions</c>,
/// which Npgsql has marked internal — meaning it can move, be renamed, or be
/// removed without notice on a minor / patch upgrade of
/// <c>Npgsql.EntityFrameworkCore.PostgreSQL</c>.
/// </summary>
/// <remarks>
/// Most such breakages already surface as a compile error in the production
/// project (which references the type directly via a <c>using</c> directive
/// and the constructor parameter); these tests don't replace that signal.
/// What they add is twofold:
/// <list type="bullet">
///   <item>An executable record of the dependency in the test suite, so the
///   constraint is searchable by anyone auditing "what breaks on an Npgsql
///   upgrade" rather than discoverable only by tracing a build failure.</item>
///   <item>A metadata-level full-name canary in
///   <see cref="NpgsqlMigrationsSqlGenerator_keeps_INpgsqlSingletonOptions_constructor"/>
///   for the narrow case where Npgsql renames the type but ships a
///   backwards-compatible alias — the production build would stay green,
///   but the assertion would catch it.</item>
/// </list>
/// On any failure here the upgrade needs an explicit re-evaluation: re-shape
/// the generator around a replacement type, find a public-surface
/// alternative, or pin the package version.
/// </remarks>
public class CustomMigrationsSqlGeneratorContractTests
{
    private const string InternalOptionsTypeFullName =
        "Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal.INpgsqlSingletonOptions";

    [Fact]
    public void CustomMigrationsSqlGenerator_constructor_signature_pins_INpgsqlSingletonOptions()
    {
        var ctors = typeof(CustomMigrationsSqlGenerator).GetConstructors(
            BindingFlags.Public | BindingFlags.Instance);

        var ctor = Assert.Single(ctors);
        var parameters = ctor.GetParameters();

        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(MigrationsSqlGeneratorDependencies), parameters[0].ParameterType);
        Assert.Equal(InternalOptionsTypeFullName, parameters[1].ParameterType.FullName);
    }

    [Fact]
    public void NpgsqlMigrationsSqlGenerator_keeps_INpgsqlSingletonOptions_constructor()
    {
        // A removal, rename, or namespace move of INpgsqlSingletonOptions also
        // surfaces as a compile error in the production project, which references
        // the type directly. This assembly-level lookup is the canary for the
        // narrow case where Npgsql renames the type but ships a backwards-
        // compatible alias — the build would stay green, but `GetType` would
        // return null because the literal full name is no longer the canonical
        // type definition in the assembly's metadata.
        var optionsType = typeof(NpgsqlMigrationsSqlGenerator).Assembly
            .GetType(InternalOptionsTypeFullName);

        Assert.NotNull(optionsType);

        var ctor = typeof(NpgsqlMigrationsSqlGenerator).GetConstructor(
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: [typeof(MigrationsSqlGeneratorDependencies), optionsType!],
            modifiers: null);

        Assert.NotNull(ctor);
    }
}
