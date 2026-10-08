using Altinn.AccessManagement.Api.Enduser.Controllers.Base;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers.V2;

/// <summary>
/// Activity log for the maskinporten area: Maskinporten schema delegation events (the
/// Supplier-role slice of the log, hidden from every other area). Uses the same scopes as
/// the other maskinporten endpoints, per anchor direction: the supplier read scope for
/// from=party, the consumer read scope for to=party.
/// </summary>
[ApiController]
[ApiVersion(2.0)]
[Route("accessmanagement/api/v{version:apiVersion}/enduser/maskinporten/activitylog")]
[FeatureGate(AccessMgmtFeatureFlags.EnableEnduserMaskinportenActivityLogApi)]
[Authorize(Policy = AuthzConstants.POLICY_ENDUSER_MASKINPORTEN_BIDIRECTIONAL_READ)]
[Authorize(Policy = AuthzConstants.POLICY_MASKINPORTEN_DELEGATION_ENDUSER_READ)]
public class MaskinportenActivityLogController(IActivityLogService activityLogService)
    : ActivityLogAreaControllerBase(activityLogService, ActivityLogAreas.Maskinporten)
{
}
