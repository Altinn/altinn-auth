#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Altinn.Swashbuckle.Examples;

namespace Altinn.Authorization.Api.Contracts.Authorization;

/// <summary>
/// A string value identifier of an Altinn Role Code. E.g. urn:altinn:role:privatperson or urn:altinn:role:selvregistrert
/// </summary>
[JsonConverter(typeof(StringIdentifierJsonConverter))]
[ExcludeFromCodeCoverage]
public class AltinnRoleCode
    : StringIdentifier<AltinnRoleCode>,
    IStringIdentifierFactory<AltinnRoleCode>,
    IExampleDataProvider<AltinnRoleCode>
{
    private AltinnRoleCode(string value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a new <see cref="AltinnRoleCode"/> from the specified value without validation.
    /// </summary>
    /// <param name="value">The Altinn role code value.</param>
    /// <returns>A <see cref="AltinnRoleCode"/>.</returns>
    public static AltinnRoleCode CreateUnchecked(string value)
        => new(value);

    /// <inheritdoc/>
    public static IEnumerable<AltinnRoleCode>? GetExamples(ExampleDataOptions options)
    {
        yield return new AltinnRoleCode("privatperson");
        yield return new AltinnRoleCode("selvregistrert");
    }
}
