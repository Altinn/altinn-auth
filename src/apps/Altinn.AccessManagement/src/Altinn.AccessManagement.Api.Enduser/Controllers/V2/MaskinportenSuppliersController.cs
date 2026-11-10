using Altinn.AccessManagement.Api.Enduser.Controllers.Base;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers.V2;

/// <summary>
/// Version 2 of <see cref="Controllers.MaskinportenSuppliersController"/>. Only the route differs.
/// 
/// NOTE! New functionality should be added here, and not in the base controller, 
/// as it would result in it also being available in v1, which is not desired.
/// </summary>
[ApiVersion(2.0)]
[Route("accessmanagement/api/v{version:apiVersion}/enduser/maskinporten/suppliers")]
public class MaskinportenSuppliersController(
    IMaskinportenSupplierService maskinportenSupplierService
    ) : MaskinportenSuppliersControllerBase(maskinportenSupplierService)
{
}
