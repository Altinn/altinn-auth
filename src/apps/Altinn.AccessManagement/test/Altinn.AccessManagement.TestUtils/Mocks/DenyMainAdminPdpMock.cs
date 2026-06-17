using System.Security.Claims;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Common.PEP.Interfaces;

namespace Altinn.AccessManagement.TestUtils.Mocks;

/// <summary>
/// Test double for <see cref="IPDP"/> that returns "Deny" only when the
/// request is checking the <c>altinn_access_management_hovedadmin</c> resource,
/// and returns "Permit" for all other requests.
/// </summary>
/// <remarks>
/// Use this mock in tests that verify the self-delegation rejection path inside
/// <c>RequestController.ApproveRequest</c>. The controller calls PDP twice:
/// <list type="bullet">
///   <item>
///     <description>
///       <c>EndUserResourceAccessHandler</c> checks <c>altinn_access_management</c>
///       — this mock returns Permit so the HTTP authorization layer succeeds and
///       the controller action runs.
///     </description>
///   </item>
///   <item>
///     <description>
///       <c>AuthorizeResourceAccess("altinn_access_management_hovedadmin", …)</c>
///       — this mock returns Deny, so <c>isMainAdmin</c> is false and the
///       self-delegation gate adds <c>RequestFromSelfNotAllowed</c> (AM.VLD-00045).
///     </description>
///   </item>
/// </list>
/// </remarks>
public class DenyMainAdminPdpMock : IPDP
{
    private const string MainAdminResource = "altinn_access_management_hovedadmin";
    private const string ResourceIdAttribute = "urn:altinn:resource";

    private static bool IsMainAdminCheck(XacmlJsonRequestRoot xacmlJsonRequest)
    {
        return xacmlJsonRequest?.Request?.Resource?.Any(
            category => category.Attribute?.Any(
                attr => attr.AttributeId == ResourceIdAttribute
                     && attr.Value == MainAdminResource) == true) == true;
    }

    /// <inheritdoc/>
    public Task<XacmlJsonResponse> GetDecisionForRequest(XacmlJsonRequestRoot xacmlJsonRequest, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var decision = IsMainAdminCheck(xacmlJsonRequest) ? "Deny" : "Permit";

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
        return Task.FromResult(!IsMainAdminCheck(xacmlJsonRequest));
    }

    /// <inheritdoc/>
    public Task<bool> GetDecisionForUnvalidateRequest(XacmlJsonRequestRoot xacmlJsonRequest, ClaimsPrincipal user)
        => GetDecisionForUnvalidateRequest(xacmlJsonRequest, user, CancellationToken.None);
}
