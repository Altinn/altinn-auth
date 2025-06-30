using System.Net;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Core.Errors;

/// <summary>
/// Problem descriptors for Register.
/// </summary>
public static class Problems
{
    private static readonly ProblemDescriptorFactory _factory
        = ProblemDescriptorFactory.New("AM");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor NotAuthorizedForConsentRequest { get; }
        = _factory.Create(0, HttpStatusCode.Forbidden, "Not authorized for consent");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor ConsentNotFound { get; }
    = _factory.Create(1, HttpStatusCode.NotFound, "Consent not found");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor ConsentCantBeAccepted { get; }  
    = _factory.Create(2, HttpStatusCode.BadRequest, "Consent have wrong status to be consented");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor InvalidOrganizationIdentifier { get; }
    = _factory.Create(3, HttpStatusCode.BadRequest, "Unknown organization. Could not be found");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor InvalidPersonIdentifier { get; }
    = _factory.Create(4, HttpStatusCode.BadRequest, "Unknown person. Could not be found");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor InvalidConsentResource { get; }
    = _factory.Create(5, HttpStatusCode.BadRequest, "Invalid consent resource");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor UnknownConsentMetadata { get; }
    = _factory.Create(6, HttpStatusCode.BadRequest, "Invalid consent metadata");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor MissingMetadataValue { get; }
    = _factory.Create(7, HttpStatusCode.BadRequest, "Missing metadata value");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor MissingMetadata { get; }
    = _factory.Create(8, HttpStatusCode.BadRequest, "Missing metadata");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor InvalidResourceCombination { get; }
    = _factory.Create(9, HttpStatusCode.BadRequest, "Invalid resource combination");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor ConsentCantBeRevoked { get; }
     = _factory.Create(10, HttpStatusCode.BadRequest, "Consent cant be revoked.Wrong status");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor ConsentRevoked { get; }
    = _factory.Create(11, HttpStatusCode.BadRequest, $"Consent is revoked");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor MissMatchConsentParty { get; }
    = _factory.Create(12, HttpStatusCode.BadRequest, $"The consented party does not match the party requested");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor ConsentExpired { get; }
    = _factory.Create(13, HttpStatusCode.BadRequest, $"Consent is expired");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor ConsentNotAccepted { get; }
    = _factory.Create(14, HttpStatusCode.BadRequest, $"Consent is not accepted");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor ConsentCantBeRejected { get; }
    = _factory.Create(15, HttpStatusCode.BadRequest,  $"Consent cant be rejected. Wrong status");

    /// <summary>Gets a <see cref="ProblemDescriptor"/>.</summary>
    public static ProblemDescriptor ConsentWithIdAlreadyExist { get; }
    = _factory.Create(16, HttpStatusCode.BadRequest, $"Consent with id already exist");
}
