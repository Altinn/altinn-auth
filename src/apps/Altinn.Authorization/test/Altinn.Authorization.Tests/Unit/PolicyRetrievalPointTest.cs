using System.Text;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Tests.MockServices;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Repositories.Interface;
using Altinn.Platform.Authorization.Services.Implementation;
using Altinn.Platform.Authorization.Services.Interface;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.Authorization.Tests.Unit
{
    [Collection(PolicyTestCollection.Name)]
    [UnitTest]
    public class PolicyRetrievalPointTest
    {
        private const string ORG = "ttd";
        private const string APP = "repository-test-app";
        private const string POLICYPATH = "ttd/repository-test-app/policy.xml";

        private readonly IPolicyRetrievalPoint _prp;

        public PolicyRetrievalPointTest()
        {
            ServiceCollection services = new ServiceCollection();
            services.AddMemoryCache();
            ServiceProvider serviceProvider = services.BuildServiceProvider();

            IMemoryCache memoryCache = serviceProvider.GetService<IMemoryCache>();

            _prp = new PolicyRetrievalPoint(
                new PolicyRepositoryMock(new Mock<ILogger<PolicyRepositoryMock>>().Object),
                memoryCache,
                Options.Create(new GeneralSettings { PolicyCacheTimeout = 1 }),
                new ResourceRegistryMock());
        }

        /// <summary>
        /// Test case: Get file from storage.
        /// Expected: GetPolicyAsync returns a file that is not null.
        /// </summary>
        [Fact]
        public async Task GetPolicy_ByRequestWithOrgAndApp_ReturnsPolicy()
        {
            // Arrange
            XacmlContextRequest request = new XacmlContextRequest(true, true, GetXacmlContextAttributesWithOrgAndApp());

            // Act
            XacmlPolicy xacmlPolicy = await _prp.GetPolicyAsync(request);

            // Assert
            Assert.NotNull(xacmlPolicy);
        }

        /// <summary>
        /// Test case: Get a file from storage that does not exists.
        /// Expected: GetPolicyAsync returns null.
        /// </summary>
        [Fact]
        public async Task GetPolicy_ByRequestWhenPolicyNotExists_ReturnsNull()
        {
            // Arrange
            XacmlContextRequest request = new XacmlContextRequest(true, true, GetXacmlContextAttributesWithOrgAndApp(false));

            // Act
            XacmlPolicy xacmlPolicy = await _prp.GetPolicyAsync(request);

            // Assert
            Assert.Null(xacmlPolicy);
        }

        /// <summary>
        /// Test case: Get a file from storage with a request that does not contain information about org and app. 
        /// Expected: GetPolicyAsync throws ArgumentException.
        /// </summary>
        [Fact]
        public async Task GetPolicy_ByRequestWithoutOrgAndApp_ThrowsArgumentException()
        {
            // Arrange
            XacmlContextRequest request = new XacmlContextRequest(true, true, new List<XacmlContextAttributes>());

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _prp.GetPolicyAsync(request));
        }

        /// <summary>
        /// Test case: Get file from storage.
        /// Expected: GetPolicyAsync returns a file that is not null.
        /// </summary>
        [Fact]
        public async Task GetPolicy_ByRequestWithResourceId_ReturnsPolicy()
        {
            // Arrange
            XacmlContextRequest request = new XacmlContextRequest(true, true, GetXacmlContextAttributesWithResourceId("apidelegation"));

            // Act
            XacmlPolicy xacmlPolicy = await _prp.GetPolicyAsync(request);

            // Assert
            Assert.NotNull(xacmlPolicy);
        }

        /// <summary>
        /// Test case: Get file from storage.
        /// Expected: GetPolicyAsync returns a file that is not null.
        /// </summary>
        [Fact]
        public async Task GetPolicy_ByOrgApp_ReturnsPolicy()
        {
            // Arrange
            string org = "ttd";
            string app = "repository-test-app";

            // Act
            XacmlPolicy xacmlPolicy = await _prp.GetPolicyAsync(org, app);

            // Assert
            Assert.NotNull(xacmlPolicy);
        }

        /// <summary>
        /// Test case: Get a file from storage that does not exists.
        /// Expected: GetPolicyAsync returns null.
        /// </summary>
        [Fact]
        public async Task GetPolicy_ByOrgApp_NullWhenPolicyNotExists()
        {
            // Arrange
            string org = "1";
            string app = "2";

            // Act
            XacmlPolicy xacmlPolicy = await _prp.GetPolicyAsync(org, app);

            // Assert
            Assert.Null(xacmlPolicy);
        }

        /// <summary>
        /// Test case: Get a file from storage with a request that does not contain information about app. 
        /// Expected: GetPolicyAsync throws ArgumentException.
        /// </summary>
        [Fact]
        public async Task GetPolicy_ByOrgApp_ThrowsException()
        {
            // Arrange
            string org = "ttd";
            string app = string.Empty;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _prp.GetPolicyAsync(org, app));
        }

        /// <summary>
        /// Test case: Get the policy for the same org and app twice.
        /// Expected: Both calls return the policy and the repository is read once.
        /// </summary>
        [Fact]
        public async Task GetPolicyAsync_SameOrgAppRequestedTwice_ReturnsPolicyAndReadsRepositoryOnce()
        {
            // Arrange
            (PolicyRetrievalPoint prp, Mock<IPolicyRepository> repository) = CreatePolicyRetrievalPointWithMockedRepository();
            repository
                .Setup(r => r.GetPolicyAsync(POLICYPATH, It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(GetPolicyStream("current")));

            // Act
            XacmlPolicy first = await prp.GetPolicyAsync(ORG, APP);
            XacmlPolicy second = await prp.GetPolicyAsync(ORG, APP);

            // Assert
            first.PolicyId.ToString().Should().Be("urn:altinn:policyid:current");
            second.PolicyId.ToString().Should().Be("urn:altinn:policyid:current");
            repository.Verify(r => r.GetPolicyAsync(POLICYPATH, It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Test case: Get the same policy version twice.
        /// Expected: Both calls return the version and the repository is read once.
        /// </summary>
        [Fact]
        public async Task GetPolicyVersionAsync_SameVersionRequestedTwice_ReturnsPolicyAndReadsRepositoryOnce()
        {
            // Arrange
            (PolicyRetrievalPoint prp, Mock<IPolicyRepository> repository) = CreatePolicyRetrievalPointWithMockedRepository();
            repository
                .Setup(r => r.GetPolicyVersionAsync(POLICYPATH, "v1", It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(GetPolicyStream("v1")));

            // Act
            XacmlPolicy first = await prp.GetPolicyVersionAsync(POLICYPATH, "v1", TestContext.Current.CancellationToken);
            XacmlPolicy second = await prp.GetPolicyVersionAsync(POLICYPATH, "v1", TestContext.Current.CancellationToken);

            // Assert
            first.PolicyId.ToString().Should().Be("urn:altinn:policyid:v1");
            second.PolicyId.ToString().Should().Be("urn:altinn:policyid:v1");
            repository.Verify(r => r.GetPolicyVersionAsync(POLICYPATH, "v1", It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Test case: Get two different versions of the same policy, then the first version again.
        /// Expected: Each version is returned as stored and each is read from the repository once.
        /// </summary>
        [Fact]
        public async Task GetPolicyVersionAsync_DifferentVersionsOfSamePolicy_ReturnsEachVersionAndCachesThemSeparately()
        {
            // Arrange
            (PolicyRetrievalPoint prp, Mock<IPolicyRepository> repository) = CreatePolicyRetrievalPointWithMockedRepository();
            repository
                .Setup(r => r.GetPolicyVersionAsync(POLICYPATH, "v1", It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(GetPolicyStream("v1")));
            repository
                .Setup(r => r.GetPolicyVersionAsync(POLICYPATH, "v2", It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(GetPolicyStream("v2")));

            // Act
            XacmlPolicy v1 = await prp.GetPolicyVersionAsync(POLICYPATH, "v1", TestContext.Current.CancellationToken);
            XacmlPolicy v2 = await prp.GetPolicyVersionAsync(POLICYPATH, "v2", TestContext.Current.CancellationToken);
            XacmlPolicy v1Again = await prp.GetPolicyVersionAsync(POLICYPATH, "v1", TestContext.Current.CancellationToken);

            // Assert
            v1.PolicyId.ToString().Should().Be("urn:altinn:policyid:v1");
            v2.PolicyId.ToString().Should().Be("urn:altinn:policyid:v2");
            v1Again.PolicyId.ToString().Should().Be("urn:altinn:policyid:v1");
            repository.Verify(r => r.GetPolicyVersionAsync(POLICYPATH, "v1", It.IsAny<CancellationToken>()), Times.Once);
            repository.Verify(r => r.GetPolicyVersionAsync(POLICYPATH, "v2", It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Test case: Clear the rules on a returned policy and then read the same version again.
        /// Expected: The second read returns a separate policy with the original rules, still without reading the repository again.
        /// </summary>
        [Fact]
        public async Task GetPolicyVersionAsync_CallerClearedRulesOnPreviousResult_ReturnsPolicyWithOriginalRules()
        {
            // Arrange
            (PolicyRetrievalPoint prp, Mock<IPolicyRepository> repository) = CreatePolicyRetrievalPointWithMockedRepository();
            repository
                .Setup(r => r.GetPolicyVersionAsync(POLICYPATH, "v1", It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(GetPolicyStream("v1")));

            // Act
            XacmlPolicy first = await prp.GetPolicyVersionAsync(POLICYPATH, "v1", TestContext.Current.CancellationToken);
            first.Rules.Should().ContainSingle();
            first.Rules.Clear();

            XacmlPolicy second = await prp.GetPolicyVersionAsync(POLICYPATH, "v1", TestContext.Current.CancellationToken);

            // Assert
            second.Should().NotBeSameAs(first);
            second.Rules.Should().ContainSingle().Which.RuleId.Should().Be("urn:altinn:ruleid:v1");
            repository.Verify(r => r.GetPolicyVersionAsync(POLICYPATH, "v1", It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Test case: Get a policy version that does not exist, twice.
        /// Expected: Both calls return null and the repository is read both times.
        /// </summary>
        [Fact]
        public async Task GetPolicyVersionAsync_VersionMissingThenRequestedAgain_ReturnsNullAndReadsRepositoryAgain()
        {
            // Arrange
            (PolicyRetrievalPoint prp, Mock<IPolicyRepository> repository) = CreatePolicyRetrievalPointWithMockedRepository();
            repository
                .Setup(r => r.GetPolicyVersionAsync(POLICYPATH, "v9", It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Stream>(new MemoryStream()));

            // Act
            XacmlPolicy first = await prp.GetPolicyVersionAsync(POLICYPATH, "v9", TestContext.Current.CancellationToken);
            XacmlPolicy second = await prp.GetPolicyVersionAsync(POLICYPATH, "v9", TestContext.Current.CancellationToken);

            // Assert
            first.Should().BeNull();
            second.Should().BeNull();
            repository.Verify(r => r.GetPolicyVersionAsync(POLICYPATH, "v9", It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        /// <summary>
        /// Test case: Get a policy version whose document is not a valid policy, twice.
        /// Expected: Both calls throw and the repository is read both times.
        /// </summary>
        [Fact]
        public async Task GetPolicyVersionAsync_DocumentFailsToParse_ThrowsAndReadsRepositoryAgain()
        {
            // Arrange
            (PolicyRetrievalPoint prp, Mock<IPolicyRepository> repository) = CreatePolicyRetrievalPointWithMockedRepository();
            repository
                .Setup(r => r.GetPolicyVersionAsync(POLICYPATH, "v8", It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("<not-a-policy>"))));

            // Act
            Func<Task> first = () => prp.GetPolicyVersionAsync(POLICYPATH, "v8", TestContext.Current.CancellationToken);
            Func<Task> second = () => prp.GetPolicyVersionAsync(POLICYPATH, "v8", TestContext.Current.CancellationToken);

            // Assert
            await first.Should().ThrowAsync<Exception>();
            await second.Should().ThrowAsync<Exception>();
            repository.Verify(r => r.GetPolicyVersionAsync(POLICYPATH, "v8", It.IsAny<CancellationToken>()), Times.Exactly(2));
        }

        private static (PolicyRetrievalPoint Prp, Mock<IPolicyRepository> Repository) CreatePolicyRetrievalPointWithMockedRepository()
        {
            Mock<IPolicyRepository> repository = new Mock<IPolicyRepository>(MockBehavior.Strict);

            PolicyRetrievalPoint prp = new PolicyRetrievalPoint(
                repository.Object,
                new MemoryCache(new MemoryCacheOptions()),
                Options.Create(new GeneralSettings { PolicyCacheTimeout = 1 }),
                new ResourceRegistryMock());

            return (prp, repository);
        }

        private static Stream GetPolicyStream(string id) =>
            new MemoryStream(Encoding.UTF8.GetBytes($"""
                <?xml version="1.0" encoding="utf-8"?>
                <xacml:Policy xmlns:xacml="urn:oasis:names:tc:xacml:3.0:core:schema:wd-17" PolicyId="urn:altinn:policyid:{id}" Version="1.0" RuleCombiningAlgId="urn:oasis:names:tc:xacml:3.0:rule-combining-algorithm:deny-overrides">
                  <xacml:Target />
                  <xacml:Rule RuleId="urn:altinn:ruleid:{id}" Effect="Permit">
                    <xacml:Target />
                  </xacml:Rule>
                </xacml:Policy>
                """));

        private List<XacmlContextAttributes> GetXacmlContextAttributesWithOrgAndApp(bool existingApp = true)
        {
            List<XacmlContextAttributes> xacmlContexts = new List<XacmlContextAttributes>();

            XacmlContextAttributes xacmlContext = new XacmlContextAttributes(new Uri(XacmlConstants.MatchAttributeCategory.Resource));

            XacmlAttribute xacmlAttributeOrg = new XacmlAttribute(new Uri("urn:altinn:org"), true);
            xacmlAttributeOrg.AttributeValues.Add(new XacmlAttributeValue(new Uri("urn:altinn:org"), ORG));
            xacmlContext.Attributes.Add(xacmlAttributeOrg);

            xacmlContexts.Add(xacmlContext);

            XacmlContextAttributes xacmlContext2 = new XacmlContextAttributes(new Uri(XacmlConstants.MatchAttributeCategory.Resource));

            XacmlAttribute xacmlAttributeApp = new XacmlAttribute(new Uri("urn:altinn:app"), true);
            if (existingApp)
            {
                xacmlAttributeApp.AttributeValues.Add(new XacmlAttributeValue(new Uri("urn:altinn:app"), APP));
            }
            else
            {
                xacmlAttributeApp.AttributeValues.Add(new XacmlAttributeValue(new Uri("urn:altinn:app"), "dummy-app"));
            }

            xacmlContext2.Attributes.Add(xacmlAttributeApp);

            xacmlContexts.Add(xacmlContext2);

            return xacmlContexts;
        }

        private static List<XacmlContextAttributes> GetXacmlContextAttributesWithResourceId(string resourceId)
        {
            List<XacmlContextAttributes> xacmlContexts = new List<XacmlContextAttributes>();

            XacmlContextAttributes xacmlContext = new XacmlContextAttributes(new Uri(XacmlConstants.MatchAttributeCategory.Resource));

            XacmlAttribute xacmlAttributeOrg = new XacmlAttribute(new Uri("urn:altinn:resource"), true);
            xacmlAttributeOrg.AttributeValues.Add(new XacmlAttributeValue(new Uri("urn:altinn:resource"), resourceId));
            xacmlContext.Attributes.Add(xacmlAttributeOrg);

            xacmlContexts.Add(xacmlContext);

            return xacmlContexts;
        }
    }
}
