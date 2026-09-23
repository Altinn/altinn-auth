using System.Runtime.Serialization;

namespace Altinn.ResourceRegistry.Core.Exceptions
{
    /// <summary>
    /// Exception to handle when AccessManagement is not updated correctly
    /// </summary>
    [Serializable]
    public class AccessManagementUpdateException : Exception
    {
        /// <summary>
        /// Constructor with error message
        /// </summary>
        /// <param name="message">The error message</param>
        public AccessManagementUpdateException(string message) : base(message)
        {
        }
    }
} 
