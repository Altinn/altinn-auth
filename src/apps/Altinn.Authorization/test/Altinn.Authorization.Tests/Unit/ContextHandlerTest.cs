using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Tests.MockServices;
using Altinn.Authorization.Tests.Util;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Services.Implementation;
using Altinn.Platform.Events.Tests.Mocks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Moq;

namespace Altinn.Authorization.Tests.Unit
{
    /// <summary>
    /// Test class for <see cref="ContextHandler"></see>
    /// </summary>
    [UnitTest]
    public class ContextHandlerTest
    {
        private readonly ContextHandler _contextHandler;
        private readonly Mock<IFeatureManager> featureManageMock = new Mock<IFeatureManager>();
        private HttpContext _httpContext = new DefaultHttpContext();

        public ContextHandlerTest()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddMemoryCache();
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            IMemoryCache memoryCache = serviceProvider.GetService<IMemoryCache>();

            Mock<IHttpContextAccessor> httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            httpContextAccessorMock.Setup(h => h.HttpContext).Returns(_httpContext);
            _contextHandler = new ContextHandler(
                new InstanceMetadataRepositoryMock(),
                new RolesMock(),
                new OedRoleAssignmentWrapperMock(),
                new ProfileMock(),
                memoryCache,
                Options.Create(new GeneralSettings { RoleCacheTimeout = 5 }),
                new RegisterServiceMock(),
                new PolicyRetrievalPointMock(memoryCache, httpContextAccessorMock.Object, null),
                new AccessManagementWrapperMock(httpContextAccessorMock.Object, memoryCache),
                featureManageMock.Object,
                new ResourceRegistryMock());
        }

        /// <summary>
        /// Scenario:
        /// Tests if the xacml request is enriched with the required resource, subject attributes
        /// Input:
        /// Instance id, user id, action
        /// Expected Result:
        /// Xacml request is enriched with the missing resource, roles and subject attributes
        /// Success Criteria:
        /// A xacml request populated with the required attributes is returned
        /// </summary>
        [Fact]
        public async Task Enrich_InstanceUserAction_ReturnsRequestWithResourceRolesAndSubjectAttributes()
        {
            // Arrange
            string testCase = "AltinnApps0021";
            _httpContext.Request.Headers.Append("testcase", testCase);

            XacmlContextRequest request = TestSetupUtil.CreateXacmlContextRequest(testCase);
            XacmlContextRequest expectedEnrichedRequest = TestSetupUtil.GetEnrichedRequest(testCase);

            // Act
            XacmlContextRequest enrichedRequest = await _contextHandler.Enrich(request, false, null, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(enrichedRequest);
            Assert.NotNull(expectedEnrichedRequest);
            AssertionUtil.AssertEqual(expectedEnrichedRequest, enrichedRequest);
        }

        /// <summary>
        /// Scenario:
        /// Tests if the xacml request is enriched with the required resource, subject attributes
        /// Input:
        /// Instance id, org, action
        /// Expected Result:
        /// Xacml request is enriched with the missing resource and subject attributes
        /// Success Criteria:
        /// A xacml request populated with the required attributes is returned
        /// </summary>
        [Fact]
        public async Task Enrich_InstanceOrgAction_ReturnsRequestWithResourceAndSubjectAttributes()
        {
            // Arrange
            string testCase = "AltinnApps0022";
            _httpContext.Request.Headers.Append("testcase", testCase);

            XacmlContextRequest request = TestSetupUtil.CreateXacmlContextRequest(testCase);
            XacmlContextRequest expectedEnrichedRequest = TestSetupUtil.GetEnrichedRequest(testCase);

            // Act
            XacmlContextRequest enrichedRequest = await _contextHandler.Enrich(request, false, null, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(enrichedRequest);
            Assert.NotNull(expectedEnrichedRequest);
            AssertionUtil.AssertEqual(expectedEnrichedRequest, enrichedRequest);
        }

        /// <summary>
        /// Scenario:
        /// Tests if the xacml request is enriched with the required resource, subject attributes
        /// Input:
        /// Complete resource attributes
        /// Expected Result:
        /// Xacml request is enriched with the missing role attributes
        /// Success Criteria:
        /// A xacml request populated with the required attributes is returned
        /// </summary>
        [Fact]
        public async Task Enrich_CompleteResourceAttributes_ReturnsRequestWithRoleAttributes()
        {
            // Arrange
            string testCase = "AltinnApps0023";
            _httpContext.Request.Headers.Append("testcase", testCase);

            XacmlContextRequest request = TestSetupUtil.CreateXacmlContextRequest(testCase);
            XacmlContextRequest expectedEnrichedRequest = TestSetupUtil.GetEnrichedRequest(testCase);

            // Act
            XacmlContextRequest enrichedRequest = await _contextHandler.Enrich(request, false, null, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(enrichedRequest);
            Assert.NotNull(expectedEnrichedRequest);
            AssertionUtil.AssertEqual(expectedEnrichedRequest, enrichedRequest);
        }

        /// <summary>
        /// Scenario:
        /// Tests if the xacml request is enriched with the required resource, subject attributes
        /// Input:
        /// org, app, userid, partyid, action
        /// Expected Result:
        /// Xacml request is enriched with the missing role attributes
        /// Success Criteria:
        /// A xacml request populated with the required attributes is returned
        /// </summary>
        [Fact]
        public async Task Enrich_OrgAppUserPartyAction_ReturnsRequestWithRoleAttributes()
        {
            // Arrange
            string testCase = "AltinnApps0024";
            _httpContext.Request.Headers.Append("testcase", testCase);

            XacmlContextRequest request = TestSetupUtil.CreateXacmlContextRequest(testCase);
            XacmlContextRequest expectedEnrichedRequest = TestSetupUtil.GetEnrichedRequest(testCase);

            // Act
            XacmlContextRequest enrichedRequest = await _contextHandler.Enrich(request, false, null, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(enrichedRequest);
            Assert.NotNull(expectedEnrichedRequest);
            AssertionUtil.AssertEqual(expectedEnrichedRequest, enrichedRequest);
        }

        /// <summary>
        /// Scenario:
        /// Tests if the xacml request is enriched with the required resource, subject attributes
        /// Input:
        /// org, app, party id, action
        /// Expected Result:
        /// Xacml request is enriched with the missing role attributes
        /// Success Criteria:
        /// A xacml request populated with the required attributes is returned
        /// </summary>
        [Fact]
        public async Task Enrich_OrgAppPartyAction_ReturnsRequestWithRoleAttributes()
        {
            // Arrange
            string testCase = "AltinnApps0025";
            _httpContext.Request.Headers.Append("testcase", testCase);

            XacmlContextRequest request = TestSetupUtil.CreateXacmlContextRequest(testCase);
            XacmlContextRequest expectedEnrichedRequest = TestSetupUtil.GetEnrichedRequest(testCase);

            // Act
            XacmlContextRequest enrichedRequest = await _contextHandler.Enrich(request, false, null, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(enrichedRequest);
            Assert.NotNull(expectedEnrichedRequest);
            AssertionUtil.AssertEqual(expectedEnrichedRequest, enrichedRequest);
        }

        /// <summary>
        /// Scenario:
        /// Tests if the xacml request is enriched with the required resource, subject attributes
        /// Input:
        /// Instance-id, user-id, party-id
        /// Expected Result:
        /// Xacml request is enriched with the missing attributes
        /// Success Criteria:
        /// A xacml request populated with the required attributes is returned
        /// </summary>
        [Fact]
        public async Task Enrich_InstanceUserParty_ReturnsRequestWithMissingAttributes()
        {
            // Arrange
            string testCase = "AltinnApps0026";
            _httpContext.Request.Headers.Append("testcase", testCase);

            XacmlContextRequest request = TestSetupUtil.CreateXacmlContextRequest(testCase);
            XacmlContextRequest expectedEnrichedRequest = TestSetupUtil.GetEnrichedRequest(testCase);

            // Act
            XacmlContextRequest enrichedRequest = await _contextHandler.Enrich(request, false, null, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(enrichedRequest);
            Assert.NotNull(expectedEnrichedRequest);
            AssertionUtil.AssertEqual(expectedEnrichedRequest, enrichedRequest);
        }
    }
}
