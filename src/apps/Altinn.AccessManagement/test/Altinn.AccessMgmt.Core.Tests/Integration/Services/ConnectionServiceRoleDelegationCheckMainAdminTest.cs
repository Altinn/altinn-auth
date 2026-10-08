using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessMgmt.Core.Tests.Integration.Services;

/// <summary>
/// Integration tests for the main-administrator branch of <see cref="ConnectionService.RoleDelegationCheck"/>.
/// The enduser <c>roles/delegationcheck</c> endpoint always passes <c>toIsMainAdminForFrom: false</c>, so the
/// <c>mainAdminRoles</c> CTE (roles mapped from <c>hovedadministrator</c> via rolemap) is only reachable here.
/// </summary>
[IntegrationTest]
public class ConnectionServiceRoleDelegationCheckMainAdminTest : IClassFixture<ApiFixture>
{
    private const string ExplicitServiceDelegationUrn = "urn:altinn:rolecode:ektj";

    public ConnectionServiceRoleDelegationCheckMainAdminTest(ApiFixture fixture)
    {
        Fixture = fixture;
    }

    public ApiFixture Fixture { get; }

    /// <summary>
    /// A main administrator can delegate the explicit service delegation role (EKTJ) through the
    /// <c>hovedadministrator</c> → <c>ektj</c> rolemap.
    /// </summary>
    [Fact]
    public async Task RoleDelegationCheck_AsMainAdministrator_IncludesExplicitServiceDelegationRole()
    {
        using var scope = Fixture.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IConnectionService>();

        var result = await service.RoleDelegationCheck(
            TestData.HanSoloEnterprise.Id,
            TestData.LeiaOrgana.Id,
            toIsMainAdminForFrom: true,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsProblem);
        var ektj = Assert.Single(result.Value, r => r.Role.Id == RoleConstants.ExplicitServiceDelegation.Id);
        Assert.Equal(ExplicitServiceDelegationUrn, ektj.Role.Urn);
        Assert.True(ektj.Result);
        Assert.Contains(ektj.Reasons, r => r.Description == "MainAdministratorRole");
    }

    /// <summary>
    /// Without main-administrator status the same actor gets no delegable EKTJ role, proving the
    /// positive case above comes from the main-administrator rolemap.
    /// </summary>
    [Fact]
    public async Task RoleDelegationCheck_NotMainAdministrator_ExplicitServiceDelegationRoleNotDelegable()
    {
        using var scope = Fixture.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IConnectionService>();

        var result = await service.RoleDelegationCheck(
            TestData.HanSoloEnterprise.Id,
            TestData.LeiaOrgana.Id,
            toIsMainAdminForFrom: false,
            TestContext.Current.CancellationToken);

        Assert.False(result.IsProblem);
        Assert.DoesNotContain(result.Value, r => r.Role.Id == RoleConstants.ExplicitServiceDelegation.Id && r.Result);
    }
}
