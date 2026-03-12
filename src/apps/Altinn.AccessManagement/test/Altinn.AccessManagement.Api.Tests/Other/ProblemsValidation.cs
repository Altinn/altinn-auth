using Altinn.AccessManagement.Core.Errors;

namespace Altinn.AccessManagement.Api.Tests.Other;

public class ProblemsValidation
{
    [Fact]
    public void Problems_RequestProblems_ShouldBeAccessible()
    {
        _ = Problems.NotAuthorizedForConsentRequest;
        _ = Problems.ConsentNotFound;
        _ = Problems.ConsentCantBeAccepted;
        _ = Problems.InvalidOrganizationIdentifier;
        _ = Problems.InvalidPersonIdentifier;
        _ = Problems.InvalidConsentResource;
        _ = Problems.UnknownConsentMetadata;
        _ = Problems.MissingMetadataValue;
        _ = Problems.MissingMetadata;
        _ = Problems.InvalidResourceCombination;
        _ = Problems.ConsentCantBeRevoked;
        _ = Problems.ConsentRevoked;
        _ = Problems.MissMatchConsentParty;
        _ = Problems.ConsentExpired;
        _ = Problems.ConsentNotAccepted;
        _ = Problems.ConsentCantBeRejected;
        _ = Problems.ConsentWithIdAlreadyExist;
        _ = Problems.UnsupportedEntityType;
        _ = Problems.EntityTypeNotFound;
        _ = Problems.EntityVariantNotFoundOrInvalid;
        _ = Problems.MissingRightHolder;
        _ = Problems.ConnectionEntitiesDoNotExist;
        _ = Problems.MissingConnection;
        _ = Problems.PartyNotFound;
        _ = Problems.PersonInputRequiredForPersonAssignment;
        _ = Problems.AgentHasExistingDelegations;
        _ = Problems.PersonLookupFailedToManyErrors;
        _ = Problems.InvalidResource;
        _ = Problems.NotAuthorizedForDelegationRequest;
        _ = Problems.DelegationPolicyRuleWriteFailed;
        _ = Problems.AssignmentNotFound;
        _ = Problems.RequestNotFound;
        _ = Problems.RequestCreationFailed;
    }

    [Fact]
    public void ValidationErrors_AllDescriptors_ShouldBeAccessible()
    {
        _ = ValidationErrors.Required;
        _ = ValidationErrors.InvalidPartyUrn;
        _ = ValidationErrors.InvalidResource;
        _ = ValidationErrors.MissingPolicy;
        _ = ValidationErrors.MissingDelegableRights;
        _ = ValidationErrors.ToManyDelegationsToRevoke;
        _ = ValidationErrors.EntityNotExists;
        _ = ValidationErrors.RoleNotExists;
        _ = ValidationErrors.DisallowedEntityType;
        _ = ValidationErrors.InvalidQueryParameter;
        _ = ValidationErrors.AssignmentHasActiveConnections;
        _ = ValidationErrors.PackageNotExists;
        _ = ValidationErrors.ResourceNotExists;
        _ = ValidationErrors.TimeNotInFuture;
        _ = ValidationErrors.EmptyList;
        _ = ValidationErrors.ConsentNotFound;
        _ = ValidationErrors.InvalidResourceContext;
        _ = ValidationErrors.UserNotAuthorized;
        _ = ValidationErrors.InvalidRedirectUrl;
        _ = ValidationErrors.PackageIsNotAssignableToRecipient;
        _ = ValidationErrors.InvalidRole;
        _ = ValidationErrors.InvalidPackage;
        _ = ValidationErrors.DelegationHasActiveConnections;
        _ = ValidationErrors.MissingAssignment;
        _ = ValidationErrors.PackageIsNotDelegable;
        _ = ValidationErrors.InvalidExternalIdentifiers;
        _ = ValidationErrors.RequestNotFound;
        _ = ValidationErrors.RequestUnsupportedStatusUpdate;
    }
}
