using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Platform.Authorization.Clients.Interfaces;
using Altinn.Platform.Authorization.Controllers;
using Altinn.Platform.Authorization.IntegrationTests.MockServices;
using Altinn.Platform.Authorization.IntegrationTests.Util;
using Altinn.Platform.Authorization.IntegrationTests.Webfactory;
using Altinn.Platform.Authorization.Models.EventLog;
using Altinn.Platform.Authorization.Repositories.Interface;
using Altinn.Platform.Authorization.Services.Interface;
using Altinn.Platform.Authorization.Services.Interfaces;
using Altinn.Platform.Events.Tests.Mocks;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Moq;
using Xunit;

namespace Altinn.Platform.Authorization.IntegrationTests
{
    public class ResourceRegistry_DecisionTests :IClassFixture<CustomWebApplicationFactory<DecisionController>>
    {
        private readonly CustomWebApplicationFactory<DecisionController> _factory;
        private readonly Mock<IFeatureManager> featureManageMock = new Mock<IFeatureManager>();
        private readonly Mock<TimeProvider> timeProviderMock = new Mock<TimeProvider>();

        public ResourceRegistry_DecisionTests(CustomWebApplicationFactory<DecisionController> fixture)
        {
            _factory = fixture;
            SetupFeatureMock("AuditLog", true);
            SetupFeatureMock("SystemUserAccessPackageAuthorization", true);
            SetupDateTimeMock();
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_OedFormuesfullmakt_Xml_Permit()
        {
            string testCase = "ResourceRegistry_OedFormuesfullmakt_Xml_Permit";
            HttpClient client = GetTestClient();
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequest(testCase);
            XacmlContextResponse expected = TestSetupUtil.ReadExpectedResponse(testCase);

            // Act
            XacmlContextResponse contextResponse = await TestSetupUtil.GetXacmlContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_OedFormuesfullmakt_Json_Permit()
        {
            string testCase = "ResourceRegistry_OedFormuesfullmakt_Json_Permit";
            HttpClient client = GetTestClient();
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_OedFormuesfullmakt_Xml_Indeterminate()
        {
            string testCase = "ResourceRegistry_OedFormuesfullmakt_Xml_Indeterminate";
            HttpClient client = GetTestClient();
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateXacmlRequest(testCase);
            XacmlContextResponse expected = TestSetupUtil.ReadExpectedResponse(testCase);

            // Act
            XacmlContextResponse contextResponse = await TestSetupUtil.GetXacmlContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_OedFormuesfullmakt_Json_Indeterminate()
        {
            string testCase = "ResourceRegistry_OedFormuesfullmakt_Json_Indeterminate";
            HttpClient client = GetTestClient();
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where the reportee organization has access to 'ttd-accesslist-resource' through access list membership without any action filter.
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_AccessListAuthorization_Json_Permit()
        {
            string testCase = "ResourceRegistry_AccessListAuthorization_Json_Permit";
            HttpClient client = GetTestClient();
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where the reportee organization does NOT have access to 'ttd-accesslist-resource' through any access list membership.
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_AccessListAuthorization_Json_Deny()
        {
            string testCase = "ResourceRegistry_AccessListAuthorization_Json_Deny";
            HttpClient client = GetTestClient();
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where the reportee organization has access to 'ttd-accesslist-resource' through access list membership with matching action filter.
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_AccessListAuthorization_Json_PermitWithActionFilterMatch()
        {
            string testCase = "ResourceRegistry_AccessListAuthorization_Json_PermitWithActionFilterMatch";
            HttpClient client = GetTestClient();
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where the reportee organization has access to 'ttd-accesslist-resource' through access list membership but with action filter not matching the request action.
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_AccessListAuthorization_Json_DenyActionFilterNotMatching()
        {
            string testCase = "ResourceRegistry_AccessListAuthorization_Json_DenyActionFilterNotMatching";
            HttpClient client = GetTestClient();
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where the reportee is a person. Currently the access list authorization service only supports organizations.
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_AccessListAuthorization_Json_DenyAccessListDontSupportPerson()
        {
            string testCase = "ResourceRegistry_AccessListAuthorization_Json_DenyAccessListDontSupportPerson";
            HttpClient client = GetTestClient();
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry0001()
        {
            string testCase = "AltinnResourceRegistry0001";
            Mock<IEventsQueueClient> eventQueue = new Mock<IEventsQueueClient>();
            eventQueue.Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<string>(), It.IsAny<CancellationToken>()));
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
        public async Task PDP_Decision_ResourceRegistry0002()
        {
            string testCase = "AltinnResourceRegistry0002";
            HttpClient client = GetTestClient();
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry0003()
        {
            string testCase = "AltinnResourceRegistry0003";
            HttpClient client = GetTestClient();
            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry0004()
        {
            string testCase = "AltinnResourceRegistry0004";
            Mock<IEventsQueueClient> eventQueue = new Mock<IEventsQueueClient>();
            eventQueue.Setup(q => q.EnqueueAuthorizationEvent(It.IsAny<string>(), It.IsAny<CancellationToken>()));
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
            HttpClient client = GetTestClient();

            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_RequestConsent_ValidAccessList_Ver2()
        {
            string testCase = "AltinnResourceRegistry_RequestConsent_ValidAccessList_Ver2";
            HttpClient client = GetTestClient();

            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        [Fact]
        public async Task PDP_Decision_ResourceRegistry_RequestConsent_InValidAccessList()
        {
            string testCase = "AltinnResourceRegistry_RequestConsent_InValidAccessList";
            HttpClient client = GetTestClient();

            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

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
            HttpClient client = GetTestClient();

            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

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
            HttpClient client = GetTestClient();

            HttpRequestMessage httpRequestMessage = TestSetupUtil.CreateJsonProfileXacmlRequest(testCase);
            XacmlJsonResponse expected = TestSetupUtil.ReadExpectedJsonProfileResponse(testCase);

            // Act
            XacmlJsonResponse contextResponse = await TestSetupUtil.GetXacmlJsonProfileContextResponseAsync(client, httpRequestMessage);

            // Assert
            AssertionUtil.AssertEqual(expected, contextResponse);
        }

        /// <summary>
        /// Tests the scenario where subject is a system user with resource delegation giving access the resource.
        /// </summary>
        [Fact]
        public async Task PDP_Decision_ResourceRegistry_SystemUserWithDelegation_Permit()
        {
            string testCase = "ResourceRegistry_SystemUserWithDelegation_Permit";
            HttpClient client = GetTestClient();
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
        public async Task PDP_Decision_ResourceRegistry_SystemUser_WithAccessPackage_MultiRequest_Permit()
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
            HttpClient client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton<IInstanceMetadataRepository, InstanceMetadataRepositoryMock>();
                    services.AddSingleton<IDelegationMetadataRepository, DelegationMetadataRepositoryMock>();
                    services.AddSingleton<IRoles, RolesMock>();
                    services.AddSingleton<IOedRoleAssignmentWrapper, OedRoleAssignmentWrapperMock>();
                    services.AddSingleton<IParties, PartiesMock>();
                    services.AddSingleton<IProfile, ProfileMock>();
                    services.AddSingleton<IPolicyRepository, PolicyRepositoryMock>();
                    services.AddSingleton<IResourceRegistry, ResourceRegistryMock>();
                    services.AddSingleton<IDelegationChangeEventQueue, DelegationChangeEventQueueMock>();
                    services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
                    services.AddSingleton<IRegisterService, RegisterServiceMock>();
                    services.AddSingleton<IAccessManagementWrapper, AccessManagementWrapperMock>();

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
