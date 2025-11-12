using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Core;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models.Profile;
using Altinn.AccessManagement.Core.Services;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Utils;

/// <summary>
/// Resolves the target (to) UUID for AddAssignment based on either:
/// - Provided 'to' UUID referencing a non-person entity resolved via <see cref="IEntityService"/>
/// - PersonInput (identifier + last name) resolved via <see cref="IUserProfileLookupService"/>
/// Internal implementation detail of the Enduser API.
/// </summary>
internal sealed class ToUuidResolver
{
    private readonly IEntityService _entityService;
    private readonly IUserProfileLookupService _userProfileLookupService;

    internal ToUuidResolver(IEntityService entityService, IUserProfileLookupService userProfileLookupService)
    {
        _entityService = entityService;
        _userProfileLookupService = userProfileLookupService;
    }

    internal sealed record ResolveToUuidResult(Guid ToUuid, IActionResult? ErrorResult)
    {
        public bool Success => ErrorResult is null;
    }

    /// <summary>
    /// Resolve target UUID for a non-person entity from <see cref="ConnectionInput"/> via <see cref="EntityService"/>.
    /// </summary>
    internal async Task<ResolveToUuidResult> ResolveWithConnectionInputAsync(Guid connectionInputToUuid, bool allowConnectionInputForPersonEntity, CancellationToken cancellationToken)
    {
        var entity = await _entityService.GetEntity(connectionInputToUuid, cancellationToken);
        if (entity == null)
        {
            return new ResolveToUuidResult(Guid.Empty, Problems.PartyNotFound.ToActionResult());
        }

        if (!allowConnectionInputForPersonEntity && entity.TypeId == EntityTypeConstants.Person.Id)
        {
            return new ResolveToUuidResult(Guid.Empty, Problems.PersonInputRequiredForPersonAssignment.ToActionResult());
        }

        return new ResolveToUuidResult(connectionInputToUuid, null);
    }

    /// <summary>
    /// Resolve target UUID for a person entity from <see cref="PersonInput"/> via <see cref="UserProfileLookupService"/>.
    /// </summary>
    internal async Task<ResolveToUuidResult> ResolveWithPersonInputAsync(PersonInput person, HttpContext httpContext, CancellationToken cancellationToken)
    {
        int authUserId = AuthenticationHelper.GetUserId(httpContext);

        string identifier = person.PersonIdentifier;
        string lastName = person.LastName;

        bool treatAsSsn = identifier.Length == 11 && identifier.All(char.IsDigit);

        UserProfileLookup lookup = new();
        if (treatAsSsn)
        {
            lookup.Ssn = identifier;
        }
        else
        {
            lookup.Username = identifier;
        }

        try
        {
            var profile = await _userProfileLookupService.GetUserProfile(authUserId, lookup, lastName);
            if (profile == null)
            {
                return new ResolveToUuidResult(Guid.Empty, Problems.InvalidPersonIdentifier.ToActionResult());
            }

            Guid? resolvedUuid = profile.UserUuid != Guid.Empty ? profile.UserUuid : profile.Party?.PartyUuid;
            if (!resolvedUuid.HasValue || resolvedUuid.Value == Guid.Empty)
            {
                return new ResolveToUuidResult(Guid.Empty, Problems.PartyNotFound.ToActionResult());
            }

            return new ResolveToUuidResult(resolvedUuid.Value, null);
        }
        catch (TooManyFailedLookupsException)
        {
            var problemDetails = Problems.InvalidPersonIdentifier.ToProblemDetails();
            var result = new ObjectResult(problemDetails) { StatusCode = StatusCodes.Status429TooManyRequests };
            return new ResolveToUuidResult(Guid.Empty, result);
        }
    }
}
