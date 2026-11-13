using System.Net;
using System.Net.Http.Headers;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Authorization.Api.Contracts.Authorization;
using Altinn.Authorization.Api.Contracts.Register;
using Altinn.Authorization.Api.Contracts.ResourceRegistry;
using Altinn.Authorization.Enums;
using Altinn.Authorization.Models.ResourceRegistry;
using Altinn.Authorization.Tests.Fixtures;
using Altinn.Authorization.Tests.MockServices;
using Altinn.Authorization.Tests.Util;
using Altinn.Platform.Authorization.Clients.Interfaces;
using Altinn.Platform.Authorization.Exceptions;
using Altinn.Platform.Authorization.Helpers;
using Altinn.Platform.Authorization.Models.AccessManagement;
using Altinn.Platform.Authorization.Models.EventLog;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.ResourceRegistry.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Moq;

namespace Altinn.Authorization.Tests.Integration;

/// <summary>
/// A lookup the decision depends on that fails must give Indeterminate with a processing-error status.
/// It must never be answered as if the lookup found nothing, which reads as "no access", or be skipped.
/// </summary>
[IntegrationTest]
public class DependencyFailureDecisionTests : IClassFixture<AuthorizationApiFixture>
{
    private const string ProcessingError = "urn:oasis:names:tc:xacml:1.0:status:processing-error";

    private readonly AuthorizationApiFixture _fixture;
    private readonly Mock<IEventsQueueClient> _eventQueue = new();

    public DependencyFailureDecisionTests(AuthorizationApiFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// The subject has no role for the party, so the answer depends on the delegations. Without the failure the
    /// decision is NotApplicable, and before this was fixed the failed lookup gave that same answer.
    /// </summary>
    [Fact]
    public async Task Authorize_DelegationLookupFails_ReturnsIndeterminateWithProcessingError()
    {
        HttpClient client = CreateClient(UseFailingDelegationLookup, logRequestOnError: true);
        HttpRequestMessage request = TestSetupUtil.CreateXacmlRequestExternal("AltinnResourceRegistry0007");

        HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        XacmlJsonResponse decision = await ReadDecision(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        XacmlJsonResult result = Assert.Single(decision.Response);
        AssertIndeterminateWithProcessingError(result);
        AssertionUtil.CountAuthorizationEvent(_eventQueue, Times.Once());
    }

    /// <summary>
    /// The internal endpoint answers a failed lookup the same way as the external one. The subject is a system
    /// user without a delegation, which is NotApplicable when the lookup works.
    /// </summary>
    [Fact]
    public async Task Decision_DelegationLookupFails_ReturnsIndeterminateWithProcessingError()
    {
        HttpClient client = CreateClient(UseFailingDelegationLookup);
        HttpRequestMessage request = TestSetupUtil.CreateJsonProfileXacmlRequest("ResourceRegistry_SystemUserWithoutDelegation_NotApplicable");

        HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        XacmlJsonResponse decision = await ReadDecision(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertIndeterminateWithProcessingError(Assert.Single(decision.Response));
    }

    /// <summary>
    /// A failed lookup for one request of a multi-request is answered for that request alone. The requests that
    /// get Permit from a role never reach the delegation lookup, and keep their decision.
    /// </summary>
    [Fact]
    public async Task Authorize_MultiRequestDelegationLookupFails_OtherRequestsKeepTheirDecision()
    {
        const string TestCase = "AltinnResourceRegistryMulti0012";
        HttpClient client = CreateClient(UseFailingDelegationLookup);
        XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(TestCase);
        Assert.Contains(expected.Response, r => r.Decision == nameof(XacmlContextDecision.Permit));
        Assert.Contains(expected.Response, r => r.Decision == nameof(XacmlContextDecision.NotApplicable));

        XacmlJsonResponse decision = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, TestSetupUtil.CreateXacmlRequestExternal(TestCase));

        Assert.Equal(expected.Response.Count, decision.Response.Count);
        for (int i = 0; i < expected.Response.Count; i++)
        {
            if (expected.Response[i].Decision == nameof(XacmlContextDecision.Permit))
            {
                Assert.Equal(nameof(XacmlContextDecision.Permit), decision.Response[i].Decision);
            }
            else
            {
                AssertIndeterminateWithProcessingError(decision.Response[i]);
            }

            // The attributes the client asked to get back are what ties a result to its request. They are the
            // same for a request whose lookup failed as for the decision it would otherwise have got.
            Assert.NotNull(decision.Response[i].Category);
            Assert.Equal(
                Newtonsoft.Json.JsonConvert.SerializeObject(expected.Response[i].Category),
                Newtonsoft.Json.JsonConvert.SerializeObject(decision.Response[i].Category));
        }
    }

    /// <summary>
    /// The subject has a role that gives Permit, and the resource requires the party to be on an access list.
    /// When the resource cannot be read, the PDP does not know that, and the Permit must not stand.
    /// </summary>
    [Fact]
    public async Task Authorize_ResourceLookupFailsBeforeAccessListCheck_ReturnsIndeterminateInsteadOfPermit()
    {
        HttpClient client = CreateClient(services => services.AddSingleton<IResourceRegistry>(new FailingResourceLookup()));
        HttpRequestMessage request = TestSetupUtil.CreateXacmlRequestExternal("ResourceRegistry_AccessListMember_Permit");

        HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        XacmlJsonResponse decision = await ReadDecision(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertIndeterminateWithProcessingError(Assert.Single(decision.Response));
    }

    /// <summary>
    /// Asserts the answer a failed lookup must give: no decision, a status that says it was a technical failure,
    /// and none of the obligations of a Permit.
    /// </summary>
    private static void AssertIndeterminateWithProcessingError(XacmlJsonResult result)
    {
        Assert.Equal(nameof(XacmlContextDecision.Indeterminate), result.Decision);
        Assert.Equal(ProcessingError, result.Status.StatusCode.Value);
        Assert.Null(result.Obligations);
    }

    /// <summary>
    /// Reads the XACML JSON response of a decision.
    /// </summary>
    private static async Task<XacmlJsonResponse> ReadDecision(HttpResponseMessage response)
    {
        string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<XacmlJsonResponse>(content);
    }

    /// <summary>
    /// Replaces Access Management with one that fails the delegation lookup, and answers everything else.
    /// </summary>
    private static void UseFailingDelegationLookup(IServiceCollection services)
    {
        services.AddSingleton<AccessManagementWrapperMock>();
        services.AddSingleton<IAccessManagementWrapper>(sp => new FailingDelegationLookup(sp.GetRequiredService<AccessManagementWrapperMock>()));
    }

    /// <summary>
    /// Builds a host with the failing service, and a client that is authorized for the external endpoint.
    /// </summary>
    /// <param name="configureServices">Registers the service that fails</param>
    /// <param name="logRequestOnError">Whether the feature flag for audit-logging a failed request is on</param>
    private HttpClient CreateClient(Action<IServiceCollection> configureServices, bool logRequestOnError = false)
    {
        Mock<IFeatureManager> featureManager = new();
        featureManager.Setup(m => m.IsEnabledAsync("AuditLog")).ReturnsAsync(true);
        featureManager.Setup(m => m.IsEnabledAsync("DecisionRequestLogRequestOnError", It.IsAny<CancellationToken>())).ReturnsAsync(logRequestOnError);

        HttpClient client = _fixture.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddSingleton(featureManager.Object);
                services.AddSingleton(_eventQueue.Object);
                configureServices(services);
            });
        }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
        return client;
    }

    /// <summary>
    /// Access Management answers the role lookup, and fails the delegation lookup.
    /// </summary>
    private sealed class FailingDelegationLookup(AccessManagementWrapperMock inner) : IAccessManagementWrapper
    {
        public Task<IEnumerable<DelegationChangeDto>> GetAllDelegationChanges(DelegationChangeInputDto input, CancellationToken cancellationToken = default) =>
            throw new DependencyFailedException(DependencyCall.AccessManagement, HttpStatusCode.ServiceUnavailable);

        public Task<IEnumerable<DelegationChangeDto>> GetAllDelegationChanges(CancellationToken cancellationToken = default, params Action<DelegationChangeInputDto>[] actions) =>
            throw new DependencyFailedException(DependencyCall.AccessManagement, HttpStatusCode.ServiceUnavailable);

        public Task<IEnumerable<AuthorizedPartyDto>> GetAuthorizedParties(CancellationToken cancellationToken = default) =>
            inner.GetAuthorizedParties(cancellationToken);

        public Task<AuthorizedPartyDto> GetAuthorizedParty(int partyId, CancellationToken cancellationToken = default) =>
            inner.GetAuthorizedParty(partyId, cancellationToken);

        public Task<PipResponseDto> GetRolesAndAccessPackages(Guid to, Guid from, AccessRestriction accessRestriction = AccessRestriction.None, string viaPartyOrganizationNumber = null, CancellationToken cancellationToken = default) =>
            inner.GetRolesAndAccessPackages(to, from, accessRestriction, viaPartyOrganizationNumber, cancellationToken);
    }

    /// <summary>
    /// Resource Registry answers the policy and access list lookups, and fails the lookup of the resource itself.
    /// </summary>
    private sealed class FailingResourceLookup : IResourceRegistry
    {
        private readonly ResourceRegistryMock _inner = new();

        public Task<ServiceResource> GetResourceAsync(string resourceId, CancellationToken cancellationToken = default) =>
            throw new DependencyFailedException(DependencyCall.ResourceRegistry, HttpStatusCode.InternalServerError);

        public Task<XacmlPolicy> GetResourcePolicyAsync(string resourceId, CancellationToken cancellationToken = default) =>
            _inner.GetResourcePolicyAsync(resourceId, cancellationToken);

        public Task<IEnumerable<AccessListResourceMembershipWithActionFilterDto>> GetMembershipsForResourceForParty(PartyUrn partyUrn, ResourceIdUrn resourceIdUrn, CancellationToken cancellationToken = default) =>
            _inner.GetMembershipsForResourceForParty(partyUrn, resourceIdUrn, cancellationToken);

        public Task<IEnumerable<AccessListInfoDto>> GetMembershipsForParty(PartyUrn.PartyUuid partyUrn, CancellationToken cancellationToken = default) =>
            _inner.GetMembershipsForParty(partyUrn, cancellationToken);
    }
}
