using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altinn.Authorization.Api.Contracts.AccessManagement
{
    /// <summary>
    /// Represents a data transfer object that defines a right, including its key, display name, associated
    /// resources, and action identifier.
    /// </summary>
    public class RightDto
    {
        /// <summary>
        /// Unique key for action
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Name of the action to present in frontend
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Concatenated key for subresources from policy rule
        /// </summary>
        public IEnumerable<string> Resource { get; set; }

        /// <summary>
        /// Action
        /// </summary>
        public string Action { get; set; }
    }
}
