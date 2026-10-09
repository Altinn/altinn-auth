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
/// Activity log for the connections area: assignment and delegation events involving the
/// party (Maskinporten schema events live under enduser/maskinporten/activitylog).
/// Authorization follows the connections endpoints' model — the directional scopes and the
/// person access-manager rule — keyed on the required direction parameter.
/// </summary>
[ApiController]
[ApiVersion(2.0)]
[Route("accessmanagement/api/v{version:apiVersion}/enduser/connections/activitylog")]
[FeatureGate(AccessMgmtFeatureFlags.EnableEnduserConnectionsActivityLogApi)]
[Authorize(Policy = AuthzConstants.POLICY_ENDUSER_CONNECTIONS_ACTIVITYLOG_READ)]
[Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
public class ConnectionsActivityLogController(IActivityLogService activityLogService)
    : ActivityLogAreaControllerBase(activityLogService, ActivityLogAreas.Connections)
{
}
