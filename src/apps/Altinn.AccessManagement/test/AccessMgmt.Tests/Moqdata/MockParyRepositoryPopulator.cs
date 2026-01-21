using Altinn.AccessManagement.Core.Models.Party;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.Authorization.Api.Contracts.Register;
using Moq;

namespace AccessMgmt.Tests.Moqdata
{
    /// <summary>
    /// Utility class for populating a mock IAmPartyRepository with test data
    /// </summary>
    public static class MockParyRepositoryPopulator
    {
        public static void SetupMockPartyRepository(Mock<IAmPartyRepository> _mockAmPartyRepository)
        {
            // Reset all existing setups
            _mockAmPartyRepository.Reset();

            // Setup specific mock responses for test data
            MinimalParty elenafjear = new MinimalParty
            {
                PartyUuid = Guid.Parse("d5b861c8-8e3b-44cd-9952-5315e5990cf5"),
                PartyId = 513370001,
                Name = "ELENA FJÆR",
                PersonId = "01025161013",
                PartyType = EntityTypeConstants.Person // Person type
            };

            // Person: 01025161013
            _mockAmPartyRepository.Setup(x => x.GetByPersonNo(PersonIdentifier.Parse("01025161013"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(elenafjear);

            _mockAmPartyRepository.Setup(x => x.GetByPartyId(elenafjear.PartyId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(elenafjear);

            _mockAmPartyRepository.Setup(x => x.GetByUuid(elenafjear.PartyUuid, It.IsAny<CancellationToken>()))
                .ReturnsAsync(elenafjear);

            // Organization: 810419512
            MinimalParty smekkFullBank = new MinimalParty
            {
                PartyUuid = Guid.Parse("8ef5e5fa-94e1-4869-8635-df86b6219181"),
                PartyId = 501235,
                Name = "SmekkFull Bank AS",
                OrganizationId = "810419512",
                PartyType = EntityTypeConstants.Organization // Organization type
            };

            _mockAmPartyRepository.Setup(x => x.GetByOrgNo(OrganizationNumber.Parse(smekkFullBank.OrganizationId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(smekkFullBank);

            _mockAmPartyRepository.Setup(x => x.GetByPartyId(smekkFullBank.PartyId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(smekkFullBank);

            _mockAmPartyRepository.Setup(x => x.GetByUuid(smekkFullBank.PartyUuid, It.IsAny<CancellationToken>()))
                .ReturnsAsync(smekkFullBank);

            // Organization: 991825827
            MinimalParty digitaliseringsdirektoratet = new MinimalParty
            {
                PartyUuid = Guid.Parse("CDDA2F11-95C5-4BE4-9690-54206FF663F6"),
                PartyId = 501236,
                Name = "DIGITALISERINGSDIREKTORATET",
                OrganizationId = "991825827",
                PartyType = EntityTypeConstants.Organization // Organization type
            };

            _mockAmPartyRepository.Setup(x => x.GetByOrgNo(OrganizationNumber.Parse(digitaliseringsdirektoratet.OrganizationId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(digitaliseringsdirektoratet);

            _mockAmPartyRepository.Setup(x => x.GetByPartyId(digitaliseringsdirektoratet.PartyId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(digitaliseringsdirektoratet);

            _mockAmPartyRepository.Setup(x => x.GetByUuid(digitaliseringsdirektoratet.PartyUuid, It.IsAny<CancellationToken>()))
                .ReturnsAsync(digitaliseringsdirektoratet);

            // Organization: 810418192
            MinimalParty banksupplierorg = new MinimalParty
            {
                PartyUuid = Guid.Parse("00000000-0000-0000-0005-000000004219"),
                PartyId = 50004219,
                Name = "KOLSAAS OG FLAAM",
                OrganizationId = "810418192",
                PartyType = EntityTypeConstants.Organization // Organization type
            };

            _mockAmPartyRepository.Setup(x => x.GetByOrgNo(OrganizationNumber.Parse("810418192"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(banksupplierorg);

            _mockAmPartyRepository.Setup(x => x.GetByPartyId(banksupplierorg.PartyId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(banksupplierorg);

            _mockAmPartyRepository.Setup(x => x.GetByUuid(banksupplierorg.PartyUuid, It.IsAny<CancellationToken>()))
                .ReturnsAsync(banksupplierorg);

            // Organization: 810418192
            MinimalParty lepsoyogTonstad = new MinimalParty
            {
                PartyUuid = Guid.Parse("00000000-0000-0000-0005-000000006078"),
                PartyId = 50006078,
                Name = "LEPSØY OG TONSTAD",
                OrganizationId = "910493353",
                PartyType = EntityTypeConstants.Organization // Organization type
            };

            _mockAmPartyRepository.Setup(x => x.GetByOrgNo(OrganizationNumber.Parse("910493353"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(lepsoyogTonstad);

            _mockAmPartyRepository.Setup(x => x.GetByPartyId(lepsoyogTonstad.PartyId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lepsoyogTonstad);

            _mockAmPartyRepository.Setup(x => x.GetByUuid(lepsoyogTonstad.PartyUuid, It.IsAny<CancellationToken>()))
                .ReturnsAsync(lepsoyogTonstad);

            // Person: 01025181049 (for duplicate test)
            _mockAmPartyRepository.Setup(x => x.GetByPersonNo(PersonIdentifier.Parse("01025181049"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MinimalParty
                {
                    PartyUuid = Guid.Parse("d47ac10b-58cc-4372-a567-0e02b2c3d483"),
                    PartyId = 501238,
                    Name = "Kari Nordmann",
                    PersonId = "01025181049",
                    PartyType = EntityTypeConstants.Person // Person type
                });

            // Non-existing person: 01014922047 (should return null)
            _mockAmPartyRepository.Setup(x => x.GetByPersonNo(PersonIdentifier.Parse("01014922047"), It.IsAny<CancellationToken>()))
                .ReturnsAsync((MinimalParty)null);
        }
    }
}
