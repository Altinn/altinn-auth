using System.Net.Mime;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Audit;
using Altinn.AccessMgmt.Core.Extensions;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.Register;
using Altinn.Authorization.ProblemDetails;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Mvc;

namespace Altinn.AccessManagement.Api.ServiceOwner.Controllers
{
    /// <summary>
    /// Connection service for service owner
    /// </summary>
    [ApiController]
    [Route("accessmanagement/api/v1/serviceowner/connections")]
    public class ConnectionsController(
        IServiceOwnerConnectionService connectionService,
        IEntityService EntityService,
        IPackageService packageService,
        IResourceService resourceService,
        IContextRetrievalService contextRetrievalService,
        IOptions<ServiceOwnerDelegationSettings> serviceOwnerDelegationSettings
    ) : ControllerBase
    {
        private Action<ConnectionOptions> ConfigureConnections { get; } = options =>
        {
            options.AllowedWriteFromEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
            options.AllowedWriteToEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
            options.AllowedReadFromEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
            options.AllowedReadToEntityTypes = [EntityTypeConstants.Organization, EntityTypeConstants.Person];
            options.FilterFromEntityTypes = [];
            options.FilterToEntityTypes = [];
        };

        [HttpPost("accesspackages")]
        [Authorize(Policy = AuthzConstants.SCOPE_SERVICEOWNER_PACKAGE_DELEGATION_WRITE)]
        [AuditServiceOwnerConsumer]
        [ProducesResponseType<AssignmentPackageDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddPackages([FromBody] ServiceOwnerAccessPackageDelegation packageDelegation, CancellationToken cancellationToken = default)
        {
            // Validate service owner is authorized to delegate this package
            string packageIdentifier = packageDelegation.PackageUrn.ValueSpan.ToString();
            if (!IsServiceOwnerAuthorizedForPackage(packageIdentifier, out _))
            {
                return Problems.PackageDelegationNotAuthorized.ToActionResult();
            }

            Guid? fromEntityId = null;
            Guid? toEntityId = null;
            Entity fromEntity = null;

            if (packageDelegation.From.IsPersonId(out PersonIdentifier personFrom))
            {
                fromEntity = await EntityService.GetByPersNo(personFrom.ToString(), cancellationToken);
                fromEntityId = fromEntity?.Id;
            }
            else if (packageDelegation.From.IsOrganizationId(out OrganizationNumber orgFrom))
            {
                fromEntity = await EntityService.GetByOrgNo(orgFrom.ToString(), cancellationToken);
                fromEntityId = fromEntity?.Id;
            }

            if (packageDelegation.To.IsPersonId(out PersonIdentifier personTo))
            {
                Entity personToEntity = await EntityService.GetByPersNo(personTo.ToString(), cancellationToken);
                toEntityId = personToEntity?.Id;
            }
            else if (packageDelegation.To.IsOrganizationId(out OrganizationNumber orgTo))
            {
                Entity orgToEntity = await EntityService.GetByOrgNo(orgTo.ToString(), cancellationToken);
                toEntityId = orgToEntity?.Id;
            }

            // Validate entities exist
            if (fromEntityId is null || toEntityId is null)
            {
                return Problems.ConnectionEntitiesDoNotExist.ToActionResult();
            }

            PackageDto package = await packageService.GetPackageByUrnValue(packageIdentifier, cancellationToken);

            if (package is null)
            {
                return Problems.PackageNotFound.ToActionResult();
            }

            // Validate that the package is of type for the given fromEntity
            // (e.g. org packages can only be delegated from org entities and person packages only from persons)
            if (package.Type.Id != fromEntity.TypeId)
            {
                return Problems.PackageNotAvailableForEntity.ToActionResult();
            }

            Result<AssignmentPackageDto> result = await connectionService.AddPackage(fromEntityId.Value, toEntityId.Value, package.Id, ConfigureConnections, cancellationToken);

            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(result.Value);
        }

        [HttpPost("accesspackages/revoke")]
        [Authorize(Policy = AuthzConstants.SCOPE_SERVICEOWNER_PACKAGE_DELEGATION_WRITE)]
        [AuditServiceOwnerConsumer]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RevokePackages([FromBody] ServiceOwnerAccessPackageDelegation packageDelegation, CancellationToken cancellationToken = default)
        {
            // Validate service owner is authorized to delegate this package
            string packageIdentifier = packageDelegation.PackageUrn.ValueSpan.ToString();
            if (!IsServiceOwnerAuthorizedForPackage(packageIdentifier, out OrganizationNumber? organizationNumber))
            {
                return Problems.PackageDelegationNotAuthorized.ToActionResult();
            }

            Guid? fromEntity = null;
            Guid? toEntity = null;

            if (packageDelegation.From.IsPersonId(out PersonIdentifier personFrom))
            {
                Entity personFromEntity = await EntityService.GetByPersNo(personFrom.ToString(), cancellationToken);
                fromEntity = personFromEntity?.Id;
            }
            else if (packageDelegation.From.IsOrganizationId(out OrganizationNumber orgFrom))
            {
                Entity orgFromEntity = await EntityService.GetByOrgNo(orgFrom.ToString(), cancellationToken);
                fromEntity = orgFromEntity?.Id;
            }

            if (packageDelegation.To.IsPersonId(out PersonIdentifier personTo))
            {
                Entity personToEntity = await EntityService.GetByPersNo(personTo.ToString(), cancellationToken);
                toEntity = personToEntity?.Id;
            }
            else if (packageDelegation.To.IsOrganizationId(out OrganizationNumber orgTo))
            {
                Entity orgToEntity = await EntityService.GetByOrgNo(orgTo.ToString(), cancellationToken);
                toEntity = orgToEntity?.Id;
            }

            // Validate entities exist
            if (fromEntity is null || toEntity is null)
            {
                return Problems.ConnectionEntitiesDoNotExist.ToActionResult();
            }

            PackageDto package = await packageService.GetPackageByUrnValue(packageIdentifier, cancellationToken);

            if (package is null)
            {
                return Problems.PackageNotFound.ToActionResult();
            }

            Entity authenticatedServiceOwnerEntity = await EntityService.GetByOrgNo(organizationNumber.ToString(), cancellationToken);
            if (authenticatedServiceOwnerEntity is null)
            {
                return Problems.PartyNotFound.ToActionResult();
            }

            Result<bool> result = await connectionService.RevokePackage(fromEntity.Value, toEntity.Value, package.Id, authenticatedServiceOwnerEntity.Id, cancellationToken);

            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return NoContent();
        }

        /// <summary>
        /// Gets the right keys available for delegation on a resource owned by the authenticated service owner.
        /// </summary>
        [HttpGet("resources/rights")]
        [Authorize(Policy = AuthzConstants.SCOPE_SERVICEOWNER_RESOURCE_DELEGATION_WRITE)]
        [FeatureGate(AccessMgmtFeatureFlags.EnableServiceOwnerResourceDelegation)]
        [ProducesResponseType<IEnumerable<RightDto>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetResourceRights([FromQuery(Name = "resource")] string resource, CancellationToken cancellationToken = default)
        {
            Resource resourceObj = await resourceService.GetResource(resource, cancellationToken);
            if (resourceObj is null)
            {
                return InvalidResourceProblem(resource);
            }

            if (IsMaskinportenSchema(resourceObj))
            {
                return MaskinportenSchemaProblem(resourceObj);
            }

            ServiceResource registryResource = await contextRetrievalService.GetResource(resourceObj.RefId, cancellationToken);
            if (registryResource is { Delegable: false })
            {
                return NotDelegableProblem(resourceObj);
            }

            if (!IsServiceOwnerAuthorizedForResource(resourceObj, out OrganizationNumber? organizationNumber))
            {
                return NotResourceOwnerProblem(resourceObj, organizationNumber);
            }

            Result<List<RightDto>> result = await connectionService.GetResourceRights(resourceObj.RefId, this.GetLanguageCode(), cancellationToken);

            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Delegates rights on a resource owned by the authenticated service owner from one party to another.
        /// </summary>
        [HttpPost("resources")]
        [Authorize(Policy = AuthzConstants.SCOPE_SERVICEOWNER_RESOURCE_DELEGATION_WRITE)]
        [FeatureGate(AccessMgmtFeatureFlags.EnableServiceOwnerResourceDelegation)]
        [AuditServiceOwnerConsumer]
        [ProducesResponseType<AssignmentResourceDto>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddResource([FromBody] ServiceOwnerResourceDelegation resourceDelegation, CancellationToken cancellationToken = default)
        {
            Resource resourceObj = await resourceService.GetResource(resourceDelegation.Resource, cancellationToken);
            if (resourceObj is null)
            {
                return InvalidResourceProblem(resourceDelegation.Resource);
            }

            if (IsMaskinportenSchema(resourceObj))
            {
                return MaskinportenSchemaProblem(resourceObj);
            }

            if (resourceObj is { Delegable: false })
            {
                return NotDelegableProblem(resourceObj);
            }

            if (!IsServiceOwnerAuthorizedForResource(resourceObj, out OrganizationNumber? organizationNumber))
            {
                return NotResourceOwnerProblem(resourceObj, organizationNumber);
            }

            Guid? fromEntityId = await ResolvePartyId(resourceDelegation.From, cancellationToken);
            Guid? toEntityId = await ResolvePartyId(resourceDelegation.To, cancellationToken);

            // Validate entities exist
            if (fromEntityId is null || toEntityId is null)
            {
                return Problems.ConnectionEntitiesDoNotExist.ToActionResult();
            }

            Entity authenticatedServiceOwnerEntity = await EntityService.GetByOrgNo(organizationNumber.ToString(), cancellationToken);
            if (authenticatedServiceOwnerEntity is null)
            {
                return Problems.PartyNotFound.ToActionResult();
            }

            Result<AssignmentResourceDto> result = await connectionService.AddResource(fromEntityId.Value, toEntityId.Value, resourceObj, resourceDelegation.RightKeys?.DirectRightKeys, authenticatedServiceOwnerEntity.Id, ConfigureConnections, cancellationToken);

            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return Ok(result.Value);
        }

        /// <summary>
        /// Revokes a resource delegation previously made by the authenticated service owner between two parties.
        /// </summary>
        [HttpPost("resources/revoke")]
        [Authorize(Policy = AuthzConstants.SCOPE_SERVICEOWNER_RESOURCE_DELEGATION_WRITE)]
        [FeatureGate(AccessMgmtFeatureFlags.EnableServiceOwnerResourceDelegation)]
        [AuditServiceOwnerConsumer]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<AltinnProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RevokeResource([FromBody] ServiceOwnerResourceDelegation resourceDelegation, CancellationToken cancellationToken = default)
        {
            Resource resourceObj = await resourceService.GetResource(resourceDelegation.Resource, cancellationToken);
            if (resourceObj is null)
            {
                return InvalidResourceProblem(resourceDelegation.Resource);
            }

            if (IsMaskinportenSchema(resourceObj))
            {
                return MaskinportenSchemaProblem(resourceObj);
            }

            if (!IsServiceOwnerAuthorizedForResource(resourceObj, out OrganizationNumber? organizationNumber))
            {
                return NotResourceOwnerProblem(resourceObj, organizationNumber);
            }

            Guid? fromEntityId = await ResolvePartyId(resourceDelegation.From, cancellationToken);
            Guid? toEntityId = await ResolvePartyId(resourceDelegation.To, cancellationToken);

            // Validate entities exist
            if (fromEntityId is null || toEntityId is null)
            {
                return Problems.ConnectionEntitiesDoNotExist.ToActionResult();
            }

            Entity authenticatedServiceOwnerEntity = await EntityService.GetByOrgNo(organizationNumber.ToString(), cancellationToken);
            if (authenticatedServiceOwnerEntity is null)
            {
                return Problems.PartyNotFound.ToActionResult();
            }

            Result<bool> result = await connectionService.RevokeResource(fromEntityId.Value, toEntityId.Value, resourceObj.Id, authenticatedServiceOwnerEntity.Id, cancellationToken);

            if (result.IsProblem)
            {
                return result.Problem.ToActionResult();
            }

            return NoContent();
        }

        /// <summary>
        /// Resolves a party given as a person identifier or an organization identifier to the entity id.
        /// Returns null if the party is given in another form or does not exist.
        /// </summary>
        private async Task<Guid?> ResolvePartyId(ServiceOwnerConnectionPartyUrn party, CancellationToken cancellationToken)
        {
            if (party.IsPersonId(out PersonIdentifier personIdentifier))
            {
                Entity personEntity = await EntityService.GetByPersNo(personIdentifier.ToString(), cancellationToken);
                return personEntity?.Id;
            }

            if (party.IsOrganizationId(out OrganizationNumber organizationNumber))
            {
                Entity orgEntity = await EntityService.GetByOrgNo(organizationNumber.ToString(), cancellationToken);
                return orgEntity?.Id;
            }

            return null;
        }

        /// <summary>
        /// MaskinportenSchema resources are delegated through the dedicated Maskinporten delegation API,
        /// not as rightholder resources through these endpoints.
        /// </summary>
        private static bool IsMaskinportenSchema(Resource resource)
        {
            return string.Equals(resource.Type?.Name, "MaskinportenSchema", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The resource type rules the resource out whoever asks, so it is checked before ownership.
        /// </summary>
        private static IActionResult MaskinportenSchemaProblem(Resource resource)
        {
            ProblemDetails problem = Problems.ResourceNotDelegable.ToProblemDetails();
            problem.Detail = $"Resource '{resource.RefId}' is of type MaskinportenSchema, which cannot be delegated through this API";
            return problem.ToActionResult();
        }

        /// <summary>
        /// The Resource Registry's Delegable flag rules the resource out whoever asks, so it is checked before ownership.
        /// </summary>
        private static IActionResult NotDelegableProblem(Resource resource)
        {
            ProblemDetails problem = Problems.ResourceNotDelegable.ToProblemDetails();
            problem.Detail = $"Resource '{resource.RefId}' is marked as not delegable in the Resource Registry";
            return problem.ToActionResult();
        }

        /// <summary>
        /// Names the resource owner and the organization the token is for, so the caller can tell a wrong token from
        /// a wrong resource. Neither is news to the caller: the owner is public in the Resource Registry.
        /// </summary>
        private IActionResult NotResourceOwnerProblem(Resource resource, OrganizationNumber? organizationNumber)
        {
            string owner = DescribeOrganization(resource.Provider?.Code, resource.Provider?.RefId);
            string caller = organizationNumber is null
                ? "the token does not identify an organization"
                : $"the token is for {DescribeOrganization(User.FindFirst(AltinnCoreClaimTypes.Org)?.Value, organizationNumber.ToString())}";

            ProblemDetails problem = Problems.ResourceDelegationNotAuthorized.ToProblemDetails();
            problem.Detail = $"Resource '{resource.RefId}' is owned by {owner}, but {caller}";
            return problem.ToActionResult();
        }

        private static string DescribeOrganization(string code, string organizationNumber)
        {
            if (string.IsNullOrWhiteSpace(organizationNumber))
            {
                return string.IsNullOrWhiteSpace(code) ? "an unknown organization" : code;
            }

            return string.IsNullOrWhiteSpace(code) ? organizationNumber : $"{code} ({organizationNumber})";
        }

        private static IActionResult InvalidResourceProblem(string resource)
        {
            ProblemDetails problem = Problems.InvalidResource.ToProblemDetails();
            problem.Extensions["resource"] = resource;
            return problem.ToActionResult();
        }

        /// <summary>
        /// The service owner is authorized for a resource when the organization number from the consumer claim
        /// equals the organization number of the resource provider. A provider without an organization number,
        /// like TTD in the test environments, is matched on the org claim of the token against the provider code
        /// instead, the same way the request endpoint does it.
        /// </summary>
        private bool IsServiceOwnerAuthorizedForResource(Resource resource, out OrganizationNumber? organizationNumber)
        {
            var consumerParty = OrgUtil.GetAuthenticatedParty(User);
            if (consumerParty is null || !consumerParty.IsOrganizationId(out organizationNumber))
            {
                organizationNumber = null;
                return false;
            }

            string providerOrgNo = resource.Provider?.RefId;
            if (!string.IsNullOrWhiteSpace(providerOrgNo))
            {
                return string.Equals(providerOrgNo, organizationNumber.ToString(), StringComparison.Ordinal);
            }

            string providerCode = resource.Provider?.Code;
            string orgClaim = User.FindFirst(AltinnCoreClaimTypes.Org)?.Value;
            return !string.IsNullOrWhiteSpace(providerCode) && string.Equals(providerCode, orgClaim, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsServiceOwnerAuthorizedForPackage(string packageIdentifier, out OrganizationNumber? organizationNumber)
        {
            var consumerParty = OrgUtil.GetAuthenticatedParty(User);
            if (consumerParty is null || !consumerParty.IsOrganizationId(out organizationNumber))
            {
                organizationNumber = null;
                return false;
            }

            var whiteList = serviceOwnerDelegationSettings.Value.PackageWhiteList;
            if (whiteList.TryGetValue(organizationNumber.ToString(), out var allowedPackages))
            {
                return allowedPackages.Contains(packageIdentifier, StringComparer.OrdinalIgnoreCase);
            }

            return false;
        }
    }
}
