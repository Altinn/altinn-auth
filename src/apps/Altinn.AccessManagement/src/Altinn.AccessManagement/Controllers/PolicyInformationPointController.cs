using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Models;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace Altinn.AccessManagement.Controllers;

/// <summary>
/// Controller responsible for all operations for managing delegations of Altinn Apps
/// </summary>
[Route("accessmanagement/api/v1/policyinformation")]
[ApiController]
public class PolicyInformationPointController(
    IFeatureManager featureManager,
    IMapper mapper,
    IPolicyInformationPoint pip,
    IConnectionService connectionService,
    IAuthorizedPartyRepoServiceEf authorizedPartyRepoService
    ) : ControllerBase
{
    /// <summary>
    /// Endpoint to find all delegation changes for a given user, reportee and app/resource context
    /// </summary>
    /// <param name="request">The input model that contains id info about user, reportee, resource and resourceMatchType </param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>A list of delegation changes that's stored in the database </returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost]
    [Route("getdelegationchanges")]
    public async Task<ActionResult<List<DelegationChangeExternal>>> GetAllDelegationChanges([FromBody] DelegationChangeInput request, CancellationToken cancellationToken)
    {
        DelegationChangeList response = await pip.GetAllDelegations(request, includeInstanceDelegations: true, cancellationToken);

        if (!response.IsValid)
        {
            foreach (string errorKey in response.Errors.Keys)
            {
                ModelState.AddModelError(errorKey, response.Errors[errorKey]);
            }

            return new ObjectResult(ProblemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
        }

        return mapper.Map<List<DelegationChangeExternal>>(response.DelegationChanges);
    }

    /// <summary>
    /// Endpoint to lookup all access packages a given to-party uuid has for a given from-party uuid
    /// </summary>
    /// <param name="from">The uuid of the party to lookop if the to-party has access packages for</param>
    /// <param name="to">The uuid of the party to lookup access packages og behalf of the from-party</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>A list of all access package urns to-party has access to on behalf of the from-party</returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet]
    [Route("accesspackages")]
    public async Task<ActionResult> GetAccessPackages([FromQuery] Guid from, [FromQuery] Guid to, CancellationToken cancellationToken)
    {
        List<AccessPackageUrn> packages = new();

        IEnumerable<PackagePermissionDto> connectionPackages = null;
        if (await featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.AuthorizedPartiesEfEnabled))
        {
            connectionPackages = await authorizedPartyRepoService.GetPackagesFromOthers(to, from, cancellationToken);
        }
        else
        {
            connectionPackages = await connectionService.GetPackagePermissionsFromOthers(partyId: to, fromId: from, cancellationToken: cancellationToken);
        }

        if (connectionPackages != null)
        {
            packages.AddRange(connectionPackages.Select(conPackage => AccessPackageUrn.AccessPackageId.Create(AccessPackageIdentifier.CreateUnchecked(conPackage.Package.Urn.Split(':').Last()))));
        }

        return Ok(packages);
    }
}
