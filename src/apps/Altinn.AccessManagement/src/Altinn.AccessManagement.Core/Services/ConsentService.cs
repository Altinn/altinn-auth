using System.Diagnostics;
using System.Text;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.Register;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using static Altinn.Authorization.ABAC.Constants.XacmlConstants;

namespace Altinn.AccessManagement.Core.Services
{
    /// <summary>
    /// Service for handling consent
    /// </summary>
    /// <remarks>
    /// Service responsible for consent functionality
    /// </remarks>
    public class ConsentService : IConsent
    {
        private readonly IConsentRepository _consentRepository;
        private readonly IResourceRegistryClient _resourceRegistryClient;
        private readonly IAMPartyService _ampartyService;
        private readonly IMemoryCache _memoryCache;
        private readonly TimeProvider _timeProvider;
        private readonly GeneralSettings _generalSettings;
        private readonly AppDbContext _db;
        private readonly ILogger<ConsentService> _logger;
        private readonly IConsentDelegationCheckService _consentDelegationCheckService;

        private const string ResourceParam = "Resource";

        public ConsentService(
            AppDbContext db,
            ILogger<ConsentService> logger,
            IConsentRepository consentRepository,
            IResourceRegistryClient resourceRegistryClient,
            IAMPartyService ampartyService,
            IMemoryCache memoryCache,
            TimeProvider timeProvider,
            IOptions<GeneralSettings> generalSettings,
            IConsentDelegationCheckService consentDelegationCheckService)
        {
            _db = db;
            _logger = logger;
            _consentRepository = consentRepository;
            _resourceRegistryClient = resourceRegistryClient;
            _ampartyService = ampartyService;
            _memoryCache = memoryCache;
            _timeProvider = timeProvider;
            _generalSettings = generalSettings.Value;
            _consentDelegationCheckService = consentDelegationCheckService;
        }

        /// <inheritdoc/>
        public async Task<Result<ConsentRequestDetailsWrapper>> CreateRequest(ConsentRequest consentRequest, ConsentPartyUrn performedByParty, CancellationToken cancellationToken)
        {
            Result<ConsentRequest> result = await ValidateAndSetInternalIdentifiers(consentRequest, cancellationToken);

            if (result.IsProblem)
            {
                return result.Problem;
            }

            performedByParty = await MapFromExternalIdenity(performedByParty, cancellationToken);

            ConsentRequestDetails requestDetails = await _consentRepository.CreateRequest(result.Value, performedByParty, cancellationToken);
            if (requestDetails == null)
            {
                // Need to verify if it is null because of duplicate
                ConsentRequestDetails consentRequestDetails = await _consentRepository.GetRequest(consentRequest.Id, cancellationToken);
                if (consentRequestDetails != null
                    && consentRequest.Id == consentRequestDetails.Id
                    && consentRequest.From == consentRequestDetails.From
                    && consentRequest.To == consentRequestDetails.To)
                {
                    consentRequestDetails.From = await MapToExternalIdentity(consentRequestDetails.From, cancellationToken);
                    consentRequestDetails.To = await MapToExternalIdentity(consentRequestDetails.To, cancellationToken);
                    if (consentRequestDetails.HandledBy != null)
                    {
                        consentRequestDetails.HandledBy = await MapToExternalIdentity(consentRequestDetails.HandledBy, cancellationToken);
                    }

                    if (consentRequestDetails.RequiredDelegator != null)
                    {
                        consentRequestDetails.RequiredDelegator = await MapToExternalIdentity(consentRequestDetails.RequiredDelegator, cancellationToken);
                    }

                    consentRequestDetails.ViewUri = GetConsentViewUri(consentRequestDetails.Id);

                    // We dont validate resource or other parameters when creating a consent request and it exist for the same parties  
                    ConsentRequestDetailsWrapper requestWrapper = new()
                    {
                        ConsentRequest = consentRequestDetails,
                        AlreadyExisted = true
                    };

                    return requestWrapper;
                }

                return Problems.ConsentWithIdAlreadyExist.Create([new("requestId", consentRequest.Id.ToString())]);
            }

            requestDetails.From = await MapToExternalIdentity(requestDetails.From, cancellationToken);
            requestDetails.To = await MapToExternalIdentity(requestDetails.To, cancellationToken);
            if (requestDetails.HandledBy != null)
            {
                requestDetails.HandledBy = await MapToExternalIdentity(requestDetails.HandledBy, cancellationToken);
            }

            if (requestDetails.RequiredDelegator != null)
            {
                requestDetails.RequiredDelegator = await MapToExternalIdentity(requestDetails.RequiredDelegator, cancellationToken);
            }

            foreach (ConsentRequestEvent consentRequestEvent in requestDetails.ConsentRequestEvents)
            {
                consentRequestEvent.PerformedBy = await MapToExternalIdentity(consentRequestEvent.PerformedBy, cancellationToken);
            }

            requestDetails.ViewUri = GetConsentViewUri(requestDetails.Id);
            ConsentRequestDetailsWrapper consentRequestDetailsWrapper = new()
            {
                ConsentRequest = requestDetails,
            };

            return consentRequestDetailsWrapper;
        }

        /// <inheritdoc/>
        public async Task<Result<ConsentRequestDetails>> RejectRequest(Guid consentRequestId, Guid performedByParty, CancellationToken cancellationToken)
        {
            ConsentRequestDetails details = await _consentRepository.GetRequest(consentRequestId, cancellationToken);
            if (details.ConsentRequestStatus == ConsentRequestStatusType.Rejected)
            {
                await SetExternalIdentities(details, cancellationToken);
                return details;
            }

            if (details.ConsentRequestStatus != ConsentRequestStatusType.Created)
            {
                return Problems.ConsentCantBeRejected;
            }

            try
            {
                await _consentRepository.RejectConsentRequest(consentRequestId, performedByParty, cancellationToken);
            }
            catch (InvalidOperationException)
            {
                await _consentRepository.GetRequest(consentRequestId, cancellationToken);

                if (details.ConsentRequestStatus == ConsentRequestStatusType.Rejected)
                {
                    await SetExternalIdentities(details, cancellationToken);
                    return details;
                }

                if (details.ConsentRequestStatus != ConsentRequestStatusType.Created)
                {
                    return Problems.ConsentCantBeRejected;
                }

                throw;
            }

            ConsentRequestDetails updated = await _consentRepository.GetRequest(consentRequestId, cancellationToken);
            return updated;
        }

        /// <inheritdoc/>
        public async Task<Result<Consent>> GetConsent(Guid consentRequestId, ConsentPartyUrn from, ConsentPartyUrn to, CancellationToken cancellationToken)
        {
            MultipleProblemBuilder errors = default;
            ConsentRequestDetails consentRequest = await _consentRepository.GetRequest(consentRequestId, cancellationToken);

            // Map from external to internal identies 
            from = await MapFromExternalIdenity(from, cancellationToken);
            to = await MapFromExternalIdenity(to, cancellationToken);

            if (consentRequest == null)
            {
                return Problems.ConsentNotFound;
            }
            else
            {
                errors = ValidateGetConsentRequest(from, to, ref errors, consentRequest);

                if (errors.TryBuild(out var errorResult))
                {
                    return errorResult;
                }

                Consent consent = new()
                {
                    Id = consentRequest.Id,
                    Consented = consentRequest.Consented.Value,
                    From = await MapToExternalIdentity(consentRequest.From, cancellationToken),
                    To = await MapToExternalIdentity(consentRequest.To, cancellationToken),
                    ValidTo = consentRequest.ValidTo,
                    ConsentRights = consentRequest.ConsentRights
                };

                if (consentRequest.HandledBy != null)
                {
                    consent.HandledBy = await MapToExternalIdentity(consentRequest.HandledBy, cancellationToken);
                }

                return consent;
            }
        }

        /// <inheritdoc/>
        public async Task<Result<Consent>> GetConsent(Guid consentRequestId, CancellationToken cancellationToken)
        {
            ValidationErrorBuilder errors = default;
            ConsentRequestDetails consentRequest = await _consentRepository.GetRequest(consentRequestId, cancellationToken);

            if (consentRequest == null)
            {
                errors.Add(ValidationErrors.ConsentNotFound, "/from");

                if (errors.TryBuild(out var errorResultStart))
                {
                    return errorResultStart;
                }

                // Should not be possible to get here
                throw new UnreachableException($"Consent request with id {consentRequestId} not found");
            }
            else
            {
                Consent consent = new()
                {
                    Id = consentRequest.Id,
                    From = consentRequest.From,
                    To = consentRequest.To,
                    ValidTo = consentRequest.ValidTo,
                    ConsentRights = consentRequest.ConsentRights,
                    ConsentRequestEvents = AddExpiredEventIfConsentIsExpired(consentRequest.ConsentRequestEvents, consentRequest.ValidTo, consentRequest.To),
                    TemplateId = consentRequest.TemplateId,
                    HandledBy = consentRequest.HandledBy,
                };

                consent.Context = await _consentRepository.GetConsentContext(consentRequestId, cancellationToken);

                return consent;
            }
        }

        private MultipleProblemBuilder ValidateGetConsentRequest(ConsentPartyUrn from, ConsentPartyUrn to, ref MultipleProblemBuilder problemsBUilders, ConsentRequestDetails consentRequest)
        {
            if (!to.Equals(consentRequest.To) && !to.Equals(consentRequest.HandledBy))
            {
                problemsBUilders.Add(Problems.ConsentNotFound);
            }

            if (!from.Equals(consentRequest.From))
            {
                problemsBUilders.Add(Problems.MissMatchConsentParty);
            }

            if (consentRequest.ValidTo < _timeProvider.GetUtcNow())
            {
                problemsBUilders.Add(Problems.ConsentExpired);
            }

            if (consentRequest.ConsentRequestStatus == ConsentRequestStatusType.Created)
            {
                problemsBUilders.Add(Problems.ConsentNotAccepted);
            }
            else if (consentRequest.ConsentRequestStatus == ConsentRequestStatusType.Revoked)
            {
                problemsBUilders.Add(Problems.ConsentRevoked);
            }
            else if (consentRequest.ConsentRequestStatus != ConsentRequestStatusType.Accepted)
            {
                problemsBUilders.Add(Problems.ConsentNotAccepted);
            }

            return problemsBUilders;
        }

        /// <inheritdoc/>
        public async Task<Result<ConsentRequestDetails>> GetRequest(Guid consentRequestId, ConsentPartyUrn performedByParty, bool useInternalIdenties, CancellationToken cancellationToken)
        {
            ConsentRequestDetails details = await _consentRepository.GetRequest(consentRequestId, cancellationToken);
            if (details == null)
            {
                return Problems.ConsentNotFound;
            }

            if (!useInternalIdenties)
            {
                details.To = await MapToExternalIdentity(details.To, cancellationToken);
                details.From = await MapToExternalIdentity(details.From, cancellationToken);

                if (details.HandledBy != null)
                {
                    details.HandledBy = await MapToExternalIdentity(details.HandledBy, cancellationToken);
                }

                if (details.RequiredDelegator != null)
                {
                    details.RequiredDelegator = await MapToExternalIdentity(details.RequiredDelegator, cancellationToken);
                }

                foreach (ConsentRequestEvent consentRequestEvent in details.ConsentRequestEvents)
                {
                    consentRequestEvent.PerformedBy = await MapToExternalIdentity(consentRequestEvent.PerformedBy, cancellationToken);
                }
            }

            if (performedByParty.IsOrganizationId(out OrganizationNumber organizationNumber))
            {
                bool isTo = details.To.IsOrganizationId(out OrganizationNumber toOrganizationNumber) && toOrganizationNumber.Equals(organizationNumber);
                bool isHandledBy = details.HandledBy != null && details.HandledBy.IsOrganizationId(out OrganizationNumber handledByOrganizationNumber) && handledByOrganizationNumber.Equals(organizationNumber);

                if (!isTo && !isHandledBy)
                {
                    return Problems.NotAuthorizedForConsentRequest;
                }
            }
            else if (performedByParty.IsPartyUuid(out Guid partyUuid))
            {
                bool isAuthorized = await AuthorizeUserForConsentRequest(partyUuid, details, cancellationToken);
                if (!isAuthorized)
                {
                    return Problems.NotAuthorizedForConsentRequest;
                }
            }

            details.ViewUri = GetConsentViewUri(details.Id);
            details.ConsentRequestEvents = AddExpiredEventIfConsentIsExpired(details.ConsentRequestEvents, details.ValidTo, details.To);

            return details;
        }

        /// <inheritdoc/>
        public async Task<Result<ConsentRequestDetails>> AcceptRequest(Guid consentRequestId, Guid performedByParty, ConsentContext context, CancellationToken cancellationToken)
        {
            ConsentRequestDetails details = await _consentRepository.GetRequest(consentRequestId, cancellationToken);

            if (details == null)
            {
                return Problems.ConsentNotFound;
            }

            bool isAuthorized = await AuthorizeUserForConsentRequest(performedByParty, details, cancellationToken);

            if (!isAuthorized)
            {
                return Problems.NotAuthorizedForConsentRequest;
            }

            if (details.ConsentRequestStatus == ConsentRequestStatusType.Accepted)
            {
                await SetExternalIdentities(details, cancellationToken);
                return details;
            }

            if (details.ConsentRequestStatus != ConsentRequestStatusType.Created)
            {
                return Problems.ConsentCantBeAccepted;
            }

            ValidationErrorBuilder errors = default;
            ValidateContext(context, ref errors);

            if (errors.TryBuild(out var beforeErrorREsult))
            {
                return beforeErrorREsult;
            }

            try
            {
                await _consentRepository.AcceptConsentRequest(consentRequestId, performedByParty, context, cancellationToken);
            }
            catch (Exception)
            {
                details = await _consentRepository.GetRequest(consentRequestId, cancellationToken);

                if (details.ConsentRequestStatus == ConsentRequestStatusType.Accepted)
                {
                    await SetExternalIdentities(details, cancellationToken);
                    return details;
                }

                if (details.ConsentRequestStatus != ConsentRequestStatusType.Created)
                {
                    return Problems.ConsentCantBeAccepted;
                }

                throw;
            }

            ConsentRequestDetails updated = await _consentRepository.GetRequest(consentRequestId, cancellationToken);
            return updated;
        }

        /// <inheritdoc/>
        public async Task<Result<ConsentRequestDetails>> RevokeConsent(Guid consentRequestId, Guid performedByParty, CancellationToken cancellationToken)
        {
            ConsentRequestDetails details = await _consentRepository.GetRequest(consentRequestId, cancellationToken);
            if (details.ConsentRequestStatus == ConsentRequestStatusType.Revoked)
            {
                await SetExternalIdentities(details, cancellationToken);
                return details;
            }

            if (details.ConsentRequestStatus != ConsentRequestStatusType.Accepted)
            {
                return Problems.ConsentCantBeRevoked;
            }

            try
            {
                await _consentRepository.Revoke(consentRequestId, performedByParty, cancellationToken);
            }
            catch (Exception)
            {
                details = await _consentRepository.GetRequest(consentRequestId, cancellationToken);

                if (details.ConsentRequestStatus == ConsentRequestStatusType.Revoked)
                {
                    await SetExternalIdentities(details, cancellationToken);
                    return details;
                }

                if (details.ConsentRequestStatus != ConsentRequestStatusType.Accepted)
                {
                    return Problems.ConsentCantBeRevoked;
                }

                throw;
            }

            ConsentRequestDetails updated = await _consentRepository.GetRequest(consentRequestId, cancellationToken);
            return updated;
        }

        /// <inheritdoc/>
        public async Task<string> GetRequestRedirectUrl(Guid consentRequestId, CancellationToken cancellationToken)
        {
            ConsentRequestDetails details = await _consentRepository.GetRequest(consentRequestId, cancellationToken);
            if (details == null)
            {
                return null;
            }

            return details.RedirectUrl;
        }

        private async Task SetExternalIdentities(ConsentRequestDetails details, CancellationToken cancellationToken)
        {
            details.From = await MapToExternalIdentity(details.From, cancellationToken);
            details.To = await MapToExternalIdentity(details.To, cancellationToken);
            foreach (ConsentRequestEvent consentRequestEvent in details.ConsentRequestEvents)
            {
                consentRequestEvent.PerformedBy = await MapToExternalIdentity(consentRequestEvent.PerformedBy, cancellationToken);
            }
        }

        private async Task<ConsentPartyUrn> MapFromExternalIdenity(ConsentPartyUrn consentPartyUrn, CancellationToken cancellationToken)
        {
            if (consentPartyUrn.IsPersonId(out PersonIdentifier personIdentifier))
            {
                return await GetInternalIdentifier(personIdentifier, cancellationToken);
            }
            else if (consentPartyUrn.IsOrganizationId(out OrganizationNumber organizationNumber))
            {
                return await GetInternalIdentifier(organizationNumber, cancellationToken);
            }

            return consentPartyUrn;
        }

        private async Task<ConsentPartyUrn> MapToExternalIdentity(ConsentPartyUrn consentPartyUrn, CancellationToken cancellationToken)
        {
            if (consentPartyUrn.IsPartyUuid(out Guid partyUuid))
            {
                if (_memoryCache.TryGetValue(partyUuid, out ConsentPartyUrn consentPartyUrnCache))
                {
                    return consentPartyUrnCache;
                }
                else
                {
                    ConsentPartyUrn consentPartyUrnFromRegister = await GetExternalIdentifier(partyUuid, cancellationToken);
                    _memoryCache.Set(partyUuid, consentPartyUrnFromRegister, TimeSpan.FromMinutes(5));
                    return consentPartyUrnFromRegister;
                }
            }

            return consentPartyUrn;
        }

        private async Task<ConsentPartyUrn> GetExternalIdentifier(Guid guid, CancellationToken cancellationToken)
        {
            MinimalParty party = await _ampartyService.GetByUid(guid, cancellationToken);

            if (party == null)
            {
                throw new ArgumentException($"Party with guid {guid} not found");
            }

            if (!string.IsNullOrEmpty(party.PersonId))
            {
                return ConsentPartyUrn.PersonId.Create(PersonIdentifier.Parse(party.PersonId));
            }

            if (!string.IsNullOrEmpty(party.OrganizationId))
            {
                return ConsentPartyUrn.OrganizationId.Create(OrganizationNumber.Parse(party.OrganizationId));
            }

            throw new ArgumentException($"Party with guid {guid} is not valid consent party");
        }

        private async Task<ConsentPartyUrn> GetInternalIdentifier(OrganizationNumber organizationNumber, CancellationToken cancellationToken)
        {
            MinimalParty party = await _ampartyService.GetByOrgNo(organizationNumber, cancellationToken);
            if (party == null)
            {
                return null;
            }

            return ConsentPartyUrn.PartyUuid.Create(party.PartyUuid);
        }

        private async Task<ConsentPartyUrn> GetInternalIdentifier(PersonIdentifier personIdentifier, CancellationToken cancellationToken)
        {
            MinimalParty party = await _ampartyService.GetByPersonNo(personIdentifier, cancellationToken);
            if (party == null)
            {
                return null;
            }

            return ConsentPartyUrn.PartyUuid.Create(party.PartyUuid);
        }

        /// <summary>
        /// This method iterates throug the consent request and verifies that user is allowe to delegate all rights requested in consent
        /// Currently no sub resources is supported. Ignores sub resources in response.
        /// </summary>
        private async Task<bool> AuthorizeUserForConsentRequest(Guid userUuid, ConsentRequestDetails consentRequest, CancellationToken cancellationToken)
        {
            Guid fromParty = ((ConsentPartyUrn.PartyUuid)consentRequest.From).Value;
            if (consentRequest.RequiredDelegator != null && consentRequest.RequiredDelegator.IsPartyUuid(out Guid requiredDelegatorPartyUuid))
            {
                if (requiredDelegatorPartyUuid != userUuid)
                {
                    return false; // User is not the required delegator
                }
            }

            var uuids = new List<Guid> { fromParty, userUuid };
            var entities = await _db.Entities
                .AsNoTracking()
                .Where(e => uuids.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, cancellationToken);

            if (!entities.TryGetValue(fromParty, out Entity fromEntity))
            {
                return false;
            }

            if (!entities.TryGetValue(userUuid, out Entity userEntity))
            {
                return false;
            }

            foreach (ConsentRight consentRight in consentRequest.ConsentRights)
            {
                if (!await AuthorizeForConsentRight(fromEntity, userEntity, consentRight))
                {
                    return false;
                }
            }

            return true;
        }

        private async Task<bool> AuthorizeForConsentRight(Entity party, Entity authenticatedUser, ConsentRight consentRight)
        {
            if (consentRight.Resource == null || consentRight.Resource.Count != 1)
            {
                return false;
            }

            if (party.Id == Guid.Empty)
            {
                return false;
            }

            // A ConsentRight should only have one resource. Currently no support for subresources as part of a consent request.
            ConsentResourceAttribute resource = consentRight.Resource[0];

            ConsentDelegationCheckResult checkResult = await _consentDelegationCheckService.CheckDelegatableRights(
                authenticatedUserUuid: authenticatedUser.Id,
                partyUuid: party.Id,
                resourceIdentifier: resource.Value);

            if (!checkResult.IsSuccess)
            {
                return false;
            }

            foreach (string action in consentRight.Action)
            {
                bool actionMatch = checkResult.DelegatableActions.Any(a =>
                    a.Replace(MatchAttributeIdentifiers.ActionId + ":", string.Empty).Equals(action, StringComparison.OrdinalIgnoreCase));

                if (!actionMatch)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Validates and sets internal identifiers for the consent request
        /// - Validates that the from and to party is valid
        /// - Validates that resources requested in consent is valid
        /// - Validates that valid to time is valid
        /// </summary>
        private async Task<Result<ConsentRequest>> ValidateAndSetInternalIdentifiers(ConsentRequest consentRequest, CancellationToken cancelactionToken)
        {
            ValidationErrorBuilder validationErrorsBuilder = default;
            MultipleProblemBuilder problemsBuilder = default;
            Result<ConsentPartyUrn> fromParty = await ValidatePartyFromExternalIdentity(consentRequest.From, cancelactionToken);
            if (fromParty.IsProblem)
            {
                return fromParty.Problem;
            }
            else
            {
                consentRequest.From = fromParty.Value;
            }

            Result<ConsentPartyUrn> toParty = await ValidatePartyFromExternalIdentity(consentRequest.To, cancelactionToken);
            if (toParty.IsProblem)
            {
                return toParty.Problem;
            }
            else
            {
                consentRequest.To = toParty.Value;
            }

            if (consentRequest.RequiredDelegator != null)
            {
                Result<ConsentPartyUrn> requiredDelegator = await ValidatePartyFromExternalIdentity(consentRequest.RequiredDelegator, cancelactionToken);
                if (requiredDelegator.IsProblem)
                {
                    return requiredDelegator.Problem;
                }
                else
                {
                    consentRequest.RequiredDelegator = requiredDelegator.Value;
                }
            }

            if (consentRequest.HandledBy != null)
            {
                Result<ConsentPartyUrn> handledBy = await ValidatePartyFromExternalIdentity(consentRequest.HandledBy, cancelactionToken);
                if (handledBy.IsProblem)
                {
                    return handledBy.Problem;
                }
                else
                {
                    consentRequest.HandledBy = handledBy.Value;
                }
            }

            validationErrorsBuilder = ValidateValidTo(consentRequest, validationErrorsBuilder);

            string templateId = string.Empty;

            if (consentRequest.ConsentRights == null || consentRequest.ConsentRights.Count == 0)
            {
                validationErrorsBuilder.Add(ValidationErrors.EmptyList, ResourceParam);
            }
            else
            {
                templateId = string.Empty;
                for (int rightIndex = 0; rightIndex < consentRequest.ConsentRights.Count; rightIndex++)
                {
                    (problemsBuilder, templateId) = await ValidateConsentRight(consentRequest, problemsBuilder, rightIndex, templateId, cancelactionToken);
                }
            }

            if (consentRequest.RedirectUrl != null && !IsValidUrl(consentRequest.RedirectUrl))
            {
                validationErrorsBuilder.Add(ValidationErrors.InvalidRedirectUrl, "RedirectUrl");
            }

            if (validationErrorsBuilder.TryBuild(out var errorResult))
            {
                problemsBuilder.Add(errorResult);
            }

            if (problemsBuilder.TryBuild(out var problemResult))
            {
                return problemResult;
            }

            ConsentTemplate consentTemplate = await GetTemplate(templateId, cancelactionToken);

            consentRequest.TemplateVersion = consentTemplate.Version;
            consentRequest.TemplateId = consentTemplate.Id;

            return consentRequest;
        }

        private static void ValidateContext(ConsentContext context, ref ValidationErrorBuilder errors)
        {
            if (context == null)
            {
                errors.Add(ValidationErrors.Required, "Context");
            }
        }

        private async Task<(MultipleProblemBuilder Errors, string TemplateId)> ValidateConsentRight(ConsentRequest consentRequest, MultipleProblemBuilder problemsBuilder, int rightIndex, string templateId, CancellationToken cancelactionToken)
        {
            ConsentRight consentRight = consentRequest.ConsentRights[rightIndex];
            ValidationErrorBuilder validationErrors = default;

            if (consentRight.Action == null || consentRight.Action.Count == 0)
            {
                validationErrors.Add(ValidationErrors.Required, $"/consentRight/{rightIndex}/action");
            }

            if (consentRight.Resource == null || consentRight.Resource.Count == 0 || consentRight.Resource.Count > 1)
            {
                problemsBuilder.Add(Problems.InvalidConsentResource);
            }
            else
            {
                ConsentResourceAttribute consentResourceAttribute = consentRight.Resource[0];
                ServiceResource resourceDetails = await _resourceRegistryClient.GetResource(consentResourceAttribute.Value, cancelactionToken);
                if (resourceDetails == null)
                {
                    problemsBuilder.Add(Problems.InvalidConsentResource);
                }
                else if (!resourceDetails.ResourceType.Equals(Models.ResourceRegistry.ResourceType.Consent))
                {
                    problemsBuilder.Add(Problems.InvalidConsentResource);
                }
                else
                {
                    ValidateConsentMetadata(ref problemsBuilder, rightIndex, consentRight, resourceDetails);
                    consentResourceAttribute.Version = resourceDetails.VersionId.ToString();
                }

                if (resourceDetails != null && string.IsNullOrEmpty(templateId))
                {
                    templateId = resourceDetails.ConsentTemplate;
                }
                else if (resourceDetails != null && !templateId.Equals(resourceDetails.ConsentTemplate, StringComparison.InvariantCultureIgnoreCase))
                {
                    problemsBuilder.Add(Problems.InvalidResourceCombination);
                }
            }

            if (validationErrors.TryBuild(out var errorResult))
            {
                problemsBuilder.Add(errorResult);
            }

            return (problemsBuilder, templateId);
        }

        private static void ValidateConsentMetadata(ref MultipleProblemBuilder problemsBuilder, int rightIndex, ConsentRight consentRight, ServiceResource resourceDetails)
        {
            if (consentRight.Metadata != null && consentRight.Metadata.Count > 0)
            {
                foreach (KeyValuePair<string, string> metaData in consentRight.Metadata)
                {
                    if (resourceDetails.ConsentMetadata == null || (!resourceDetails.ConsentMetadata.ContainsKey(metaData.Key.ToLower())))
                    {
                        problemsBuilder.Add(Problems.UnknownConsentMetadata.Create([new("key", metaData.Key.ToLower())]));
                    }

                    if (string.IsNullOrEmpty(metaData.Value))
                    {
                        problemsBuilder.Add(Problems.MissingMetadataValue.Create([new($"ConsentRight index: {rightIndex}, key", metaData.Key.ToLower())]));
                    }
                }
            }

            ValidateRequiredMetadata(ref problemsBuilder, consentRight, resourceDetails);
        }

        private static void ValidateRequiredMetadata(ref MultipleProblemBuilder problemsBuilder, ConsentRight consentRight, ServiceResource resourceDetails)
        {
            if (resourceDetails.ConsentMetadata != null)
            {
                foreach (string key in resourceDetails.ConsentMetadata.Keys.Select(consentMetadata => consentMetadata))
                {
                    if (consentRight.Metadata == null || !consentRight.Metadata.ContainsKey(key))
                    {
                        problemsBuilder.Add(Problems.MissingMetadata.Create([new("key", key.ToLower())]));
                    }
                }
            }
        }

        private static ValidationErrorBuilder ValidateValidTo(ConsentRequest consentRequest, ValidationErrorBuilder errors)
        {
            if (consentRequest.ValidTo < DateTime.UtcNow && consentRequest.CreatedTime == null)
            {
                errors.Add(ValidationErrors.TimeNotInFuture, "ValidTo");
            }

            return errors;
        }

        private async Task<Result<ConsentPartyUrn>> ValidatePartyFromExternalIdentity(ConsentPartyUrn consentPartyUrn, CancellationToken cancelactionToken)
        {
            ConsentPartyUrn to = await MapFromExternalIdenity(consentPartyUrn, cancelactionToken);
            if (to == null)
            {
                if (consentPartyUrn.IsOrganizationId(out _))
                {
                    return Problems.InvalidOrganizationIdentifier.Create([new("orgnr", consentPartyUrn.ToString())]);
                }
                else if (consentPartyUrn.IsPersonId(out _))
                {
                    return Problems.InvalidPersonIdentifier.Create([new("fnumber", consentPartyUrn.ToString())]);
                }
            }

            return to;
        }

        private async Task<ConsentTemplate> GetTemplate(string templateId, CancellationToken cancellationToken)
        {
            ConsentTemplate consentTemplate = await _resourceRegistryClient.GetConsentTemplate(templateId, null, cancellationToken);
            return consentTemplate ?? throw new ArgumentException($"Consent template with id {templateId} not found");
        }

        /// <summary>
        /// Generates the URI for the consent view in the Altinn UI portal.
        /// </summary>
        private string GetConsentViewUri(Guid requesteId)
        {
            return $"https://am.ui.{_generalSettings.Hostname}/accessmanagement/ui/consent/request?id={requesteId}";
        }

        public static bool IsValidUrl(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uriResult))
            {
                return false;
            }

            if (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps)
            {
                return true;
            }

            // Allow custom URI schemes for mobile app deep links (RFC 3986: letter followed by letters, digits, +, -, .)
            return System.Text.RegularExpressions.Regex.IsMatch(uriResult.Scheme, @"^[a-z][a-z0-9+\-.]*$");
        }

        public async Task<Result<List<ConsentRequestDetails>>> GetRequestsForParty(Guid coveredByParty, bool useInternalIdenties, CancellationToken cancellationToken)
        {
            Result<List<ConsentRequestDetails>> requests = await _consentRepository.GetRequestsForParty(coveredByParty, cancellationToken);

            if (requests.Value != null)
            {
                foreach (var req in requests.Value)
                {
                    req.ConsentRequestEvents = AddExpiredEventIfConsentIsExpired(req.ConsentRequestEvents, req.ValidTo, req.To);
                }
            }

            return requests;
        }

        /// <inheritdoc/>
        public async Task<Result<int>> GetConsentRequestCountForParty(Guid offeredByParty, ConsentRequestStatusType status, CancellationToken cancellationToken)
        {
            return await _consentRepository.GetConsentRequestCountForParty(offeredByParty, status, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<Result<List<ConsentStatusChange>>> GetConsentEventsForParty(ConsentPartyUrn consentReceiver, ConsentEventsQuery query, int pageSize, CancellationToken cancellationToken)
        {
            ValidationErrorBuilder validationErrorBuilder = default;

            if (pageSize <= 0 || pageSize > 1000)
            {
                validationErrorBuilder.Add(ValidationErrors.InvalidPageSizeForConsentEvents, "pageSize");
            }

            if (validationErrorBuilder.TryBuild(out var errorResult))
            {
                return errorResult;
            }

            Result<ConsentPartyUrn> consentedToPartyResult = await ValidatePartyFromExternalIdentity(consentReceiver, cancellationToken);
            if (consentedToPartyResult.IsProblem)
            {
                return consentedToPartyResult.Problem;
            }

            consentedToPartyResult.Value.IsPartyUuid(out Guid partyUuid);
            Result<List<ConsentStatusChange>> result = await _consentRepository.GetConsentEventsForParty(partyUuid, query, pageSize, cancellationToken);

            if (result.IsProblem)
            {
                return result.Problem;
            }

            return result.Value;
        }

        private List<ConsentRequestEvent> AddExpiredEventIfConsentIsExpired(List<ConsentRequestEvent> consentRequestEvents, DateTimeOffset validTo, ConsentPartyUrn to)
        {
            if (validTo < _timeProvider.GetUtcNow() && !consentRequestEvents.Exists(r => r.EventType.Equals(ConsentRequestEventType.Expired)))
            {
                var newEvent = new ConsentRequestEvent
                {
                    EventType = ConsentRequestEventType.Expired,
                    Created = validTo,
                    PerformedBy = to
                };
                return [.. consentRequestEvents, newEvent];
            }

            return consentRequestEvents;
        }
    }
}
