using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.Authorization.Api.Contracts.Consent;

namespace Altinn.AccessManagement.Api.Enterprise.Extensions
{
    /// <summary>
    /// Provides extension methods for transforming ConsentRight to ConsentRightExternal.
    /// </summary>
    public static class ConsentRightExtensions
    {
        /// <summary>
        /// Converts a <see cref="ConsentRight"/> object to a <see cref="ConsentRightDto"/> object.
        /// </summary>
        /// <param name="core">The <see cref="ConsentRight"/> object to convert.</param>
        /// <returns>A <see cref="ConsentRightDto"/> object representing the converted data.</returns>
        public static ConsentRightDto ToConsentRightExternal(this ConsentRight core)
        {
            return new ConsentRightDto
            {
                Action = core.Action,
                Resource = core.Resource.Select(static x => x.ToConsentResourceAttributeExternal()).ToList(),
                Metadata = core.Metadata
            };
        }
    }
}
