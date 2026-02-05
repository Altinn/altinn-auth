using Altinn.Authorization.Api.Contracts.AccessManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.AccessMgmt.Core.Models
{
    public class ActionAccess
    {
        public string ActionKey { get; set; }

        public IEnumerable<string> AccessorUrns { get; set; }

        public bool AccessListDenied { get; set; } = false;

        public List<AccessPackageDto.AccessPackageDtoCheck> PackageAllowAccess { get; set; }

        public HashSet<ActionDto.Reason> PackageDenyAccess { get; set; }

        public List<RoleDtoCheck> RoleAllowAccess { get; set; }

        public HashSet<ActionDto.Reason> RoleDenyAccess { get; set; }

        public List<string> ResourceAllowAccess { get; set; }

        public HashSet<ActionDto.Reason> ResourceDenyAccess { get; set; }
    }
}
