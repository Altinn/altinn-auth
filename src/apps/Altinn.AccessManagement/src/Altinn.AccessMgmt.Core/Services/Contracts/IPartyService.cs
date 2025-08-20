using Altinn.AccessMgmt.Core.Models;
using Altinn.Authorization.Api.Contracts.Party; //AddPartyResultDto.cs
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

/// <summary>
/// Interface for party management services
/// </summary>
public interface IPartyService
{
    /// <summary>
    /// Adds a party to the system if it does not already exist
    /// </summary>
    /// <param name="party">The party to add</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns>
    /// A <see cref="Result{AddPartyResultDto}"/> indicating the outcome of the operation.
    /// </returns>
    Task<Result<AddPartyResultDto>> AddParty(PartyBaseInternal party, CancellationToken cancellationToken = default);
}
