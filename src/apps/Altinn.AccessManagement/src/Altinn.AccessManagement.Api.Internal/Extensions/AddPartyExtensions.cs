using Altinn.AccessManagement.Core.Models.Consent;
using Altinn.AccessMgmt.Persistence.Services.Models;
using Altinn.Authorization.Api.Contracts.Consent;
using Altinn.Authorization.Api.Contracts.Party;

namespace Altinn.AccessManagement.Api.Internal.Extensions
{
    public static class AddPartyExtensions
    {
        /// <summary>
        /// Converts a <see cref="AddPartyResult"/> object to a <see cref="AddPartyResultDto"/> object.
        /// </summary>
        /// <param name="core">The <see cref="AddPartyResult"/> object to convert.</param>
        /// <returns>A <see cref="AddPartyResultDto"/> object representing the converted data.</returns>
        public static AddPartyResultDto ToPartyResultDto(this AddPartyResult core)
        {
            return new AddPartyResultDto
            {
                PartyUuid = core.PartyUuid,
                PartyCreated = core.PartyCreated
            };
        }
    }
}
