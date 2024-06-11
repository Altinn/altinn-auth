#nullable enable

using Altinn.Authorization.ProblemDetails;

namespace Altinn.ResourceRegistry.Core.Errors;

/// <summary>
/// Validation errors for the Resource Registry.
/// </summary>
public static class ValidationErrors
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

    /// <summary>
    /// Gets a validation error descriptor for when no party is specified when getting access-list memberships.
    /// </summary>
    public static ValidationErrorDescriptor AccessListMemberships_Requires_Party { get; }
        = _factory.Create(3, "At least 1 party must be specified.");

    /// <summary>
    /// Gets a validation error descriptor for when an invalid party URN is provided.
    /// </summary>
    public static ValidationErrorDescriptor InvalidPartyUrn { get; }
        = _factory.Create(4, "Invalid party URN.");

    /// <summary>
    /// Gets a validation error descriptor for when an invalid resource URN is provided.
    /// </summary>
    public static ValidationErrorDescriptor InvalidResourceUrn { get; }
        = _factory.Create(5, "Invalid resource URN.");

    /// <summary>
    /// Gets a validation error descriptor for when too many parties are included in a request.
    /// </summary>
    public static ValidationErrorDescriptor AccessListMemberships_TooManyParties { get; }
        = _factory.Create(6, "Multiple parties currently not supported.");

    /// <summary>
    /// Gets a validation error descriptor for when too many parties are included in a request.
    /// </summary>
    public static ValidationErrorDescriptor AccessListMemberships_TooManyResources { get; }
        = _factory.Create(7, "Multiple resources currently not supported.");
}
