using Altinn.Authorization.Api.Contracts.AccessManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.AccessMgmt.Core.Models
{
    public class RuleAccess
    {
        public string Key { get; set; }

        public IEnumerable<string> AccessorUrns { get; set; }

        public bool AccessListDenied { get; set; } = false;

        public List<AccessPackageDto.AccessPackageDtoCheck> PackageAllowAccess { get; set; }

        public HashSet<RuleCheckDto.Permision> PackageDenyAccess { get; set; }

        public List<RoleDtoCheck> RoleAllowAccess { get; set; }

        public HashSet<RuleCheckDto.Permision> RoleDenyAccess { get; set; }

        public List<RulePermission> ResourceAllowAccess { get; set; }
    }
}
