#nullable enable

using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Core.Errors;

/// <summary>
/// Validation errors for the Access Management.
/// </summary>
public static class ValidationErrors
{
    private static readonly ValidationErrorDescriptorFactory _factory
        = ValidationErrorDescriptorFactory.New("AM");

    /// <summary>
    /// The field is required.
    /// </summary>
    public static ValidationErrorDescriptor Required => StdValidationErrors.Required;

    /// <summary>
    /// Gets a validation error descriptor for when an invalid party URN is provided.
    /// </summary>
    public static ValidationErrorDescriptor InvalidPartyUrn { get; }
        = _factory.Create(1, "Invalid party URN.");

    /// <summary>
    /// Gets a validation error descriptor for when an invalid Resource URN is provided.
    /// </summary>
    public static ValidationErrorDescriptor InvalidResource { get; }
        = _factory.Create(2, $"Resource must be valid.");

    /// <summary>
    /// Gets a validation error descriptor for when a resource is missing policy file
    /// </summary>
    public static ValidationErrorDescriptor MissingPolicy { get; }
        = _factory.Create(3, $"Resource must have a policy.");

    /// <summary>
    /// Gets a validation error descriptor for when a Resource not has any delegable rights for the app
    /// </summary>
    public static ValidationErrorDescriptor MissingDelegableRights { get; }
        = _factory.Create(4, $"The Resource must have a policy giving the app rights to delegate at least one right.");

    /// <summary>
    /// Gets a validation error descriptor for when a Resource not has any delegable rights for the app
    /// </summary>
    public static ValidationErrorDescriptor ToManyDelegationsToRevoke { get; }
        = _factory.Create(5, $"There is to many policy files to update. Must delete individual delegations.");

    /// <summary>
    /// Missing party.
    /// </summary>
    public static ValidationErrorDescriptor EntityNotExists { get; }
        = _factory.Create(6, $"Entity do not exists.");

    /// <summary>
    /// Role is missing.
    /// </summary>
    public static ValidationErrorDescriptor RoleNotExists { get; }
        = _factory.Create(7, $"Role do not exsists.");

    /// <summary>
    /// EntityType.
    /// </summary>
    public static ValidationErrorDescriptor DisallowedEntityType { get; }
        = _factory.Create(8, $"This entity type is not allowed here.");

    /// <summary>
    /// Invalid party type.
    /// </summary>
    public static ValidationErrorDescriptor InvalidQueryParameter { get; }
        = _factory.Create(9, $"One or more query parameters are invalid.");

    /// <summary>
    /// Assignment is active in one or more delegations.
    /// </summary>
    public static ValidationErrorDescriptor AssignmentHasActiveConnections { get; }
        = _factory.Create(10, $"Assignment is active in one or more connections.");

    /// <summary>
    /// Assignment is active in one or more delegations.
    /// </summary>
    public static ValidationErrorDescriptor PackageNotExists { get; }
        = _factory.Create(11, $"Package do not exists.");

    /// <summary>
    /// Resource does not exist.
    /// </summary>
    public static ValidationErrorDescriptor ResourceNotExists { get; }
        = _factory.Create(11, $"Resource do not exists.");

    /// <summary>
    /// Gets a validation error descriptor
    /// </summary>
    public static ValidationErrorDescriptor TimeNotInFuture { get; }
        = _factory.Create(22, $"The time need to be in the future");

    /// <summary>
    /// Gets a validation error descriptor
    /// </summary>
    public static ValidationErrorDescriptor EmptyList { get; }
        = _factory.Create(23, $"The list cant be empty");

    /// <summary>
    /// Gets a validation error descriptor
    /// </summary>
    public static ValidationErrorDescriptor ConsentNotFound { get; }
    = _factory.Create(24, $"Incorrect consentId or wrong consent receiver");

    /// <summary>
    /// Gets a validation error descriptor
    /// </summary>
    public static ValidationErrorDescriptor InvalidResourceContext { get; }
    = _factory.Create(25, $"Resource context does not match consent request rights");

    public static ValidationErrorDescriptor UserNotAuthorized { get; }
    = _factory.Create(26, $"User not authorized for operation.");

    /// <summary>
    /// Gets a validation error descriptor
    /// </summary>
    public static ValidationErrorDescriptor InvalidRedirectUrl { get; internal set; }
        = _factory.Create(27, $"Redirect URL is not a valid URL");

    /// <summary>
    /// Invalid party type.
    /// </summary>
    public static ValidationErrorDescriptor PackageIsNotAssignableToRecipient { get; }
        = _factory.Create(28, $"One or more packages is not assignable to receipient.");

    /// <summary>
    /// Gets a validation error descriptor for when an invalid Role.
    /// </summary>
    public static ValidationErrorDescriptor InvalidRole { get; }
        = _factory.Create(29, $"Invalid Role.");

    /// <summary>
    /// Gets a validation error descriptor for when an invalid package.
    /// </summary>
    public static ValidationErrorDescriptor InvalidPackage { get; }
        = _factory.Create(30, $"Invalid Package.");

    /// <summary>
    /// Delegation has active connections.
    /// </summary>
    public static ValidationErrorDescriptor DelegationHasActiveConnections { get; }
        = _factory.Create(31, "The delegation has one or more active access packages associated with it. Set cascade to true to remove the delegation and its active access packages.");

    /// <summary>
    /// Delegation has active connections.
    /// </summary>
    public static ValidationErrorDescriptor MissingAssignment { get; }
        = _factory.Create(32, "Assignment of role do not exist.");

    /// <summary>
    /// Delegation has active connections.
    /// </summary>
    public static ValidationErrorDescriptor PackageIsNotDelegable { get; }
        = _factory.Create(33, "Package is not delegable.");

    /// <summary>
    /// Delegation has active connections.
    /// </summary>
    public static ValidationErrorDescriptor InvalidExternalIdentifiers { get; }
        = _factory.Create(34, "Given external identifiers yielded empty result.");

    /// <summary>
    /// Request not found
    /// </summary>
    public static ValidationErrorDescriptor RequestNotFound { get; }
        = _factory.Create(35, $"Request do not exists.");

    /// <summary>
    /// Request not found
    /// </summary>
    public static ValidationErrorDescriptor RequestUnsupportedStatusUpdate { get; }
        = _factory.Create(36, $"Request does not support this status update.");

    /// <summary>
    /// RequestMissingFromOrTo
    /// </summary>
    public static ValidationErrorDescriptor RequestMissingFromOrTo { get; }
        = _factory.Create(37, $"Query must have either from or to defined.");

    /// <summary>
    /// RequestMissingFromOrTo
    /// </summary>
    public static ValidationErrorDescriptor RequestMissingResourceOrPackage { get; }
        = _factory.Create(38, $"Either Resource or Package must be included in request.");

    /// <summary>
    /// RequestMissingFromOrTo
    /// </summary>
    public static ValidationErrorDescriptor RequestFailedToCreateRequest { get; }
        = _factory.Create(39, $"Could not create request.");

    /// <summary>
    /// Failed to Approve request
    /// </summary>
    public static ValidationErrorDescriptor RequestFailedToApprove { get; }
        = _factory.Create(40, $"Failed to approve request.");

    /// <summary>
    /// Request connection not found
    /// </summary>
    public static ValidationErrorDescriptor RequestConnectionNotFound { get; }
        = _factory.Create(41, $"Initial connection between parties not found.");

    /// <summary>
    /// DbNoRowsAffected
    /// </summary>
    public static ValidationErrorDescriptor DbNoRowsAffected { get; }
        = _factory.Create(42, $"No rows affected.");

    /// <summary>
    /// DbNoRowsFound
    /// </summary>
    public static ValidationErrorDescriptor DbNoRowsFound { get; }
        = _factory.Create(43, $"No rows found.");

    /// <summary>
    /// RequestUnauthorizedStatusUpdate
    /// </summary>
    public static ValidationErrorDescriptor RequestUnauthorizedStatusUpdate { get; }
        = _factory.Create(44, $"Party cannot perform this status change.");

    /// <summary>
    /// Request connection not found
    /// </summary>
    public static ValidationErrorDescriptor RequestFromSelfNotAllowed { get; }
        = _factory.Create(45, $"Self-targeted requests are not allowed.");

    /// <summary>
    /// Either Resource or Package must be included in the request, but not both.
    /// </summary>
    public static ValidationErrorDescriptor ResourceAndPackageIsSpecified { get; }
        = _factory.Create(46, "Either Resource or Package must be included in the request, but not both.");

    /// <summary>
    /// More than one fromParty is connected to the same instance uuid this should not be posible
    /// </summary>
    public static ValidationErrorDescriptor InvalidInstanceId { get; }
        = _factory.Create(47, $"The instance ID is invalid as more than one owner was found.");

    /// <summary>
    /// Package is not assignable
    /// </summary>
    public static ValidationErrorDescriptor PackageIsNotAssignable { get; }
        = _factory.Create(48, $"One or more packages is not assignable.");

    /// <summary>
    /// Resource is not delegable
    /// </summary>
    public static ValidationErrorDescriptor ResourceIsNotDelegable { get; }
        = _factory.Create(49, $"One or more resources is not delegable.");

    /// <summary>
    /// Invalid page size for consent status changes. The page size must be between 1 and 1000
    /// </summary>
    public static ValidationErrorDescriptor InvalidPageSizeForConsentStatusChanges { get; }
    = _factory.Create(50, $"Page size must be between 1 and 1000.");
}
