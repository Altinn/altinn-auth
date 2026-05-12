#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Altinn.Swashbuckle.Examples;

namespace Altinn.Authorization.Api.Contracts.Authorization;

/// <summary>
/// A string value identifier of a CCR (enhetsregisteret) role. E.g. urn:altinn:external-role:ccr:daglig-leder
/// </summary>
[JsonConverter(typeof(StringIdentifierJsonConverter))]
[ExcludeFromCodeCoverage]
public class CcrRoleCode
    : StringIdentifier<CcrRoleCode>,
    IStringIdentifierFactory<CcrRoleCode>,
    IExampleDataProvider<CcrRoleCode>
{
    private CcrRoleCode(string value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a new <see cref="CcrRoleCode"/> from the specified value without validation.
    /// </summary>
    /// <param name="value">The CCR role code value.</param>
    /// <returns>A <see cref="CcrRoleCode"/>.</returns>
    public static CcrRoleCode CreateUnchecked(string value)
        => new(value);

    /// <inheritdoc/>
    public static IEnumerable<CcrRoleCode>? GetExamples(ExampleDataOptions options)
    {
        yield return new CcrRoleCode("daglig-leder");
    }
}
