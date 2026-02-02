namespace Altinn.AccessManagement.Api.Enduser.Validation;

/// <summary>
/// Centralized validation message constants for enduser connection/assignment validation.
/// All messages use consistent casing, terminology (UUID) and avoid trailing periods for uniformity.
/// </summary>
internal static class ValidationErrorMessageTexts
{
    internal const string PartyMustMatchFrom = "Must match the 'party' UUID";
    internal const string FromOrToMustMatchParty = "Either 'from' or 'to' must match the 'party' UUID";
    internal const string ProvideEitherPackageRef = "Provide either a package URN or a package ID, not both";
    internal const string ProvideEitherResourceRef = "Provide either a resource key or a resource ID, not both";
    internal const string RequireOnePackageRef = "Either a package URN or a package ID must be provided";
    internal const string RequireOneResourceRef = "Either a resource key or a resource ID must be provided";
    internal const string PackageIdMustNotBeEmpty = "Package ID must not be empty UUID";
    internal const string ResourceIdMustNotBeEmpty = "Resource ID must not be empty UUID";
    internal const string InvalidPartyValue = "Must be a valid non-empty UUID";
    internal const string PersonIdentifierRequired = "Required when providing PersonInput details";
    internal const string PersonIdentifierInvalid = "Invalid national identity number";
    internal const string LastNameRequired = "Required when providing PersonInput details";
    internal const string PersonIdentifierLastNameInvalid = "PersonInput details must match";
}
