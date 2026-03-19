using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Services.Contracts;

namespace Altinn.AccessMgmt.Core.Services;

/// <summary>
/// Adapter that implements <see cref="IConsentDelegationCheckService"/> using the new 
/// <see cref="IConnectionService.ResourceDelegationCheck"/> method which supports access packages.
/// </summary>
public class ConsentDelegationCheckService(IConnectionService connectionService) : IConsentDelegationCheckService
{
    /// <inheritdoc />
    public async Task<ConsentDelegationCheckResult> CheckDelegatableRights(Guid authenticatedUserUuid, Guid partyUuid, string resourceIdentifier, CancellationToken cancellationToken = default)
    {
        var result = await connectionService.ResourceDelegationCheck(
            authenticatedUserUuid: authenticatedUserUuid,
            party: partyUuid,
            resource: resourceIdentifier,
            ignoreDelegableFlag: true,
            cancellationToken: cancellationToken);

        if (result.IsProblem)
        {
            return new ConsentDelegationCheckResult { IsSuccess = false };
        }

        var delegatableActions = result.Value.Rights
            .Where(r => r.Result && r.Right.Action is not null)
            .Select(r => r.Right.Action.Urn())
            .ToList();

        return new ConsentDelegationCheckResult
        {
            IsSuccess = true,
            DelegatableActions = delegatableActions
        };
    }
}
