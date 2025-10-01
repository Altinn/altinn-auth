using Altinn.AccessMgmt.PersistenceEF.Queries.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Utils;

/// <summary>
/// Utility for mapping of entity models to DTOs, related to delegation checks.
/// </summary>
public partial class DtoMapper
{
    public IEnumerable<AccessPackageDto.Check> Convert(IEnumerable<PackageDelegationCheck> packageDelegationCheck)
    {
        return packageDelegationCheck.GroupBy(p => p.Package.Id).Select(group =>
        {
            var firstPackage = group.First();
            return new AccessPackageDto.Check
            {
                Package = new AccessPackageDto
                {
                    Id = firstPackage.Package.Id,
                    Urn = firstPackage.Package.Urn,
                    AreaId = firstPackage.Package.AreaId
                },
                Result = group.Any(p => p.Result),
                Reasons = group.Select(p => new AccessPackageDto.Check.Reason
                {
                    Description = p.Reason.Description,
                    RoleId = p.Reason.RoleId,
                    RoleUrn = p.Reason.RoleUrn,
                    FromId = p.Reason.FromId,
                    FromName = p.Reason.FromName,
                    ToId = p.Reason.ToId,
                    ToName = p.Reason.ToName,
                    ViaId = p.Reason.ViaId,
                    ViaName = p.Reason.ViaName,
                    ViaRoleId = p.Reason.ViaRoleId,
                    ViaRoleUrn = p.Reason.ViaRoleUrn
                })
            };
        }).ToList();
    }
}
