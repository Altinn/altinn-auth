using Altinn.AccessManagement.Api.Enduser.Models;
using Altinn.AccessManagement.Core;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Helpers;
using Altinn.AccessManagement.Core.Models.Profile;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Api.Enduser.Validation;

public class InputValidation(
    IHttpContextAccessor httpContextAccessor,
    IUserProfileLookupService userProfileLookupService,
    ConnectionQuery connectionQuery,
    IEntityService entityService
    ) : IInputValidation
{
    public async Task<Result<Entity>> SanitizeToInput(Guid party, Guid? toParty, PersonInput personInput, Action<SanitizeOptions> configureSanitizeOptions, CancellationToken cancellationToken = default)
    {
        ValidationErrorBuilder errorBuilder = default;
        var options = new SanitizeOptions();
        configureSanitizeOptions(options);

        if (toParty is { } toPartyUuid && toPartyUuid != Guid.Empty)
        {
            var toEntity = await entityService.GetEntity(toPartyUuid, cancellationToken);
            if (toEntity is null)
            {
                errorBuilder.Add(ValidationErrors.EntityNotExists, $"QUERY/{options.ToParameterName}", [new(options.ToParameterName, $"Cannot find any parties with uuid '{toParty}'.")]);
            }

            if (errorBuilder.TryBuild(out var problem))
            {
                return problem;
            }

            if (toEntity is { })
            {
                if (options.EntitiesToValidateForAnyConnections.Contains(toEntity.TypeId))
                {
                    var connections = await connectionQuery.GetConnectionsFromOthersAsync(
                    new()
                    {
                        FromIds = [party],
                        ToIds = [toEntity.Id],
                    },
                    cancellationToken);

                    if (connections.Count == 0)
                    {
                        errorBuilder.Add(ValidationErrors.EntityNotExists, $"QUERY/{options.ToParameterName}", [new(options.ToParameterName, $"Cannot find any parties with uuid '{toParty}'.")]);
                    }                    
                }

                if (toEntity.TypeId == EntityTypeConstants.Person.Entity.Id && toEntity.DateOfDeath is not null)
                {
                    errorBuilder.Add(ValidationErrors.EntityNotExists, $"QUERY/{options.ToParameterName}", [new(options.ToParameterName, "Person not available for delegation (deceased).")]);
                }

                if (errorBuilder.TryBuild(out var validationError))
                {
                    return validationError;
                }

                return toEntity;
            }            
        }

        if (personInput is { })
        {
            return await LookupProfile(personInput, cancellationToken);
        }

        errorBuilder.Add(
            ValidationErrors.Required,
            [$"QUERY/{options.ToParameterName}", $"/{nameof(personInput.PersonIdentifier)}", $"/{nameof(personInput.LastName)}"],
            [new("params", $"Invalid combination of params. Either 'QUERY/{options.ToParameterName}' must be set or '/{nameof(personInput.PersonIdentifier)}' and '/{nameof(personInput.LastName)}'.")]
        );
        errorBuilder.TryBuild(out var problemInstance);
        return problemInstance;
    }

    private async Task<Result<Entity>> LookupProfile(PersonInput person, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(person);

        ValidationErrorBuilder errorBuilder = default;
        int authUserId = AuthenticationHelper.GetUserId(httpContextAccessor.HttpContext);

        if (string.IsNullOrWhiteSpace(person.PersonIdentifier))
        {
            errorBuilder.Add(ValidationErrors.Required, [$"/{nameof(person.PersonIdentifier)}"], [new("personIdentifier", "personIdentifier is null or empty.")]);
        }

        if (string.IsNullOrWhiteSpace(person.LastName))
        {
            errorBuilder.Add(ValidationErrors.Required, [$"/{nameof(person.LastName)}"], [new("lastName", "lastname is either null or empty.")]);
        }

        if (errorBuilder.TryBuild(out var problem))
        {
            return problem;
        }

        UserProfileLookup lookup = new();
        bool treatAsSsn = person.PersonIdentifier.Length == 11 && person.PersonIdentifier.All(char.IsDigit);
        if (treatAsSsn)
        {
            lookup.Ssn = person.PersonIdentifier;
        }
        else
        {
            lookup.Username = person.PersonIdentifier;
        }

        try
        {
            var profile = await userProfileLookupService.GetUserProfile(authUserId, lookup, person.LastName);
            if (profile is null)
            {
                errorBuilder.Add(ValidationErrors.InvalidExternalIdentifiers, [$"/{nameof(person.PersonIdentifier)}", $"/{nameof(person.LastName)}"], [new("personInput", ValidationErrorMessageTexts.PersonIdentifierLastNameInvalid)]);
            }
            else
            {
                var profileUuid = profile.UserUuid != Guid.Empty ? profile.UserUuid : profile.Party?.PartyUuid;
                if (profileUuid is { } value && profileUuid.Value != Guid.Empty)
                {
                    var entity = await entityService.GetEntity(value, cancellationToken);
                    if (entity is { })
                    {
                        if (entity.TypeId == EntityTypeConstants.Person && entity.DateOfDeath is not null)
                        {
                            errorBuilder.Add(ValidationErrors.InvalidExternalIdentifiers, [$"/{nameof(person.PersonIdentifier)}", $"/{nameof(person.LastName)}"], [new("personInput", "Person not available for delegation (deceased).")]);
                        }
                        else
                        {
                            return entity;
                        }
                    }
                    else
                    {
                        errorBuilder.Add(ValidationErrors.InvalidExternalIdentifiers, [$"/{nameof(person.PersonIdentifier)}", $"/{nameof(person.LastName)}"], [new("personInput", "person was found in profile register, but not in AM.")]);
                    }
                }
                else
                {
                    errorBuilder.Add(ValidationErrors.InvalidExternalIdentifiers, [$"/{nameof(person.PersonIdentifier)}", $"/{nameof(person.LastName)}"], [new("personInput", ValidationErrorMessageTexts.PersonIdentifierLastNameInvalid)]);
                }
            }
        }
        catch (TooManyFailedLookupsException)
        {
            return Problems.PersonLookupFailedToManyErrors;
        }

        errorBuilder.TryBuild(out var errors);
        return errors;
    }
}

public class SanitizeOptions
{
    public IReadOnlyCollection<Guid> EntitiesToValidateForAnyConnections { get; set; } = [];

    public IReadOnlyCollection<Guid> AllowedToEntityTypes { get; set; } = [];

    /// <summary>
    /// Name of the query parameter holding the target entity, used in validation
    /// error pointers so they match the parameter name the endpoint exposes.
    /// </summary>
    public string ToParameterName { get; set; } = "to";
}

public interface IInputValidation
{
    Task<Result<Entity>> SanitizeToInput(Guid party, Guid? toParty, PersonInput personInput, Action<SanitizeOptions> configureSanitizeOptions, CancellationToken cancellationToken = default);
}
