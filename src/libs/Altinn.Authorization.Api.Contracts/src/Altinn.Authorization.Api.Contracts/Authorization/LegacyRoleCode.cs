#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Altinn.Swashbuckle.Examples;

namespace Altinn.Authorization.Api.Contracts.Authorization;

/// <summary>
/// A string value for a legacy role code. E.g. urn:altinn:rolecode:priv or urn:altinn:rolecode:seln or urn:altinn:rolecode:dagl
/// </summary>
[JsonConverter(typeof(StringIdentifierJsonConverter))]
[ExcludeFromCodeCoverage]
public class LegacyRoleCode
    : StringIdentifier<LegacyRoleCode>,
    IStringIdentifierFactory<LegacyRoleCode>,
    IExampleDataProvider<LegacyRoleCode>
{
    private LegacyRoleCode(string value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a new <see cref="LegacyRoleCode"/> from the specified value without validation.
    /// </summary>
    /// <param name="value">The role code.</param>
    /// <returns>A <see cref="LegacyRoleCode"/>.</returns>
    public static LegacyRoleCode CreateUnchecked(string value)
        => new(value);

    /// <inheritdoc/>
    public static IEnumerable<LegacyRoleCode>? GetExamples(ExampleDataOptions options)
    {
        yield return new LegacyRoleCode("dagl");
        yield return new LegacyRoleCode("priv");
    }
}
