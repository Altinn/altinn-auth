using System.Net.Mime;
using Altinn.AccessManagement.Core.Configuration;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Audit;
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
