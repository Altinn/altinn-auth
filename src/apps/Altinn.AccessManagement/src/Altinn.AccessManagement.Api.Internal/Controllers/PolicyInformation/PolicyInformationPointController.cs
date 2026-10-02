using Altinn.AccessManagement.Api.Internal.Extensions;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Altinn.Authorization.Api.Contracts.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Internal.Controllers.PolicyInformation;

/// <summary>
/// Controller responsible for all operations for managing delegations of Altinn Apps
/// </summary>
[Route("accessmanagement/api/v1/policyinformation")]
[ApiController]
public class PolicyInformationPointController(
    IPolicyInformationPoint pip,
    IAuthorizedPartyRepoServiceEf authorizedPartyRepoService,
    IEntityService entityService
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
    public async Task<ActionResult<List<DelegationChangeDto>>> GetAllDelegationChanges([FromBody] DelegationChangeInputDto request, CancellationToken cancellationToken)
    {
        DelegationChangeList response = await pip.GetAllDelegations(request.ToDelegationChangeInput(), includeInstanceDelegations: true, cancellationToken);

        if (!response.IsValid)
        {
            foreach (string errorKey in response.Errors.Keys)
            {
                ModelState.AddModelError(errorKey, response.Errors[errorKey]);
            }

            return new ObjectResult(ProblemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
        }

        return response.DelegationChanges.Select(x => x.ToDelegationChangeDto()).ToList();
    }

    /// <summary>
    /// Endpoint to lookup all access packages a given to-party uuid has for a given from-party uuid
    /// </summary>
    /// <param name="from">The uuid of the party to lookup if the to-party has access packages for</param>
    /// <param name="to">The uuid of the party to lookup access packages on behalf of the from-party</param>
    /// <param name="authContext">The authorization context limiting which kinds of access are considered</param>
    /// <param name="viaParty">The organization number of the via-party (required for <see cref="AuthContext.ClientAccess"/>)</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>A list of all access package urns to-party has access to on behalf of the from-party</returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet]
    [Route("accesspackages")]
    public async Task<ActionResult> GetAccessPackages([FromQuery] Guid from, [FromQuery] Guid to, [FromQuery] AuthContext authContext = AuthContext.All, [FromQuery] string viaParty = null, CancellationToken cancellationToken = default)
    {
        List<AccessPackageUrn> packages = new();

        var connectionPackages = await GetConnections(from, to, authContext, viaParty, cancellationToken);
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
    /// <param name="authContext">The authorization context limiting which kinds of access are considered</param>
    /// <param name="viaParty">The organization number of the via-party (required for <see cref="AuthContext.ClientAccess"/>)</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>Lists of all roles and access package urns to-party has access to on behalf of the from-party</returns>
    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet]
    [Route("roles-and-accesspackages")]
    public async Task<ActionResult> GetRolesAndAccessPackages([FromQuery] Guid from, [FromQuery] Guid to, [FromQuery] AuthContext authContext = AuthContext.All, [FromQuery] string viaParty = null, CancellationToken cancellationToken = default)
    {
        PipResponseDto pipResponse = new();

        var connections = await GetConnections(from, to, authContext, viaParty, cancellationToken);
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

    /// <summary>
    /// Gets the connections from the from-party to the to-party, limited by the authorization context:
    /// - All: every connection (direct, keyrole, delegation and hierarchy).
    /// - DirectAccess: excludes keyrole (org-to-org) inheritance and client delegations.
    /// - ClientAccess: only client delegations received through the given via-party organization.
    /// </summary>
    private async Task<List<ConnectionQueryExtendedRecord>> GetConnections(Guid from, Guid to, AuthContext authContext, string viaParty, CancellationToken cancellationToken)
    {
        Guid? viaPartyId = null;
        if (authContext == AuthContext.ClientAccess)
        {
            if (string.IsNullOrWhiteSpace(viaParty))
            {
                return [];
            }

            var viaPartyEntity = await entityService.GetByOrgNo(viaParty, cancellationToken);
            if (viaPartyEntity == null)
            {
                return [];
            }

            viaPartyId = viaPartyEntity.Id;
        }

        var filter = new AuthorizedPartiesFilters
        {
            IncludeAccessPackages = true,
            IncludePartiesViaKeyRoles = authContext == AuthContext.All ? AuthorizedPartiesIncludeFilter.True : AuthorizedPartiesIncludeFilter.False,
            IncludeClientDelegations = authContext != AuthContext.DirectAccess,
            PartyFilter = new SortedDictionary<Guid, Guid> { { from, from } }
        };

        var connections = await authorizedPartyRepoService.GetPipConnectionsFromOthers(to, filters: filter, ct: cancellationToken);
        if (connections == null || authContext != AuthContext.ClientAccess)
        {
            return connections;
        }

        return connections
            .Where(c => c.Reason == ConnectionReason.Delegation && c.DelegationId.HasValue && c.ViaId == viaPartyId)
            .ToList();
    }
}
