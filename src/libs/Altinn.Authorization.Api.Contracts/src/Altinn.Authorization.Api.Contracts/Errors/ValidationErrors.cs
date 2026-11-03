#nullable enable

using System.Diagnostics.CodeAnalysis;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Core.Errors;

/// <summary>
/// Validation errors for the Access Management.
/// </summary>
[ExcludeFromCodeCoverage]
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
    /// Gets a validation error descriptor for when an invalid resource URN is provided.
    /// </summary>
    public static ValidationErrorDescriptor InvalidResource { get; }
        = _factory.Create(2, $"Resource must be valid.");

    /// <summary>
    /// Gets a validation error descriptor for when a resource has no policy.
    /// </summary>
    public static ValidationErrorDescriptor MissingPolicy { get; }
        = _factory.Create(3, $"Resource must have a policy.");

    /// <summary>
    /// Gets a validation error descriptor for when the resource policy does not give the app the right to delegate any rights.
    /// </summary>
    public static ValidationErrorDescriptor MissingDelegableRights { get; }
        = _factory.Create(4, $"The Resource must have a policy giving the app rights to delegate at least one right.");

    /// <summary>
    /// Gets a validation error descriptor for when there are too many policy files to update, so delegations must be deleted one by one.
    /// </summary>
    public static ValidationErrorDescriptor ToManyDelegationsToRevoke { get; }
        = _factory.Create(5, $"There is to many policy files to update. Must delete individual delegations.");

    /// <summary>
    /// Gets a validation error descriptor for when an entity does not exist.
    /// </summary>
    public static ValidationErrorDescriptor EntityNotExists { get; }
        = _factory.Create(6, $"Entity do not exists.");

    /// <summary>
    /// Gets a validation error descriptor for when a role does not exist.
    /// </summary>
    public static ValidationErrorDescriptor RoleNotExists { get; }
        = _factory.Create(7, $"Role do not exsists.");

    /// <summary>
    /// Gets a validation error descriptor for when the entity type is not allowed.
    /// </summary>
    public static ValidationErrorDescriptor DisallowedEntityType { get; }
        = _factory.Create(8, $"This entity type is not allowed here.");

    /// <summary>
    /// Gets a validation error descriptor for when one or more query parameters are invalid.
    /// </summary>
    public static ValidationErrorDescriptor InvalidQueryParameter { get; }
        = _factory.Create(9, $"One or more query parameters are invalid.");

    /// <summary>
    /// Gets a validation error descriptor for when an assignment still has active connections.
    /// </summary>
    public static ValidationErrorDescriptor AssignmentHasActiveConnections { get; }
        = _factory.Create(10, $"Assignment is active in one or more connections.");

    /// <summary>
    /// Gets a validation error descriptor for when a package does not exist.
    /// </summary>
    public static ValidationErrorDescriptor PackageNotExists { get; }
        = _factory.Create(11, $"Package do not exists.");

    /// <summary>
    /// Gets a validation error descriptor for when a resource does not exist.
    /// </summary>
    public static ValidationErrorDescriptor ResourceNotExists { get; }
        = _factory.Create(12, $"Resource does not exist.");

    /// <summary>
    /// Gets a validation error descriptor for when a time is not in the future.
    /// </summary>
    public static ValidationErrorDescriptor TimeNotInFuture { get; }
        = _factory.Create(22, $"The time need to be in the future");

    /// <summary>
    /// Gets a validation error descriptor for when a list is empty.
    /// </summary>
    public static ValidationErrorDescriptor EmptyList { get; }
        = _factory.Create(23, $"The list cant be empty");

    /// <summary>
    /// Gets a validation error descriptor for when a consent is not found or the consent receiver is wrong.
    /// </summary>
    public static ValidationErrorDescriptor ConsentNotFound { get; }
    = _factory.Create(24, $"Incorrect consentId or wrong consent receiver");

    /// <summary>
    /// Gets a validation error descriptor for when the resource context does not match the rights in the consent request.
    /// </summary>
    public static ValidationErrorDescriptor InvalidResourceContext { get; }
    = _factory.Create(25, $"Resource context does not match consent request rights");

    /// <summary>
    /// Gets a validation error descriptor for when the user is not authorized for the operation.
    /// </summary>
    public static ValidationErrorDescriptor UserNotAuthorized { get; }
    = _factory.Create(26, $"User not authorized for operation.");

    /// <summary>
    /// Gets a validation error descriptor for when a redirect URL is not valid.
    /// </summary>
    public static ValidationErrorDescriptor InvalidRedirectUrl { get; internal set; }
        = _factory.Create(27, $"Redirect URL is not a valid URL");

    /// <summary>
    /// Gets a validation error descriptor for when one or more packages cannot be assigned to the recipient.
    /// </summary>
    public static ValidationErrorDescriptor PackageIsNotAssignableToRecipient { get; }
        = _factory.Create(28, $"One or more packages is not assignable to receipient.");

    /// <summary>
    /// Gets a validation error descriptor for when a role is invalid.
    /// </summary>
    public static ValidationErrorDescriptor InvalidRole { get; }
        = _factory.Create(29, $"Invalid Role.");

    /// <summary>
    /// Gets a validation error descriptor for when a package is invalid.
    /// </summary>
    public static ValidationErrorDescriptor InvalidPackage { get; }
        = _factory.Create(30, $"Invalid Package.");

    /// <summary>
    /// Gets a validation error descriptor for when a delegation still has active access packages.
    /// </summary>
    public static ValidationErrorDescriptor DelegationHasActiveConnections { get; }
        = _factory.Create(31, "The delegation has one or more active access packages associated with it. Set cascade to true to remove the delegation and its active access packages.");

    /// <summary>
    /// Gets a validation error descriptor for when a role assignment does not exist.
    /// </summary>
    public static ValidationErrorDescriptor MissingAssignment { get; }
        = _factory.Create(32, "Assignment of role do not exist.");

    /// <summary>
    /// Gets a validation error descriptor for when a package is not delegable.
    /// </summary>
    public static ValidationErrorDescriptor PackageIsNotDelegable { get; }
        = _factory.Create(33, "Package is not delegable.");

    /// <summary>
    /// Gets a validation error descriptor for when no party matches the given external identifiers.
    /// </summary>
    public static ValidationErrorDescriptor InvalidExternalIdentifiers { get; }
        = _factory.Create(34, "Given external identifiers yielded empty result.");

    /// <summary>
    /// Gets a validation error descriptor for when a request does not exist.
    /// </summary>
    public static ValidationErrorDescriptor RequestNotFound { get; }
        = _factory.Create(35, $"Request do not exists.");

    /// <summary>
    /// Gets a validation error descriptor for when a request does not support the status update.
    /// </summary>
    public static ValidationErrorDescriptor RequestUnsupportedStatusUpdate { get; }
        = _factory.Create(36, $"Request does not support this status update.");

    /// <summary>
    /// Gets a validation error descriptor for when a query has neither from nor to.
    /// </summary>
    public static ValidationErrorDescriptor RequestMissingFromOrTo { get; }
        = _factory.Create(37, $"Query must have either from or to defined.");

    /// <summary>
    /// Gets a validation error descriptor for when a request has neither a resource nor a package.
    /// </summary>
    public static ValidationErrorDescriptor RequestMissingResourceOrPackage { get; }
        = _factory.Create(38, $"Either Resource or Package must be included in request.");

    /// <summary>
    /// Gets a validation error descriptor for when a request could not be created.
    /// </summary>
    public static ValidationErrorDescriptor RequestFailedToCreateRequest { get; }
        = _factory.Create(39, $"Could not create request.");

    /// <summary>
    /// Gets a validation error descriptor for when a request could not be approved.
    /// </summary>
    public static ValidationErrorDescriptor RequestFailedToApprove { get; }
        = _factory.Create(40, $"Failed to approve request.");

    /// <summary>
    /// Gets a validation error descriptor for when the initial connection between the parties is not found.
    /// </summary>
    public static ValidationErrorDescriptor RequestConnectionNotFound { get; }
        = _factory.Create(41, $"Initial connection between parties not found.");

    /// <summary>
    /// Gets a validation error descriptor for when no database rows were affected.
    /// </summary>
    public static ValidationErrorDescriptor DbNoRowsAffected { get; }
        = _factory.Create(42, $"No rows affected.");

    /// <summary>
    /// Gets a validation error descriptor for when no database rows were found.
    /// </summary>
    public static ValidationErrorDescriptor DbNoRowsFound { get; }
        = _factory.Create(43, $"No rows found.");

    /// <summary>
    /// Gets a validation error descriptor for when the party is not allowed to make the status change.
    /// </summary>
    public static ValidationErrorDescriptor RequestUnauthorizedStatusUpdate { get; }
        = _factory.Create(44, $"Party cannot perform this status change.");

    /// <summary>
    /// Gets a validation error descriptor for when a party sends a request to itself.
    /// </summary>
    public static ValidationErrorDescriptor RequestFromSelfNotAllowed { get; }
        = _factory.Create(45, $"Self-targeted requests are not allowed.");

    /// <summary>
    /// Gets a validation error descriptor for when a request includes both a resource and a package.
    /// </summary>
    public static ValidationErrorDescriptor ResourceAndPackageIsSpecified { get; }
        = _factory.Create(46, "Either Resource or Package must be included in the request, but not both.");

    /// <summary>
    /// Gets a validation error descriptor for when more than one owner is found for an instance.
    /// </summary>
    public static ValidationErrorDescriptor InvalidInstanceId { get; }
        = _factory.Create(47, $"The instance ID is invalid as more than one owner was found.");

    /// <summary>
    /// Gets a validation error descriptor for when one or more packages are not assignable.
    /// </summary>
    public static ValidationErrorDescriptor PackageIsNotAssignable { get; }
        = _factory.Create(48, $"One or more packages is not assignable.");

    /// <summary>
    /// Gets a validation error descriptor for when one or more resources are not delegable.
    /// </summary>
    public static ValidationErrorDescriptor ResourceIsNotDelegable { get; }
        = _factory.Create(49, $"One or more resources is not delegable.");

    /// <summary>
    /// Gets a validation error descriptor for when the page size for consent events is not between 1 and 1000.
    /// </summary>
    public static ValidationErrorDescriptor InvalidPageSizeForConsentEvents { get; }
    = _factory.Create(50, $"Page size must be between 1 and 1000.");

    /// <summary>
    /// Gets a validation error descriptor for when createdAfter is not earlier than createdBefore.
    /// </summary>
    public static ValidationErrorDescriptor InvalidDateRange { get; }
    = _factory.Create(51, $"'createdAfter' must be earlier than 'createdBefore'.");

    /// <summary>
    /// Gets a validation error descriptor for when one or more event types are invalid.
    /// </summary>
    public static ValidationErrorDescriptor InvalidEventType { get; }
    = _factory.Create(52, $"One or more event types are invalid. Valid values are: rejected, accepted, revoked, deleted, used.");

    /// <summary>
    /// Gets a validation error descriptor for when the continuation token is invalid.
    /// </summary>
    public static ValidationErrorDescriptor InvalidContinuationToken { get; }
    = _factory.Create(53, $"The continuation token is invalid.");

    /// <summary>
    /// Gets a validation error descriptor for when a role assignment cannot be revoked.
    /// </summary>
    public static ValidationErrorDescriptor UnableToRevokeRoleAssignment { get; }
        = _factory.Create(54, $"Role assignment cannot be revoked.");

    /// <summary>
    /// Gets a validation error descriptor for when the operation could not be completed.
    /// </summary>
    public static ValidationErrorDescriptor UnableToCompleteOperation { get; }
        = _factory.Create(55, $"The operation could not be completed.");

    /// <summary>
    /// Gets a validation error descriptor for when an assignment has resources delegated.
    /// </summary>
    public static ValidationErrorDescriptor AssignmentResourcesExist { get; }
        = _factory.Create(56, "Assignment has resources delegated.");

    /// <summary>
    /// Gets a validation error descriptor for when a resource type is invalid.
    /// </summary>
    public static ValidationErrorDescriptor InvalidResourceType { get; }
        = _factory.Create(57, "Invalid resource type.");

    /// <summary>
    /// Gets a validation error descriptor for when clearing delegation policy rules fails.
    /// </summary>
    public static ValidationErrorDescriptor PolicyClearFailed { get; }
        = _factory.Create(58, "Failed to clear delegation policy rules.");

    /// <summary>
    /// Gets a validation error descriptor for when a resource is held through a client delegation and cannot be revoked on this endpoint.
    /// </summary>
    public static ValidationErrorDescriptor ResourceNotRevocableViaClientDelegation { get; }
        = _factory.Create(59, "Resource is delegated through a client delegation.");
}
