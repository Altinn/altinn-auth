using Altinn.AccessManagement.Api.Enduser.Controllers.Base;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers;

/// <summary>
/// Controller for managing Maskinporten supplier assignments and scope delegations
/// </summary>
[Route("accessmanagement/api/v1/enduser/maskinportensuppliers")]
public class MaskinportenSuppliersController(
    IMaskinportenSupplierService maskinportenSupplierService
    ) : MaskinportenSuppliersControllerBase(maskinportenSupplierService)
{
}
