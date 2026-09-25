using System.ComponentModel.DataAnnotations;

namespace Altinn.ResourceRegistry.Integration;

/// <summary>
/// Options for the register client.
/// </summary>
public class RegisterClientOptions
    : IValidatableObject
{
    /// <summary>
    /// The register URI.
    /// </summary>
    public Uri? Uri { get; set; }

    /// <inheritdoc/>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Uri is null)
        {
            yield return new ValidationResult("Uri is required", [nameof(Uri)]);
        }
    }
}
