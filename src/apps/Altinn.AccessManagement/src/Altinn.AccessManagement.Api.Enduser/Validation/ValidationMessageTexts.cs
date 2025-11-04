namespace Altinn.AccessManagement.Api.Enduser.Validation;

/// <summary>
/// Centralized validation message constants for enduser connection/assignment validation.
/// All messages use consistent casing, terminology (UUID) and avoid trailing periods for uniformity.
/// </summary>
internal static class ValidationMessageTexts
{
 internal const string PartyMustMatchFrom = "Must match the 'party' UUID";
 internal const string FromOrToMustMatchParty = "Either 'from' or 'to' must match the 'party' UUID";
 internal const string ProvideEitherPackageRef = "Provide either a package URN or a package ID, not both";
 internal const string RequireOnePackageRef = "Either a package URN or a package ID must be provided";
 internal const string PackageIdMustNotBeEmpty = "Package ID must not be empty UUID";
 internal const string InvalidPartyValue = "Must be a valid non-empty UUID or one of <me, all>";
 internal const string PersonIdentifierRequired = "PersonIdentifier is required when providing PersonInput details";
 internal const string PersonIdentifierInvalidNin = "PersonIdentifier with national identity number format is not a valid national identity number";
 internal const string LastNameRequired = "LastName is required when providing PersonInput details";
}
