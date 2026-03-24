using System.Net;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Models;

namespace Altinn.AccessManagement.TestUtils.Mocks;

/// <summary>
/// Mock implementation of <see cref="IAltinn2RightsClient"/> that returns
/// successful no-op responses for all operations.
/// </summary>
public class Altinn2RightsClientMock : IAltinn2RightsClient
{
    /// <inheritdoc/>
    public Task<HttpResponseMessage> ClearReporteeRights(int fromPartyId, int toPartyId, int toUserId = 0, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    /// <inheritdoc/>
    public Task<DelegationCheckResponse> PostDelegationCheck(int authenticatedUserId, int reporteePartyId, string serviceCode, string serviceEditionCode)
    {
        return Task.FromResult(new DelegationCheckResponse());
    }

    /// <inheritdoc/>
    public Task<DelegationActionResult> PostDelegation(int authenticatedUserId, int reporteePartyId, SblRightDelegationRequest delegationRequest)
    {
        return Task.FromResult(new DelegationActionResult());
    }
}
