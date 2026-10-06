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
/// </summary>
[ApiController]
[ApiVersion(2.0)]
[Route("accessmanagement/api/v{version:apiVersion}/enduser/connections/activitylog")]
[FeatureGate(AccessMgmtFeatureFlags.EnableEnduserConnectionsActivityLogApi)]
[Authorize(Policy = AuthzConstants.SCOPE_PORTAL_ENDUSER)]
[Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
public class ConnectionsActivityLogController(IActivityLogService activityLogService)
    : ActivityLogAreaControllerBase(activityLogService, ActivityLogAreas.Connections)
{
}
