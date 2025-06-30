using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.Authorization.Api.Contracts.Consent;

namespace Altinn.AccessManagement.Api.Maskinporten.Extensions
{
    /// <summary>
    /// Provides extension methods for transforming ConsentResourceAttribute to ConsentResourceAttributeExternal.
    /// </summary>
    public static class ConsentResourceAttributeExtensions
    {
        /// <summary>
        /// Maps a ConsentResourceAttribute to a ConsentResourceAttributeExternal.
        /// </summary>
        /// <param name="core">the ConsentResourceAttribute</param>
        /// <returns></returns>
        public static ConsentResourceAttributeDto ToConsentResourceAttributeExternal(this ConsentResourceAttribute core)
        {
            return new ConsentResourceAttributeDto
            {
                Type = core.Type,
                Value = core.Value
            };
        }
    }
}
