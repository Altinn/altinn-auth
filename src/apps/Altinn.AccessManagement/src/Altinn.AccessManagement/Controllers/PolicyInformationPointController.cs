using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.Authorization.Models;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Controllers;

/// <summary>
/// Controller responsible for all operations for managing delegations of Altinn Apps
/// </summary>
[Route("accessmanagement/api/v1/policyinformation")]
[ApiController]
public class PolicyInformationPointController : ControllerBase
{
    private readonly IPolicyInformationPoint _pip;
    private readonly IMapper _mapper;
    private readonly IConnectionRepository _connectionRepository;
    private readonly IConnectionPackageRepository _connectionPackageRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyInformationPointController"/> class.
    /// </summary>
    /// <param name="pip">The policy information point</param>
    /// <param name="mapper">The mapper</param>
    /// <param name="connectionRepository">Connection repo</param>
    /// <param name="connectionPackageRepository">ConnectionPackage repo</param>
    public PolicyInformationPointController(IPolicyInformationPoint pip, IMapper mapper, IConnectionRepository connectionRepository, IConnectionPackageRepository connectionPackageRepository)
    {
        _pip = pip;
        _mapper = mapper;
        _connectionRepository = connectionRepository;
        _connectionPackageRepository = connectionPackageRepository;
    }

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
        DelegationChangeList response = await _pip.GetAllDelegations(request, includeInstanceDelegations: true, cancellationToken);

        if (!response.IsValid)
        {
            foreach (string errorKey in response.Errors.Keys)
            {
                ModelState.AddModelError(errorKey, response.Errors[errorKey]);
            }

            return new ObjectResult(ProblemDetailsFactory.CreateValidationProblemDetails(HttpContext, ModelState));
        }

        return _mapper.Map<List<DelegationChangeExternal>>(response.DelegationChanges);
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
        List<AccessPackageUrn> result = [];
        var connectionFilter = _connectionRepository.CreateFilterBuilder()
            .Equal(t => t.FromId, from)
            .Equal(t => t.ToId, to)
            .NotSet(t => t.FacilitatorId)
            .NotSet(t => t.Id);

        var connections = await _connectionRepository.Get(connectionFilter, cancellationToken: cancellationToken);
        if (connections != null && connections.Any())
        {
            var connectionPackageFilter = _connectionPackageRepository.CreateFilterBuilder()
                .In(t => t.Id, connections.Select(t => t.Id));

            var packages = await _connectionPackageRepository.GetExtended(connectionPackageFilter, cancellationToken: cancellationToken);
            result.AddRange(packages.Select(conPackage => AccessPackageUrn.AccessPackageId.Create(AccessPackageIdentifier.CreateUnchecked(conPackage.Package.Urn.Split(':').Last()))));
        }

        return Ok(result);
    }
}
