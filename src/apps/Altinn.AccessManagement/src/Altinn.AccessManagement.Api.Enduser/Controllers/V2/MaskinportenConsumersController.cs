using Altinn.AccessMgmt.Core.Services.Contracts;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers.V2;

/// <summary>
/// Version 2 of <see cref="Controllers.MaskinportenConsumersController"/>. Only the route differs.
/// </summary>
[ApiVersion(2.0)]
[Route("accessmanagement/api/v{version:apiVersion}/enduser/maskinporten/consumers")]
public class MaskinportenConsumersController(
    IMaskinportenSupplierService maskinportenSupplierService
    ) : Controllers.MaskinportenConsumersController(maskinportenSupplierService)
{
}
