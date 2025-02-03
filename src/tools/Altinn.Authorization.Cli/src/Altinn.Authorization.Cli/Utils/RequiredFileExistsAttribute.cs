using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Altinn.Authorization.Cli.Utils;

/// <summary>
/// Requires that a file exists.
/// </summary>
public class RequiredFileExistsAttribute
    : RequiredAttribute
{
    /// <inheritdoc/>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        FileInfo file;
        if (value is string filePath)
        {
            file = new FileInfo(filePath);
        }
        else if (value is FileInfo fileInfo)
        {
            file = fileInfo;
        }
        else
        {
            return Error(validationContext);
        }

        if (!file.Exists)
        {
            return Error(validationContext);
        }

        return base.IsValid(value, validationContext);
    }

    private ValidationResult Error(ValidationContext validationContext)
    {
        return new ValidationResult(FormatErrorMessage(validationContext.DisplayName));
    }

    /// <inheritdoc/>
    public override string FormatErrorMessage(string name)
        => string.Create(CultureInfo.InvariantCulture, $"The parameter '{name}' must be set and point to an existing file.");
}
