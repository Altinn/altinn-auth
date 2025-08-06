using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.Authorization.Api.Contracts.Consent;
using Altinn.Authorization.Api.Contracts.Party;

namespace Altinn.AccessManagement.Api.Internal.Extensions
{
    public static class PartyBaseDtoExtensions
    {
        /// <summary>
        /// Converts a <see cref="PartyBaseDto"/> object to a <see cref="PartyBaseInternal"/> object.
        /// </summary>
        /// <param name="core">The <see cref="PartyBaseDto"/> object to convert.</param>
        /// <returns>A <see cref="PartyBaseInternal"/> object representing the converted data.</returns>
        public static PartyBaseInternal ToCore(this PartyBaseDto core)
        {
            return new PartyBaseInternal
            {
                PartyUuid = core.PartyUuid,
                EntityType = core.EntityType,
                EntityVariantType = core.EntityVariantType,
                DisplayName = core.DisplayName,
                CreatedBy = core.CreatedBy
            };
        }
    }
}
