#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Altinn.Swashbuckle.Examples;

namespace Altinn.Authorization.Api.Contracts.Authorization;

/// <summary>
/// A string value identifier of a CRA (sivilrettsforvaltningen) role. E.g. urn:altinn:external-role:cra:helfo-fastlege
/// </summary>
[JsonConverter(typeof(StringIdentifierJsonConverter))]
[ExcludeFromCodeCoverage]
public class CraRoleCode
    : StringIdentifier<CraRoleCode>,
    IStringIdentifierFactory<CraRoleCode>,
    IExampleDataProvider<CraRoleCode>
{
    private CraRoleCode(string value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a new <see cref="CraRoleCode"/> from the specified value without validation.
    /// </summary>
    /// <param name="value">The CRA role code value.</param>
    /// <returns>A <see cref="CraRoleCode"/>.</returns>
    public static CraRoleCode CreateUnchecked(string value)
        => new(value);

    /// <inheritdoc/>
    public static IEnumerable<CraRoleCode>? GetExamples(ExampleDataOptions options)
    {
        yield return new CraRoleCode("helfo-fastlege");
    }
}
