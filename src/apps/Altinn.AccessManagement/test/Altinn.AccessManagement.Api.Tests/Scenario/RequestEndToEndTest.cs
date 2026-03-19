using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessManagement.Api.Tests.Scenario;

public class RequestEndToEndTest
{
    private const string ServiceOwnerRoute = "accessmanagement/api/v1/serviceowner/delegationrequests";
    private const string EnduserRoute = "accessmanagement/api/v1/enduser/request";

    private static HttpClient CreateServiceOwnerClient(ApiFixture fixture, string orgNo)
    {
        var client = fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim("consumer", JsonSerializer.Serialize(new { authority = "iso6523-actorid-upis", ID = $"0192:{orgNo}" })));
            claims.Add(new Claim("scope", AuthzConstants.ALTINN_SERVICEOWNER_DELEGATIONREQUESTS_WRITE));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    private static HttpClient CreateServiceOwnerReadClient(ApiFixture fixture, string orgNo)
    {
        var client = fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim("consumer", JsonSerializer.Serialize(new { authority = "iso6523-actorid-upis", ID = $"0192:{orgNo}" })));
            claims.Add(new Claim("scope", AuthzConstants.ALTINN_SERVICEOWNER_DELEGATIONREQUESTS_READ));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    private static HttpClient CreateEnduserClient(ApiFixture fixture, Guid partyUuid)
    {
        var client = fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, partyUuid.ToString()));
            claims.Add(new Claim("scope", AuthzConstants.SCOPE_PORTAL_ENDUSER));
            claims.Add(new Claim("scope", AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_READ));
            claims.Add(new Claim("scope", AuthzConstants.POLICY_ACCESS_MANAGEMENT_ENDUSER_WRITE));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    private static void EnableFeatureFlags(ApiFixture fixture)
    {
        fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentResource);
        fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentPackage);
    }
}
