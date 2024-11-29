#nullable enable

using Altinn.Authorization.ProblemDetails;

namespace Altinn.Authorization.Errors;

/// <summary>
/// Validation errors for Authorization
/// </summary>
public static class ValidationErrors
{
    private static readonly ValidationErrorDescriptorFactory _factory
        = ValidationErrorDescriptorFactory.New("AUTHZ");

    /// <summary>
    /// Gets a validation error descriptor for when a provided resource registry identifier is not found as a valid resource.
    /// </summary>
    public static ValidationErrorDescriptor ResourceRegistry_ResourceIdentifier_NotFound { get; }
        = _factory.Create(0, "Unknown resource registry identifier.");

    /// <summary>
    /// Gets a validation error descriptor for when the authencticated organization number is not authorized as the competent authority owner of a resource registry resource.
    /// </summary>
    public static ValidationErrorDescriptor ResourceRegistry_CompetentAuthority_NotMatchingAuthenticatedOrganization { get; }
        = _factory.Create(1, "Authorized organization is not the competent authority owner of the requested resource.");

    /// <summary>
    /// Gets a validation error descriptor for when the authencticated organization code is not authorized as the competent authority owner of a resource registry resource.
    /// </summary>
    public static ValidationErrorDescriptor ResourceRegistry_CompetentAuthority_NotMatchingAuthenticatedOrgCode { get; }
        = _factory.Create(2, "Authorized organization code is not the competent authority owner of the requested resource.");
}
