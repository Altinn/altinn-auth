#nullable enable

using Altinn.Authorization.ProblemDetails;

namespace Altinn.ResourceRegistry.Errors;

/// <summary>
/// Validation errors for the Resource Registry.
/// </summary>
internal static class ValidationErrors
{
    private static readonly ValidationErrorDescriptorFactory _factory
        = ValidationErrorDescriptorFactory.New("RR");

    /// <summary>
    /// Gets a validation error descriptor for when too many members are being replaced.
    /// </summary>
    public static ValidationErrorDescriptor AccessList_ReplaceMembers_TooMany { get; }
        = _factory.Create(0, "Cannot replace more than 100 members at a time. Use POST and DELETE methods instead.");

    /// <summary>
    /// Gets a validation error descriptor for when too many members are being added or removed.
    /// </summary>
    public static ValidationErrorDescriptor AccessList_AddRemoveMembers_TooMany { get; }
        = _factory.Create(1, "Cannot add or remove more than 100 members at a time.");

    /// <summary>
    /// Gets a validation error descriptor for when resource connections is requested without providing a resource identifier.
    /// </summary>
    public static ValidationErrorDescriptor AccessList_IncludeResourceConnections_MissingResourceIdentifier { get; }
        = _factory.Create(2, "Resource identifier is required when including resource connections.");
}
