using Altinn.ResourceRegistry.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.ResourceRegistry.Core.Models
{
    public class ResourceSearch
    {
        /// <summary>
        /// ID
        /// </summary>
        public string? id { get; set; }

        /// <summary>
        /// Title
        /// </summary>
        public string? title { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        public string? description { get; set; }

        /// <summary>
        /// ResourceType
        /// </summary>
        public ResourceType? resourceType { get; set; }
    }
}
