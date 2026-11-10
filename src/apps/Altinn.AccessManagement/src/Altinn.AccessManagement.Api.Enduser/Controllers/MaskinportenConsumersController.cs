using Altinn.AccessManagement.Api.Enduser.Controllers.Base;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Api.Enduser.Controllers;

/// <summary>
/// Controller for managing Maskinporten consumer connections (from supplier perspective)
/// </summary>
[Route("accessmanagement/api/v1/enduser/maskinportenconsumers")]
public class MaskinportenConsumersController(
    IMaskinportenSupplierService maskinportenSupplierService
    ) : MaskinportenConsumersControllerBase(maskinportenSupplierService)
{
}
