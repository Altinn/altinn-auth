using System.Net.Http.Headers;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Authorization.Tests.Fixtures;
using Altinn.Authorization.Tests.Util;
using Altinn.Platform.Authorization.Clients.Interfaces;
using Altinn.Platform.Authorization.Models.EventLog;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Moq;

namespace Altinn.Authorization.Tests.Integration
{
    [IntegrationTest]
    public class ExternalDecisionTest : IClassFixture<AuthorizationApiFixture>
    {
        private readonly AuthorizationApiFixture _fixture;
        private readonly HttpClient _client;
        private readonly Mock<IFeatureManager> featureManageMock = new Mock<IFeatureManager>();
        private readonly Mock<TimeProvider> timeProviderMock = new Mock<TimeProvider>();

        public ExternalDecisionTest(AuthorizationApiFixture fixture)
        {
            _fixture = fixture;
            SetupFeatureMock(true);
            SetupDateTimeMock();

            // Build the shared client through the same configured path the
            // per-test clients use, so the AuditLog feature flag set above is
            // actually honoured. Building via BuildClient() left _client on the
            // host default feature manager, so flag-dependent assertions could
            // pass for the wrong reason.
            _client = GetTestClient(featureManager: featureManageMock.Object);
        }

        [Fact]
        public async Task PDPExternal_Decision_AppInstanceUserHasReadAccess_ReturnsPermit()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "AltinnApps0008";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDPExternal_Decision_AppInstanceMultipleActions_ReturnsPermit()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "AltinnApps0010";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDPExternal_Decision_ResourceReadOnOrganization_ReturnsPermit()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "AltinnResourceRegistry0005";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDPExternal_Decision_ResourceReadOnSelf_ReturnsPermit()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");

            string testCase = "AltinnResourceRegistry0006";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);

            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDPExternal_Decision_ResourceReadOnOtherPerson_ReturnsNotApplicable()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "AltinnResourceRegistry0007";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDPExternal_Decision_ResourceUnknownAction_ReturnsNotApplicable()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "AltinnResourceRegistry0008";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// An unparseable urn:altinn:authorization:access-restriction resource attribute is a client error for that request
        /// and must return Indeterminate with a syntax-error status.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_InvalidAccessRestriction_ReturnsIndeterminate()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "AltinnResourceRegistry0100";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// A non-default access restriction (ClientDelegation) requires the admin scope. A caller without the admin
        /// scope must get Indeterminate with a processing-error status for that request.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_ClientDelegationWithoutAdminScope_ReturnsIndeterminate()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "AltinnResourceRegistry0101";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// ClientDelegation requires the via-party organization attribute. An admin-scoped caller omitting it
        /// must get Indeterminate with a missing-attribute status.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_ClientDelegationWithoutViaParty_ReturnsIndeterminate()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize altinn:authorization/authorize.admin");
            string testCase = "AltinnResourceRegistry0102";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// A whitespace-only via-party organization attribute is treated as missing for ClientDelegation.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_ClientDelegationWithWhitespaceViaParty_ReturnsIndeterminate()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize altinn:authorization/authorize.admin");
            string testCase = "AltinnResourceRegistry0105";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Admin-scoped ClientDelegation request where the system user has a client delegation received through the
        /// requested via-party. The access-restriction and via-party must be propagated to AccessManagement and give Permit.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_ClientDelegationWithMatchingViaParty_ReturnsPermit()
        {
            await AssertAdminScopedDecision("AltinnResourceRegistry0106");
        }

        /// <summary>
        /// Admin-scoped ClientDelegation request through a via-party the system user has no client delegation through.
        /// Should give NotApplicable even though a direct delegation exists for the same resource party.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_ClientDelegationWithOtherViaParty_ReturnsNotApplicable()
        {
            await AssertAdminScopedDecision("AltinnResourceRegistry0107");
        }

        /// <summary>
        /// Admin-scoped DirectAndHierarchy request where the system user has a direct delegation. Should give Permit.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_DirectAndHierarchyWithDelegation_ReturnsPermit()
        {
            await AssertAdminScopedDecision("AltinnResourceRegistry0108");
        }

        /// <summary>
        /// Admin-scoped DirectAndHierarchy request where the system user has no delegation. Should give NotApplicable.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_DirectAndHierarchyWithoutDelegation_ReturnsNotApplicable()
        {
            await AssertAdminScopedDecision("AltinnResourceRegistry0109");
        }

        private async Task AssertAdminScopedDecision(string testCase)
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize altinn:authorization/authorize.admin");
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// In a multi-request each request carries its own access-restriction and is evaluated independently.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_MultiRequest_PerRequestAccessRestriction_EvaluatedIndependently()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "AltinnResourceRegistry0103";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// access-restriction validation happens before enrichment and policy retrieval. Sub-requests with an invalid mode or a
        /// missing via-party targeting a resource without a policy must still get their own validation status, instead of
        /// the policy lookup failure replacing all results in the multi-request.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_MultiRequest_InvalidAccessRestrictionValidatedBeforeEnrichment()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize altinn:authorization/authorize.admin");
            string testCase = "AltinnResourceRegistry0104";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Scenario where org is listed in policy. Should work. Policy org is digir. Authz subject org is digdir
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_SubjectOrgListedInPolicy_ReturnsPermit()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "AltinnResourceRegistry0009";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Scenario where org is listed in policy. Should not work. Policy org is digir. Authz subject org is nav
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_SubjectOrgNotListedInPolicy_ReturnsNotApplicable()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "AltinnResourceRegistry0010";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Scenario where org is listed in policy. Should work. Policy org is digir. Authz subject org is digdir. Uses old attribute id
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_SubjectOrgListedInPolicyOldAttributeId_ReturnsPermit()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "AltinnResourceRegistry0011";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Scenario where systemuser has received delegation from the resource party for the resource. Should give Permit result.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_SystemUserWithResourceDelegation_ReturnsPermit()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "ResourceRegistry_SystemUserWithDelegation_Permit";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Scenario where systemuser has received delegation from the resource party for the resource. Should give Permit result.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_SystemUserWithResourceDelegation_WithEventLog_ReturnsPermit()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "ResourceRegistry_SystemUserWithDelegation_Permit";
            Mock<IFeatureManager> featureManageMock = new Mock<IFeatureManager>();
            featureManageMock
                .Setup(m => m.IsEnabledAsync("AuditLog"))
                .Returns(Task.FromResult(true));
            Mock<IEventsQueueClient> eventQueue = new Mock<IEventsQueueClient>();
            eventQueue.Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<AuthorizationEvent>(), It.IsAny<CancellationToken>()));
            AuthorizationEvent expectedAuthorizationEvent = TestSetupUtil.GetAuthorizationEvent(testCase);

            HttpClient client = GetTestClient(eventQueue.Object, featureManageMock.Object, timeProviderMock.Object);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
            AssertionUtil.AssertAuthorizationEvent(eventQueue, expectedAuthorizationEvent, Times.Once());
        }

        /// <summary>
        /// Scenario where systemuser has received delegation from the resource party for the resource. Should give Permit result.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_SystemUserWithAppDelegation_ReturnsPermit()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "AltinnApps_SystemUserWithDelegation_Permit";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Scenario where systemuser has received delegation from the resource party for the resource. Should give Permit result.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_SystemUserWithAppDelegation_MultipleObligationsBugFix_ReturnsPermit()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "AltinnApps_SystemUserWithDelegation_Permit";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act multiple times to ensure obligations is not cached multiple times
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, TestSetupUtil.CreateXacmlRequestExternal(testCase));
            contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, TestSetupUtil.CreateXacmlRequestExternal(testCase));
            contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, TestSetupUtil.CreateXacmlRequestExternal(testCase));

            // Assert
            Assert.True(contextResponse.Response[0].Obligations.Count() == 2, "Expected only the two instances of obligations from main app/resource policy in response");
        }

        /// <summary>
        /// Multi request scenario for 3 authorization checks in one request
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_MultiRequestThreeChecks_ReturnsPermitAndNotApplicable()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "AltinnResourceRegistryMulti0012";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Scenario where systemuser has received delegation, but request includes multiple subjects as org and orgnumber. Should NOT give Permit. 
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_SystemUserWithDelegation_TooManyRequestSubjects_ReturnsIndeterminate()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "ResourceRegistry_SystemUserWithDelegation_TooManyRequestSubjects_Indeterminate";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Scenario where the request has no subject and should give Indeterminate result. This ended in null reference exception and gave a syntaxerror status should give processing error.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_ResourceRegistry_NoSubject_ReturnsIndeterminate()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "ResourceRegistry_NoSubject_Indeterminate";

            Mock<IFeatureManager> featureManageMock = new Mock<IFeatureManager>();
            featureManageMock.Setup(m => m.IsEnabledAsync("DecisionRequestLogRequestOnError", It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            featureManageMock.Setup(m => m.IsEnabledAsync("AuditLog")).Returns(Task.FromResult(true));

            Mock<IEventsQueueClient> eventQueue = new Mock<IEventsQueueClient>();
            eventQueue.Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<AuthorizationEvent>(), It.IsAny<CancellationToken>()));
            HttpClient client = GetTestClient(eventLog: eventQueue.Object, featureManager: featureManageMock.Object);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
            AssertionUtil.CountAuthorizationEvent(eventQueue, Times.Once());
        }

        /// <summary>
        /// Scenario where the request has no resource and should give Indeterminate result. This ended in null reference exception and gave a syntaxerror status should give processing error.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_ResourceRegistry_NoResource_ReturnsIndeterminate()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "ResourceRegistry_NoResource_Indeterminate";

            Mock<IFeatureManager> featureManageMock = new Mock<IFeatureManager>();
            featureManageMock.Setup(m => m.IsEnabledAsync("DecisionRequestLogRequestOnError", It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            featureManageMock.Setup(m => m.IsEnabledAsync("AuditLog")).Returns(Task.FromResult(true));

            Mock<IEventsQueueClient> eventQueue = new Mock<IEventsQueueClient>();
            eventQueue.Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<AuthorizationEvent>(), It.IsAny<CancellationToken>()));
            HttpClient client = GetTestClient(eventLog: eventQueue.Object, featureManager: featureManageMock.Object);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
            AssertionUtil.CountAuthorizationEvent(eventQueue, Times.Once());
        }

        /// <summary>
        /// Scenario where systemuser has received delegation from the resource party for two resources. Multirequest should give Permit result for both.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_NoSubject_MultiRequest_ReturnsIndeterminate()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "ResourceRegistry_NoSubject_MultiRequest_Indeterminate";

            Mock<IFeatureManager> featureManageMock = new Mock<IFeatureManager>();
            featureManageMock.Setup(m => m.IsEnabledAsync("DecisionRequestLogRequestOnErrorMultiRequest", It.IsAny<CancellationToken>())).Returns(Task.FromResult(true));
            featureManageMock.Setup(m => m.IsEnabledAsync("AuditLog")).Returns(Task.FromResult(true));

            Mock<IEventsQueueClient> eventQueue = new Mock<IEventsQueueClient>();
            eventQueue.Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<AuthorizationEvent>(), It.IsAny<CancellationToken>()));
            HttpClient client = GetTestClient(eventLog: eventQueue.Object, featureManager: featureManageMock.Object);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
            AssertionUtil.CountAuthorizationEvent(eventQueue, Times.Exactly(2));
        }

        /// <summary>
        /// Scenario where the request has no resource and should give Indeterminate result. This ended in null reference exception and gave a syntaxerror status should give processing error.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_ResourceRegistry_NoResource_NoLog_ReturnsIndeterminate()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "ResourceRegistry_NoResource_Indeterminate";

            Mock<IFeatureManager> featureManageMock = new Mock<IFeatureManager>();
            featureManageMock.Setup(m => m.IsEnabledAsync("DecisionRequestLogRequestOnError", It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));
            featureManageMock.Setup(m => m.IsEnabledAsync("AuditLog")).Returns(Task.FromResult(true));

            Mock<IEventsQueueClient> eventQueue = new Mock<IEventsQueueClient>();
            eventQueue.Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<AuthorizationEvent>(), It.IsAny<CancellationToken>()));
            HttpClient client = GetTestClient(eventLog: eventQueue.Object, featureManager: featureManageMock.Object);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
            AssertionUtil.CountAuthorizationEvent(eventQueue, Times.Never());
        }

        /// <summary>
        /// Scenario where systemuser has received delegation from the resource party for two resources. Multirequest should give Permit result for both.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_NoSubject_MultiRequest_NoLog_ReturnsIndeterminate()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "ResourceRegistry_NoSubject_MultiRequest_Indeterminate";

            Mock<IFeatureManager> featureManageMock = new Mock<IFeatureManager>();
            featureManageMock.Setup(m => m.IsEnabledAsync("DecisionRequestLogRequestOnErrorMultiRequest", It.IsAny<CancellationToken>())).Returns(Task.FromResult(false));
            featureManageMock.Setup(m => m.IsEnabledAsync("AuditLog")).Returns(Task.FromResult(true));

            Mock<IEventsQueueClient> eventQueue = new Mock<IEventsQueueClient>();
            eventQueue.Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<AuthorizationEvent>(), It.IsAny<CancellationToken>()));
            HttpClient client = GetTestClient(eventLog: eventQueue.Object, featureManager: featureManageMock.Object);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
            AssertionUtil.CountAuthorizationEvent(eventQueue, Times.Never());
        }

        /// <summary>
        /// Scenario where systemuser has received delegation from the resource party for two resources. Multirequest should give Permit result for both.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_SystemUserWithDelegations_MultiRequest_ReturnsPermit()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "ResourceRegistry_SystemUserWithDelegations_MultiRequest_Permit";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Scenario where the subject holds role DAGL for a reportee that is a member of an access list for the
        /// access list protected resource. Should give Permit result.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_AccessListMember_ReturnsPermit()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "ResourceRegistry_AccessListMember_Permit";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Scenario where the subject holds the same role DAGL, but for a reportee that is a member of no access list
        /// for the access list protected resource. Access list membership is the only fact that differs from
        /// <see cref="PDPExternal_Decision_AccessListMember_ReturnsPermit"/>. Should give Deny result.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_AccessListNonMember_ReturnsDeny()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "ResourceRegistry_AccessListNonMember_Deny";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Scenario where the subject holds no role, but has received a delegation of the resource from a reportee
        /// that is a member of an access list for the access list protected resource. Should give Permit result.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_AccessListMemberWithUserDelegation_ReturnsPermit()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "ResourceRegistry_AccessListMemberWithUserDelegation_Permit";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Scenario where the subject has received the same delegation of the resource, but from a reportee that is a
        /// member of no access list for the access list protected resource. Access list membership is the only fact
        /// that differs from <see cref="PDPExternal_Decision_AccessListMemberWithUserDelegation_ReturnsPermit"/>.
        /// Should give Deny result.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_AccessListNonMemberWithUserDelegation_ReturnsDeny()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "ResourceRegistry_AccessListNonMemberWithUserDelegation_Deny";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Scenario where a systemuser has received a delegation of the access list protected resource from a reportee
        /// that is a member of an access list for that resource. Should give Permit result.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_AccessListSystemUserWithResourceDelegation_ReturnsPermit()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "ResourceRegistry_AccessListSystemUserWithResourceDelegation_Permit";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Scenario where the subject holds no role and has received no delegation, for a reportee that is a member
        /// of an access list for the access list protected resource. The access list is only consulted once the
        /// decision is Permit, so the response must be NotApplicable. Together with
        /// <see cref="PDPExternal_Decision_AccessListNonMemberWithoutUserAccess_ReturnsNotApplicable"/> it holds the
        /// response identical across both membership states, so a caller without access cannot use the decision to
        /// tell whether the reportee is on an access list.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_AccessListMemberWithoutUserAccess_ReturnsNotApplicable()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "ResourceRegistry_AccessListMemberWithoutUserAccess_NotApplicable";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Scenario where the subject holds no role and has received no delegation, for a reportee that is a member
        /// of no access list for the access list protected resource. Access list membership is the only fact that
        /// differs from <see cref="PDPExternal_Decision_AccessListMemberWithoutUserAccess_ReturnsNotApplicable"/>,
        /// and the response must stay NotApplicable rather than turning into the Deny that a subject with access
        /// would get.
        /// </summary>
        [Fact]
        public async Task PDPExternal_Decision_AccessListNonMemberWithoutUserAccess_ReturnsNotApplicable()
        {
            string token = PrincipalUtil.GetOrgToken("skd", "974761076", "altinn:authorization/authorize");
            string testCase = "ResourceRegistry_AccessListNonMemberWithoutUserAccess_NotApplicable";
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequestExternal(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        private HttpClient GetTestClient(IEventsQueueClient eventLog = null, IFeatureManager featureManager = null, TimeProvider timeProviderMock = null)
        {
            HttpClient client = _fixture.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    if (featureManager != null)
                    {
                        services.AddSingleton(featureManager);
                    }

                    if (eventLog != null)
                    {
                        services.AddSingleton(eventLog);
                    }

                    if (timeProviderMock != null)
                    {
                        services.AddSingleton(timeProviderMock);
                    }
                });
            }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            return client;
        }

        private void SetupFeatureMock(bool featureFlag)
        {
            featureManageMock
                .Setup(m => m.IsEnabledAsync("AuditLog"))
                .Returns(Task.FromResult(featureFlag));
        }

        private void SetupDateTimeMock()
        {
            timeProviderMock.Setup(x => x.GetUtcNow()).Returns(new DateTimeOffset(2018, 05, 15, 02, 05, 00, TimeSpan.Zero));
        }
    }
}
