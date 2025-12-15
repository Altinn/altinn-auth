using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.Authorization.Api.Contracts.Register;

namespace Altinn.AccessManagement.Tests.Mocks
{
    public class AMPartyServiceMock : IAMPartyService
    {
        public Task<MinimalParty> GetByOrgNo(OrganizationNumber orgNo, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<MinimalParty> GetByPartyId(int partyId, CancellationToken cancellationToken = default)
        {
            switch (partyId)
            {
                case 50001337:
                    return Task.FromResult(new MinimalParty
                    {
                        PartyId = 50001337,
                        PartyUuid = Guid.Parse("26de0d55-3b52-4703-9a4d-78cd60353daa"),
                        Name = "Test Org",
                        OrganizationId = "888888888",
                        PartyType = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d")
                    });
                case 50001336:
                    return Task.FromResult(new MinimalParty
                    {
                        PartyId = 50001336,
                        PartyUuid = Guid.Parse("7a07a73e-f0ff-432f-8275-b660f591dc26"),
                        Name = "Test Org2",
                        OrganizationId = "999999999",
                        PartyType = Guid.Parse("8c216e2f-afdd-4234-9ba2-691c727bb33d")
                    });
                default:
                    throw new NotImplementedException();
            }
        }

        public Task<MinimalParty> GetByPersonNo(PersonIdentifier personNo, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<MinimalParty> GetByUid(Guid partyUuid, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<MinimalParty> GetByUserId(int userId, CancellationToken cancellationToken = default)
        {
            switch (userId)
            {
                case 20001337:
                    return Task.FromResult(new MinimalParty
                    {
                        PartyId = 501337,
                        PartyUuid = Guid.Parse("9c257fa7-0daa-4f69-be17-fd6805b15484"),
                        Name = "Test Person",
                        PartyType = Guid.Parse("bfe09e70-e868-44b3-8d81-dfe0e13e058a")
                    });
                case 20001336:
                    return Task.FromResult(new MinimalParty
                    {
                        PartyId = 501336,
                        PartyUuid = Guid.Parse("58241333-3438-4652-8750-4faff77f6046"),
                        Name = "Test Person",
                        PartyType = Guid.Parse("bfe09e70-e868-44b3-8d81-dfe0e13e058a")
                    });
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
