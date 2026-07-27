using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.Common.PEP.Interfaces;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Altinn.AccessManagement.Tests.Fixtures;

/// <summary>
/// Profile fixture shared by the controller tests that mock the policy / delegation
/// data layer and exercise the legacy <c>PdpPermitMock</c> PDP, currently the
/// <c>AppsInstanceDelegationController</c> tests and the ID-porten authorization
/// fixture that derives from this one. Bakes the configuration those classes would
/// otherwise register per-class so they can share one host via a collection fixture.
/// </summary>
/// <remarks>
/// All baked services are stateless mock implementations, so a single shared host is
/// safe. Registering a mock here is harmless for member classes that never resolve it.
/// </remarks>
public class RightsApiFixture : AccessMgmtApiFixture
{
    /// <summary>Initializes a new instance of the <see cref="RightsApiFixture"/> class.</summary>
    public RightsApiFixture()
    {
        ConfigureServices(services =>
        {
            services.RemoveAll<IPDP>();
            services.AddSingleton<IPDP, PdpPermitMock>();

            // AppsInstanceDelegationController resolves IAMPartyService to rewrite the
            // From-party of the instance; mock the whole service here. This is NOT
            // defaulted in the base fixture because the consent tests rely on the real
            // AmPartyService backed by their own IAmPartyRepository mock.
            services.AddSingleton<IAMPartyService, AMPartyServiceMock>();

            services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
            services.AddSingleton<IDelegationMetadataRepository, DelegationMetadataRepositoryMock>();
            services.AddSingleton<IPolicyFactory, PolicyFactoryMock>();
            services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
            services.AddSingleton<IAuthenticationClient>(new AuthenticationMock());
            services.AddSingleton<IAccessListsAuthorizationClient>(new AccessListsAuthorizationClientMock());
        });
    }
}
