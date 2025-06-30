using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.Authorization.Api.Contracts.Consent;

namespace Altinn.AccessManagement.Api.Enterprise.Extensions
{
    /// <summary>
    /// Provides extension methods for transforming ConsentResourceAttributeExternal to ConsentResourceAttribute.
    /// </summary>
    public static class ConsentResourceAttributeExternalExtensions
    {
        /// <summary>
        /// Maps a ConsentResourceAttributeExternal to a ConsentResourceAttribute.
        /// </summary>
        /// <param name="external">the ConsentResourceAttributeExternal</param>
        /// <returns></returns>
        public static ConsentResourceAttribute ToConsentResourceAttribute(this ConsentResourceAttributeDto external)
        {
            return new ConsentResourceAttribute
            {
                Type = external.Type,
                Value = external.Value
            };
        }
    }
}
