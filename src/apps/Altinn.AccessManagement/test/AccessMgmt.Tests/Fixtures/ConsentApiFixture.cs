using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.Tests.Moqdata;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.Common.PEP.Interfaces;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.AccessManagement.Tests.Fixtures;

/// <summary>
/// Profile fixture for the consent controller tests: <see cref="LegacyApiFixture"/>
/// (the consent schema comes from EF Core; the fixture also overlays the Yuniql
/// accessmanagement/delegation schemas) plus the DI configuration every consent
/// test class shares — the legacy <c>PdpPermitMock</c> PDP, a
/// mocked policy retrieval point, the JWT-cookie stub, and a shared
/// <c>IAmPartyRepository</c> mock populated from <see cref="MockParyRepositoryPopulator"/>.
/// </summary>
/// <remarks>
/// Lets the consent classes share one host + database via a collection fixture. The
/// party mock is Setup-only (no <c>Verify</c>), so a single shared instance is safe.
/// </remarks>
public class ConsentApiFixture : LegacyApiFixture
{
    /// <summary>The shared party-repository mock (Setup-only; no Verify).</summary>
    public Mock<IAmPartyRepository> AmPartyRepository { get; } = new();

    /// <summary>Initializes a new instance of the <see cref="ConsentApiFixture"/> class.</summary>
    public ConsentApiFixture()
    {
        MockParyRepositoryPopulator.SetupMockPartyRepository(AmPartyRepository);

        ConfigureServices(services =>
        {
            // Legacy PdpPermitMock flavour used by the consent tests.
            services.RemoveAll<IPDP>();
            services.AddSingleton<IPDP, PdpPermitMock>();

            services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
            services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
            services.AddSingleton<IAmPartyRepository>(AmPartyRepository.Object);
        });
    }
}
