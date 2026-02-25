using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Models
{
    public class Right
    {
        public string Key { get; set; }

        public IEnumerable<string> AccessorUrns { get; set; }

        public bool AccessListDenied { get; set; } = false;

        public List<AccessPackageDto.AccessPackageDtoCheck> PackageAllowAccess { get; set; }

        public HashSet<RightCheckDto.Permision> PackageDenyAccess { get; set; }

        public List<RoleDtoCheck> RoleAllowAccess { get; set; }

        public HashSet<RightCheckDto.Permision> RoleDenyAccess { get; set; }

        public List<RightPermission> ResourceAllowAccess { get; set; }
    }
}
