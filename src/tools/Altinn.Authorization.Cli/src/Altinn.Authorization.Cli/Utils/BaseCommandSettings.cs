using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;
using ConsoleValidationResult = Spectre.Console.ValidationResult;
using DataAnnotationsValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace Altinn.Authorization.Cli.Utils;

/// <summary>
/// Base command settings.
/// </summary>
public abstract class BaseCommandSettings
    : CommandSettings
    , IValidatableObject
{
    /// <inheritdoc/>
    public override ConsoleValidationResult Validate()
    {
        var context = new ValidationContext(this);
        var result = new List<DataAnnotationsValidationResult>();
        if (!Validator.TryValidateObject(this, context, result, validateAllProperties: true))
        {
            var builder = new StringBuilder("Command validation failed:");
            foreach (var error in result)
            {
                builder.AppendLine().Append($"- {string.Join(", ", error.MemberNames).EscapeMarkup()}: {error.ErrorMessage}");
            }

            return ConsoleValidationResult.Error(builder.ToString());
        }

        return base.Validate();
    }

    /// <inheritdoc cref="IValidatableObject.Validate(ValidationContext)"/>
    protected virtual IEnumerable<DataAnnotationsValidationResult> Validate(ValidationContext validationContext)
    {
        if (GetType().GetCustomAttribute<System.Runtime.CompilerServices.RequiredMemberAttribute>() is not null)
        {
            // type has required members
            foreach (var error in ValidateRequiredMembers(validationContext))
            {
                yield return error;
            }
        }
    }

    private IEnumerable<DataAnnotationsValidationResult> ValidateRequiredMembers(ValidationContext validationContext)
    {
        var type = GetType();
        var nullabilityContext = new NullabilityInfoContext();
        var properties = TypeDescriptor.GetProperties(this);

        foreach (PropertyDescriptor propertyDescriptor in properties)
        {
            var propertyInfo = type.GetProperty(propertyDescriptor.Name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (propertyInfo is null)
            {
                continue;
            }

            var nullabilityInfo = nullabilityContext.Create(propertyInfo);
            var isRequired = propertyDescriptor.Attributes.OfType<System.Runtime.CompilerServices.RequiredMemberAttribute>().Any();

            if (nullabilityInfo.WriteState == NullabilityState.NotNull && isRequired)
            {
                // required non-null property
                var value = propertyDescriptor.GetValue(this);
                if (value is null)
                {
                    var message = string.Create(CultureInfo.InvariantCulture, $"The property {propertyDescriptor.DisplayName} is required.");

                    yield return new DataAnnotationsValidationResult(message, memberNames: [propertyDescriptor.Name]);
                }
            }
        }
    }

    /// <inheritdoc/>
    IEnumerable<DataAnnotationsValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        => Validate(validationContext);
}
