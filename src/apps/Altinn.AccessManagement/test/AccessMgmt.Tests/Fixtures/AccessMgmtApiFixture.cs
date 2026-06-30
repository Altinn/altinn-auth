using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using TestUtilsMocks = Altinn.AccessManagement.TestUtils.Mocks;

namespace Altinn.AccessManagement.Tests.Fixtures;

/// <summary>
/// AccessMgmt.Tests base fixture. Extends the shared <see cref="ApiFixture"/> with the
/// external-platform client mocks that virtually every AccessManagement test needs, so
/// test classes no longer repeat the same registrations. Mirrors the project-local
/// pattern of <c>AuthorizationApiFixture</c> (each test project bakes its own mock graph
/// into a fixture rather than centralising mocks in TestUtils).
/// </summary>
/// <remarks>
/// The registrations run before any per-class <see cref="ApiFixture.ConfigureServices"/>
/// callback (this fixture is constructed before the test class), so a class that needs
/// different behaviour for one of these clients can still override it.
/// Data-layer mocks (repositories, PDP, policy retrieval/factory) and signing-key
/// overrides remain per-class: they vary by test and are not safe to default.
/// </remarks>
public class AccessMgmtApiFixture : ApiFixture
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccessMgmtApiFixture"/> class.
    /// </summary>
    public AccessMgmtApiFixture()
    {
        ConfigureServices(services =>
        {
            services.AddSingleton<IPartiesClient, PartiesClientMock>();
            services.AddSingleton<IProfileClient, TestUtilsMocks.ProfileClientMock>();
            services.AddSingleton<IAltinnRolesClient, TestUtilsMocks.AltinnRolesClientMock>();
            services.AddSingleton<IAltinn2RightsClient, Altinn2RightsClientDataMock>();
        });
    }
}
