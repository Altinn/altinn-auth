using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Models;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Altinn.Authorization.Api.Contracts.Authorization;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Controllers;

/// <summary>
/// Controller responsible for all operations for managing delegations of Altinn Apps
/// </summary>
[Route("accessmanagement/api/v1/policyinformation")]
[ApiController]
public class PolicyInformationPointController(
    IMapper mapper,
    IPolicyInformationPoint pip,
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
    /// <param name="from">The uuid of the party to lookup if the to-party has access packages for</param>
    /// <param name="to">The uuid of the party to lookup access packages on behalf of the from-party</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>A list of all access package urns to-party has access to on behalf of the from-party</returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet]
    [Route("accesspackages")]
    public async Task<ActionResult> GetAccessPackages([FromQuery] Guid from, [FromQuery] Guid to, CancellationToken cancellationToken)
    {
        List<AccessPackageUrn> packages = new();

        var filter = new AuthorizedPartiesFilters { IncludeAccessPackages = true, IncludePartiesViaKeyRoles = AuthorizedPartiesIncludeFilter.True, PartyFilter = new SortedDictionary<Guid, Guid> { { from, from } } };
        var connectionPackages = await authorizedPartyRepoService.GetPipConnectionsFromOthers(to, filters: filter, ct: cancellationToken);
        if (connectionPackages != null)
        {
            packages.AddRange(connectionPackages.SelectMany(conPackage => conPackage.Packages.Select(pkg => AccessPackageUrn.Parse(pkg.Urn))));
        }

        return Ok(packages);
    }

    /// <summary>
    /// Endpoint to lookup all roles and access packages a given to-party uuid has for a given from-party uuid
    /// </summary>
    /// <param name="from">The uuid of the party to lookup if the to-party has access packages for</param>
    /// <param name="to">The uuid of the party to lookup access packages on behalf of the from-party</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>Lists of all roles and access package urns to-party has access to on behalf of the from-party</returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet]
    [Route("roles-and-accesspackages")]
    public async Task<ActionResult> GetRolesAndAccessPackages([FromQuery] Guid from, [FromQuery] Guid to, CancellationToken cancellationToken)
    {
        PipResponseDto pipResponse = new();

        var filter = new AuthorizedPartiesFilters { IncludeAccessPackages = true, IncludePartiesViaKeyRoles = AuthorizedPartiesIncludeFilter.True, PartyFilter = new SortedDictionary<Guid, Guid> { { from, from } } };
        var connections = await authorizedPartyRepoService.GetPipConnectionsFromOthers(to, filters: filter, ct: cancellationToken);
        if (connections != null)
        {
            pipResponse.AccessPackages = connections.SelectMany(conPackage => conPackage.Packages.Select(pkg => AccessPackageUrn.Parse(pkg.Urn))).Distinct().ToList();

            foreach (var conRole in connections.Where(c => c.AssignmentId.HasValue))
            {
                if (RoleConstants.TryGetById(conRole.RoleId, out var role) && role.Id != RoleConstants.Rightholder.Id && role.Id != RoleConstants.Agent.Id)
                {
                    pipResponse.Roles.Add(RoleUrn.Parse(role.Entity.Urn));
                    
                    if (!string.IsNullOrWhiteSpace(role.Entity.LegacyUrn))
                    {
                        pipResponse.Roles.Add(RoleUrn.Parse(role.Entity.LegacyUrn));
                    }
                }
            }

            pipResponse.Roles = pipResponse.Roles.Distinct().ToList();
        }

        return Ok(pipResponse);
    }
}
