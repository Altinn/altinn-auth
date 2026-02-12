using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.AccessMgmt.Core.Models
{
    public class ResourceAndAction
    {
        /// <summary>
        /// Gets or sets the collection of resource identifiers associated with the current instance.
        /// </summary>
        public IEnumerable<string> Resource { get; set; }

        /// <summary>
        /// Action part of the permission, e.g. "read", "write", "delete" etc.
        /// </summary>
        public string Action { get; set; }
    }
}
