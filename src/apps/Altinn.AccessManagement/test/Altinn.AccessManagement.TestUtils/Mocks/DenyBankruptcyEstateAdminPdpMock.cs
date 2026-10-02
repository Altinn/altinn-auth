using System.Security.Claims;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Common.PEP.Interfaces;

namespace Altinn.AccessManagement.TestUtils.Mocks;

/// <summary>
/// Test double for <see cref="IPDP"/> that returns "Deny" only when the request is checking the
/// <c>altinn_bankruptcy_estate_admin</c> resource, and "Permit" for everything else.
/// </summary>
/// <remarks>
/// The bankruptcy endpoints are guarded by
/// <c>EndUserResourceAccessRequirement("read"/"write", "altinn_bankruptcy_estate_admin")</c>. The
/// default <see cref="PermitPdpMock"/> permits every request, so that requirement is never actually
/// exercised. Registering this mock instead lets a test assert that a party the PDP does not
/// authorize is rejected with 403 Forbidden.
/// </remarks>
public class DenyBankruptcyEstateAdminPdpMock : IPDP
{
    private const string BankruptcyEstateAdminResource = "altinn_bankruptcy_estate_admin";
    private const string ResourceIdAttribute = "urn:altinn:resource";

    private static bool IsBankruptcyEstateAdminCheck(XacmlJsonRequestRoot xacmlJsonRequest)
    {
        return xacmlJsonRequest?.Request?.Resource?.Any(
            category => category.Attribute?.Any(
                attr => attr.AttributeId == ResourceIdAttribute
                     && attr.Value == BankruptcyEstateAdminResource) == true) == true;
    }

    /// <inheritdoc/>
    public Task<XacmlJsonResponse> GetDecisionForRequest(XacmlJsonRequestRoot xacmlJsonRequest, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var decision = IsBankruptcyEstateAdminCheck(xacmlJsonRequest) ? "Deny" : "Permit";

        var response = new XacmlJsonResponse
        {
            Response = [new XacmlJsonResult { Decision = decision }]
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
        return Task.FromResult(!IsBankruptcyEstateAdminCheck(xacmlJsonRequest));
    }

    /// <inheritdoc/>
    public Task<bool> GetDecisionForUnvalidateRequest(XacmlJsonRequestRoot xacmlJsonRequest, ClaimsPrincipal user)
        => GetDecisionForUnvalidateRequest(xacmlJsonRequest, user, CancellationToken.None);
}
