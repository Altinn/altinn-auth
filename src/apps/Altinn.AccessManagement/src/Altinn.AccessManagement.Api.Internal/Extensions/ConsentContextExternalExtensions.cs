using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.Authorization.Api.Contracts.Consent;

namespace Altinn.AccessManagement.Api.Internal.Extensions
{
    /// <summary>
    /// Provides extension methods for transforming ConsentRight to ConsentRightExternal.
    /// </summary>
    public static class ConsentContextExternalExtensions
    {
        /// <summary>
        /// Converts a ConsentContextExternal object to a ConsentContext object.
        /// </summary>
        /// <param name="core">the ConsentContextExternal</param>
        /// <returns></returns>
        public static ConsentContext ToConsentContext(this ConsentContextDto core)
        {
            return new ConsentContext
            {
                Language = core.Language,
            };
        }
    }
}
