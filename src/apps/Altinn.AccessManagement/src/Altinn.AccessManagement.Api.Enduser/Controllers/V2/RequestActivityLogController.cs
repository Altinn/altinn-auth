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
/// Activity log for the request area: access request events involving the party, including
/// their package/resource children and status changes.
/// </summary>
[ApiController]
[ApiVersion(2.0)]
[Route("accessmanagement/api/v{version:apiVersion}/enduser/request/activitylog")]
[FeatureGate(AccessMgmtFeatureFlags.EnableEnduserRequestActivityLogApi)]
[Authorize(Policy = AuthzConstants.POLICY_ENDUSER_REQUESTS_READ)]
[Authorize(Policy = AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ)]
public class RequestActivityLogController(IActivityLogService activityLogService)
    : ActivityLogAreaControllerBase(activityLogService, ActivityLogAreas.Request)
{
}
