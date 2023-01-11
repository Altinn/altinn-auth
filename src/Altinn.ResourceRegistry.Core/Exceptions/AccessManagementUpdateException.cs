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

        /// <summary>
        /// Serializable constructor
        /// </summary>
        /// <param name="info">the SerializationInfo object</param>
        /// <param name="context">the StreamingContext object</param>
        protected AccessManagementUpdateException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
} 
