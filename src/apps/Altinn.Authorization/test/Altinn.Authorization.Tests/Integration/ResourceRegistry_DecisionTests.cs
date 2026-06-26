using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Authorization.ABAC.Xacml;
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
using Xunit;

namespace Altinn.Authorization.Tests.Integration
{
    [IntegrationTest]
    public class ResourceRegistry_DecisionTests : IClassFixture<AuthorizationApiFixture>
    {
        private readonly AuthorizationApiFixture _fixture;
        private readonly HttpClient _client;
        private readonly Mock<IFeatureManager> featureManageMock = new Mock<IFeatureManager>();
        private readonly Mock<TimeProvider> timeProviderMock = new Mock<TimeProvider>();

        public ResourceRegistry_DecisionTests(AuthorizationApiFixture fixture)
        {
            _fixture = fixture;
            SetupFeatureMock("AuditLog", true);
            SetupFeatureMock("SystemUserAccessPackageAuthorization", true);
            SetupFeatureMock("UserAccessPackageAuthorization", true);
            SetupDateTimeMock();

            // Build the shared client through the configured path so the feature
            // flags set above are honoured. Building via BuildClient() left _client
            // on the host default feature manager, so the access-package
            // authorization flags were ignored and those decision paths could be
            // asserted without actually being enabled.
            _client = GetTestClient(featureManager: featureManageMock.Object);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_OedFormuesfullmakt_Xml_ReturnsPermit()
        {
            string testCase = "ResourceRegistry_OedFormuesfullmakt_Xml_Permit";
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequest(testCase);
            XacmlContextResponse expected = TestSetupUtil.ReadExpectedResponse(testCase);

            // Act
            XacmlContextResponse contextResponse = await TestSetupUtil.GetXacmlContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_OedFormuesfullmakt_Json_ReturnsPermit()
        {
            string testCase = "ResourceRegistry_OedFormuesfullmakt_Json_Permit";
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_OedFormuesfullmakt_Xml_ReturnsNotApplicable()
        {
            string testCase = "ResourceRegistry_OedFormuesfullmakt_Xml_Indeterminate";
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequest(testCase);
            XacmlContextResponse expected = TestSetupUtil.ReadExpectedResponse(testCase);

            // Act
            XacmlContextResponse contextResponse = await TestSetupUtil.GetXacmlContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_OedFormuesfullmakt_Json_ReturnsNotApplicable()
        {
            string testCase = "ResourceRegistry_OedFormuesfullmakt_Json_Indeterminate";
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where the reportee organization has access to 'ttd-accesslist-resource' through access list membership without any action filter.
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_AccessListAuthorization_Json_ReturnsPermit()
        {
            string testCase = "ResourceRegistry_AccessListAuthorization_Json_Permit";
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where the reportee organization does NOT have access to 'ttd-accesslist-resource' through any access list membership.
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_AccessListAuthorization_Json_ReturnsDeny()
        {
            string testCase = "ResourceRegistry_AccessListAuthorization_Json_Deny";
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where the reportee organization has access to 'ttd-accesslist-resource' through access list membership with matching action filter.
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_AccessListAuthorization_Json_WithActionFilterMatch_ReturnsPermit()
        {
            string testCase = "ResourceRegistry_AccessListAuthorization_Json_PermitWithActionFilterMatch";
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where the reportee organization has access to 'ttd-accesslist-resource' through access list membership but with action filter not matching the request action.
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_AccessListAuthorization_Json_ActionFilterNotMatching_ReturnsDeny()
        {
            string testCase = "ResourceRegistry_AccessListAuthorization_Json_DenyActionFilterNotMatching";
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where the reportee is a person. Currently the access list authorization service only supports organizations.
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_AccessListAuthorization_Json_PersonReporteeNotSupported_ReturnsDeny()
        {
            string testCase = "ResourceRegistry_AccessListAuthorization_Json_DenyAccessListDontSupportPerson";
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_ApiDelegationByPartyIdXml_ReturnsPermit()
        {
            string testCase = "AltinnResourceRegistry0001";
            Mock<IEventsQueueClient> eventQueue = new Mock<IEventsQueueClient>();
            eventQueue.Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<AuthorizationEvent>(), It.IsAny<CancellationToken>()));
            AuthorizationEvent expectedAuthorizationEvent = TestSetupUtil.GetAuthorizationEvent(testCase);
            HttpClient client = GetTestClient(eventQueue.Object, featureManageMock.Object, timeProviderMock.Object);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequest(testCase);
            XacmlContextResponse expected = TestSetupUtil.ReadExpectedResponse(testCase);

            // Act
            XacmlContextResponse contextResponse = await TestSetupUtil.GetXacmlContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
            AssertionUtil.AssertAuthorizationEvent(eventQueue, expectedAuthorizationEvent, Times.Once());
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_ApiDelegationByPartyIdJson_ReturnsPermit()
        {
            string testCase = "AltinnResourceRegistry0002";
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_ApiDelegationByOrganization_ReturnsPermit()
        {
            string testCase = "AltinnResourceRegistry0003";
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_EventsSubscribe_ReturnsPermit()
        {
            string testCase = "AltinnResourceRegistry0004";
            Mock<IEventsQueueClient> eventQueue = new Mock<IEventsQueueClient>();
            eventQueue.Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<AuthorizationEvent>(), It.IsAny<CancellationToken>()));
            AuthorizationEvent expectedAuthorizationEvent = TestSetupUtil.GetAuthorizationEvent(testCase);
            HttpClient client = GetTestClient(eventQueue.Object, featureManageMock.Object, timeProviderMock.Object);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
            AssertionUtil.AssertAuthorizationEvent(eventQueue, expectedAuthorizationEvent, Times.Once());
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_RequestConsent_ValidAccessList()
        {
            string testCase = "AltinnResourceRegistry_RequestConsent_ValidAccessList";

            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_RequestConsent_ValidAccessList_Ver2()
        {
            string testCase = "AltinnResourceRegistry_RequestConsent_ValidAccessList_Ver2";

            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_RequestConsent_InValidAccessList()
        {
            string testCase = "AltinnResourceRegistry_RequestConsent_InValidAccessList";

            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// This scenario uses a resource that only requires that it is an organization that is requesting consent.
        /// 
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_RequestConsent_ValidPartyType()
        {
            string testCase = "AltinnResourceRegistry_RequestConsent_ValidPartyType";

            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// This scenario uses a resource that only requires that it is an organization that is requesting consent.
        /// 
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_RequestConsent_SKE_Skattegrunnlag_ValidPartyType()
        {
            string testCase = "AltinnResourceRegistry_RequestConsent_ValidPartyType_Ver3";

            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// This scenario uses a resource that only requires that it is an organization that is requesting consent.
        /// 
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_RequestConsent_ValidPartyType_Ver2()
        {
            string testCase = "AltinnResourceRegistry_RequestConsent_ValidPartyType_Ver2";

            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where subject is a system user with resource delegation giving access the resource.
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_SystemUserWithDelegation_ReturnsPermit()
        {
            string testCase = "ResourceRegistry_SystemUserWithDelegation_Permit";
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where the subject is a system user that has no client-delegation for the
        /// resource, so the decision is NotApplicable. Mirrors the Bruno
        /// SysUser_ClientDelg_AccPkg_NoDelg_NotApplicable negative boundary (#3498 area 4).
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_SystemUserWithoutDelegation_ReturnsNotApplicable()
        {
            string testCase = "ResourceRegistry_SystemUserWithoutDelegation_NotApplicable";
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(_client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where the subject holds no access package granting the access-package resource,
        /// so the decision is NotApplicable. The negative counterpart of the WithAccessPackage Permit case and
        /// the access-package equivalent of the no-delegation boundary (#3498 area 3).
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_SubjectWithoutAccessPackage_ReturnsNotApplicable()
        {
            string testCase = "ResourceRegistry_SubjectWithoutAccessPackage_NotApplicable";
            HttpClient client = GetTestClient(featureManager: featureManageMock.Object);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where the subject holds the access package but requests an action the policy
        /// does not grant for it (the package grants read/write, the request asks for sign), so the decision
        /// is NotApplicable. Guards the action-scoping boundary of an access package (#3498 area 3).
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_AccessPackageUngrantedAction_ReturnsNotApplicable()
        {
            string testCase = "ResourceRegistry_AccessPackageUngrantedAction_NotApplicable";
            HttpClient client = GetTestClient(featureManager: featureManageMock.Object);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where the subject is a person (party uuid) that reaches the resource through an
        /// access package held on behalf of the reportee, so the decision is Permit. Mirrors the Bruno
        /// person-via-access-package Permit contract (#3498 area 3); the C# decision suite previously covered
        /// this path only for system users.
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_PersonWithAccessPackage_ReturnsPermit()
        {
            string testCase = "ResourceRegistry_PersonWithAccessPackage_Permit";
            HttpClient client = GetTestClient(featureManager: featureManageMock.Object);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where subject is a system user with access package giving access the resource. Request is a multi request testing all possible resource party identifiers.
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_SystemUser_WithAccessPackage_MultiRequest_ReturnsPermit()
        {
            string testCase = "ResourceRegistry_SystemUser_WithAccessPackage_MultiRequest_Permit";
            HttpClient client = GetTestClient(featureManager: featureManageMock.Object);
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

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

        private void SetupFeatureMock(string feature, bool featureFlag)
        {
            featureManageMock
                .Setup(m => m.IsEnabledAsync(feature))
                .Returns(Task.FromResult(featureFlag));
        }

        private void SetupDateTimeMock()
        {
            timeProviderMock.Setup(x => x.GetUtcNow()).Returns(new DateTimeOffset(2018, 05, 15, 02, 05, 00, TimeSpan.Zero));
        }
    }
}
