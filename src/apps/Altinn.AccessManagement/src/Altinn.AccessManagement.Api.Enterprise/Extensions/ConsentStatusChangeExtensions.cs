using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.Authorization.Api.Contracts.Consent;

namespace Altinn.AccessManagement.Api.Enterprise.Extensions
{
    /// <summary>
    /// Extension methods for consent models
    /// </summary>
    public static class ConsentStatusChangeExtensions
    {
        /// <summary>
        /// Converts a ConsentStatusChange to ConsentStatusChangeDto
        /// </summary>
        public static ConsentStatusChangeDto ToDto(this ConsentStatusChange statusChange)
        {
            return new ConsentStatusChangeDto
            {
                ConsentRequestId = statusChange.ConsentRequestId,
                EventType = statusChange.EventType.ToString(),
                ChangedDate = statusChange.ChangedDate,
            };
        }
    }
}
