using System.Security.Claims;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Common.PEP.Interfaces;

namespace Altinn.AccessMgmt.TestUtils.Mocks;

/// <summary>
/// Test double for <see cref="IPDP"/> that always returns a "Permit"
/// decision. Use this mock in tests where policy checks should allow access
/// and where PDP logic is out of scope for the scenario under test.
/// </summary>
/// <remarks>
/// - All <c>GetDecision*</c> overloads return an unconditional permit.
/// - This mock does not perform any validation of the request or the
///   <see cref="System.Security.Claims.ClaimsPrincipal"/> and should not be
///   used in tests that verify policy evaluation behavior.
/// </remarks>
public class PermitPdpMock : IPDP
{
    /// <inheritdoc/>
    public Task<XacmlJsonResponse> GetDecisionForRequest(XacmlJsonRequestRoot xacmlJsonRequest, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var response = new XacmlJsonResponse
        {
            Response = [new XacmlJsonResult { Decision = "Permit" }]
        };

        return Task.FromResult(response);
    }

    /// <inheritdoc/>
    public Task<XacmlJsonResponse> GetDecisionForRequest(XacmlJsonRequestRoot xacmlJsonRequest)
        => GetDecisionForRequest(xacmlJsonRequest, CancellationToken.None);

    /// <inheritdoc/>
    public Task<bool> GetDecisionForUnvalidateRequest(XacmlJsonRequestRoot xacmlJsonRequest, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public Task<bool> GetDecisionForUnvalidateRequest(XacmlJsonRequestRoot xacmlJsonRequest, ClaimsPrincipal user)
        => GetDecisionForUnvalidateRequest(xacmlJsonRequest, user, CancellationToken.None);
}
