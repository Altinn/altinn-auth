#nullable enable

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using Altinn.Swashbuckle.Examples;

namespace Altinn.Authorization.Api.Contracts.Authorization;

/// <summary>
/// A string value identifier of an access package.
/// </summary>
[JsonConverter(typeof(StringIdentifierJsonConverter))]
[ExcludeFromCodeCoverage]
public class AccessPackageIdentifier
    : StringIdentifier<AccessPackageIdentifier>,
    IStringIdentifierFactory<AccessPackageIdentifier>,
    IExampleDataProvider<AccessPackageIdentifier>
{
    private AccessPackageIdentifier(string value)
        : base(value)
    {
    }

    /// <summary>
    /// Creates a new <see cref="AccessPackageIdentifier"/> from the specified value without validation.
    /// </summary>
    /// <param name="value">The package identifier.</param>
    /// <returns>A <see cref="AccessPackageIdentifier"/>.</returns>
    public static AccessPackageIdentifier CreateUnchecked(string value)
        => new(value);

    /// <inheritdoc/>
    public static IEnumerable<AccessPackageIdentifier>? GetExamples(ExampleDataOptions options)
    {
        yield return new AccessPackageIdentifier("skatt-naering");
        yield return new AccessPackageIdentifier("ansettelsesforhold");
    }
}
