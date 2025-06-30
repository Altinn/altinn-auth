using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.Authorization.Api.Contracts.Consent;

namespace Altinn.AccessManagement.Api.Enterprise.Extensions
{
    /// <summary>
    /// Provides extension methods for transforming ConsentRight to ConsentRightExternal.
    /// </summary>
    public static class ConsentRightDtoExtensions
    {
        /// <summary>
        /// Converts a <see cref="ConsentRightDto"/> object to a <see cref="ConsentRight"/> object.
        /// </summary>
        /// <param name="dto">The <see cref="ConsentRightDto"/> object to convert.</param>
        /// <returns>A <see cref="ConsentRight"/> object representing the converted data.</returns>
        public static ConsentRight ToConsentRight(this ConsentRightDto dto)
        {
            ConsentRight consentRight = new ConsentRight
            {
                Action = dto.Action,
                Resource = dto.Resource.Select(static x => x.ToConsentResourceAttribute()).ToList()
            };

            if (dto.Metadata != null)
            {
                consentRight.Metadata = new MetadataDictionary();
                foreach (var item in dto.Metadata)
                {
                    consentRight.Metadata.Add(item.Key, item.Value);
                }
            }

            return consentRight;
        }
    }
}
