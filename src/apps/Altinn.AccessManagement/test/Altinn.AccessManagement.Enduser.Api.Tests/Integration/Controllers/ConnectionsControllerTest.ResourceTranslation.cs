using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Tests that the resources returned by the connections read endpoints follow Accept-Language.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests that <see cref="ConnectionsController.GetConnections"/>, <see cref="ConnectionsController.GetResources"/>,
    /// <see cref="ConnectionsController.GetInstances"/>, <see cref="ConnectionsController.GetResourceRights"/> and
    /// <see cref="ConnectionsController.GetInstanceRights"/> translate the resource, and the right names, to the
    /// language in the Accept-Language header.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - Default test data: Kaos Magic Design and Arts has delegated Sirius Skattemelding, and one of its instances,
    ///   to Josephine Yvonnesdottir
    /// - English Name, Description and RightDescription for Sirius Skattemelding
    /// - Nynorsk Name only, so Nynorsk Description and RightDescription fall back to bokmål
    /// </para>
    /// <para>
    /// Actors:
    /// - Jinx Arcane: managing director of Kaos Magic Design and Arts (views from Kaos' perspective)
    /// </para>
    /// </remarks>
    [IntegrationTest]
    public class ResourceTranslation : IClassFixture<ApiFixture>
    {
        private const string SiriusRefId = "app_skd_sirius-skattemelding-v1";
        private const string SiriusInstanceId = "urn:altinn:instance-id:50315678/b1a2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d";
        private const string EnglishName = "Tax return with business specification 2020";
        private const string EnglishDescription = "Tax return with business specification, English description";
        private const string EnglishRightDescription = "Gives access to submit the tax return";
        private const string NynorskName = "Skattemelding med næringsspesifikasjon 2020, nynorsk";

        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        public ResourceTranslation(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
            });
            Fixture.EnsureSeedOnce<ResourceTranslation>(db =>
            {
                db.TranslationEntries.AddRange(
                    new TranslationEntry { Id = TestData.SiriusSkattemelding.Id, Type = "Resource", LanguageCode = "eng", FieldName = "Name", Value = EnglishName },
                    new TranslationEntry { Id = TestData.SiriusSkattemelding.Id, Type = "Resource", LanguageCode = "eng", FieldName = "Description", Value = EnglishDescription },
                    new TranslationEntry { Id = TestData.SiriusSkattemelding.Id, Type = "Resource", LanguageCode = "eng", FieldName = "RightDescription", Value = EnglishRightDescription },
                    new TranslationEntry { Id = TestData.SiriusSkattemelding.Id, Type = "Resource", LanguageCode = "nno", FieldName = "Name", Value = NynorskName });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private static string KaosToJosephine => $"party={TestData.KaosMagicDesignAndArts.Id}&from={TestData.KaosMagicDesignAndArts.Id}&to={TestData.JosephineYvonnesdottir.Id}";

        private HttpClient CreateClient(string acceptLanguage)
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestData.JinxArcane.Id.ToString()));
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            client.DefaultRequestHeaders.Add("Accept-Language", acceptLanguage);
            return client;
        }

        private async Task<T> Get<T>(string acceptLanguage, string path)
        {
            HttpResponseMessage response = await CreateClient(acceptLanguage).GetAsync($"{Route}{path}", TestContext.Current.CancellationToken);
            string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {body}");

            T result = JsonSerializer.Deserialize<T>(body, JsonOptions);
            Assert.NotNull(result);
            return result;
        }

        private static ResourceDto Sirius(IEnumerable<ResourceDto> resources)
            => Assert.Single(resources, r => r.RefId == SiriusRefId);

        /// <summary>
        /// Jinx lists Kaos' connections to Josephine with resources and Accept-Language en.
        /// Expects Sirius Skattemelding with its English name.
        /// </summary>
        [Fact]
        public async Task GetConnections_WithEnglishAcceptLanguage_ReturnsTranslatedResources()
        {
            var result = await Get<PaginatedResult<ConnectionDto>>("en", $"?{KaosToJosephine}&includeResources=true");

            var sirius = result.Items.SelectMany(c => c.Resources).Where(r => r.RefId == SiriusRefId).ToList();
            Assert.NotEmpty(sirius);
            Assert.All(sirius, r => Assert.Equal(EnglishName, r.Name));
        }

        /// <summary>
        /// Jinx lists Kaos' connections to Josephine with resources and Accept-Language nn.
        /// Expects Sirius Skattemelding with its Nynorsk name.
        /// </summary>
        [Fact]
        public async Task GetConnections_WithNynorskAcceptLanguage_ReturnsTranslatedResources()
        {
            var result = await Get<PaginatedResult<ConnectionDto>>("nn", $"?{KaosToJosephine}&includeResources=true");

            var sirius = result.Items.SelectMany(c => c.Resources).Where(r => r.RefId == SiriusRefId).ToList();
            Assert.NotEmpty(sirius);
            Assert.All(sirius, r => Assert.Equal(NynorskName, r.Name));
        }

        /// <summary>
        /// Jinx lists the resources Kaos has delegated to Josephine with Accept-Language en.
        /// Expects Sirius Skattemelding with English name, description and right description.
        /// </summary>
        [Fact]
        public async Task GetResources_WithEnglishAcceptLanguage_ReturnsTranslatedResource()
        {
            var result = await Get<List<ResourcePermissionDto>>("en", $"/resources?{KaosToJosephine}");

            ResourceDto sirius = Sirius(result.Select(r => r.Resource));
            Assert.Equal(EnglishName, sirius.Name);
        }

        /// <summary>
        /// Jinx lists the resources Kaos has delegated to Josephine with Accept-Language nn.
        /// Expects the Nynorsk name, and no English text where Nynorsk has no translation.
        /// </summary>
        [Fact]
        public async Task GetResources_WithNynorskAcceptLanguage_ReturnsTranslatedResource()
        {
            var result = await Get<List<ResourcePermissionDto>>("nn", $"/resources?{KaosToJosephine}");

            ResourceDto sirius = Sirius(result.Select(r => r.Resource));
            Assert.Equal(NynorskName, sirius.Name);
            Assert.NotEqual(EnglishDescription, sirius.Description);
        }

        /// <summary>
        /// Jinx lists the resources Kaos has delegated to Josephine with Accept-Language nb.
        /// Expects Sirius Skattemelding with its stored bokmål name.
        /// </summary>
        [Fact]
        public async Task GetResources_WithBokmalAcceptLanguage_ReturnsUntranslatedResource()
        {
            var result = await Get<List<ResourcePermissionDto>>("nb", $"/resources?{KaosToJosephine}");

            ResourceDto sirius = Sirius(result.Select(r => r.Resource));
            Assert.Equal(TestData.SiriusSkattemelding.Name, sirius.Name);
        }

        /// <summary>
        /// Jinx lists the instances Kaos has delegated to Josephine with Accept-Language en and nn.
        /// Expects the Sirius Skattemelding instance's resource name in the requested language.
        /// </summary>
        [Theory]
        [InlineData("en", EnglishName)]
        [InlineData("nn", NynorskName)]
        public async Task GetInstances_WithAcceptLanguage_ReturnsTranslatedResource(string acceptLanguage, string expectedName)
        {
            var result = await Get<PaginatedResult<InstancePermissionDto>>(acceptLanguage, $"/resources/instances?{KaosToJosephine}");

            InstancePermissionDto instance = Assert.Single(result.Items, i => i.Instance.RefId == SiriusInstanceId);
            Assert.Equal(expectedName, instance.Resource.Name);
        }

        /// <summary>
        /// Jinx reads the rights Kaos has delegated to Josephine on Sirius Skattemelding with Accept-Language en.
        /// Expects the resource in English and English right names.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_WithEnglishAcceptLanguage_ReturnsTranslatedResourceAndRights()
        {
            var result = await Get<ExternalResourceRightDto>("en", $"/resources/rights?{KaosToJosephine}&resource={SiriusRefId}");

            Assert.Equal(EnglishName, result.Resource.Name);
            Assert.Equal(EnglishDescription, result.Resource.Description);
            Assert.Equal(EnglishRightDescription, result.Resource.RightDescription);

            var rightNames = result.DirectRights.Concat(result.IndirectRights).Select(r => r.Right.Name).ToList();
            Assert.NotEmpty(rightNames);
            Assert.Contains(rightNames, n => n != null && (n.Contains("Read") || n.Contains("Write")));
            Assert.DoesNotContain(rightNames, n => n != null && (n.Contains("Les") || n.Contains("Skriv")));
        }

        /// <summary>
        /// Jinx reads the rights Kaos has delegated to Josephine on Sirius Skattemelding with Accept-Language nn.
        /// Expects the Nynorsk resource name, Norwegian right names and a bokmål description as fallback.
        /// </summary>
        [Fact]
        public async Task GetResourceRights_WithNynorskAcceptLanguage_ReturnsTranslatedResourceAndRights()
        {
            var result = await Get<ExternalResourceRightDto>("nn", $"/resources/rights?{KaosToJosephine}&resource={SiriusRefId}");

            Assert.Equal(NynorskName, result.Resource.Name);
            Assert.Equal(TestData.SiriusSkattemelding.Description, result.Resource.Description);

            var rightNames = result.DirectRights.Concat(result.IndirectRights).Select(r => r.Right.Name).ToList();
            Assert.NotEmpty(rightNames);
            Assert.Contains(rightNames, n => n != null && (n.Contains("Les") || n.Contains("Skriv")));
        }

        /// <summary>
        /// Jinx reads the instance rights Kaos has delegated to Josephine with Accept-Language en.
        /// Expects the resource in English and English right names.
        /// </summary>
        [Fact]
        public async Task GetInstanceRights_WithEnglishAcceptLanguage_ReturnsTranslatedResourceAndRights()
        {
            var result = await Get<ExtInstanceRightDto>("en", $"/resources/instances/rights?{KaosToJosephine}&resource={SiriusRefId}&instance={SiriusInstanceId}");

            Assert.Equal(EnglishName, result.Resource.Name);
            Assert.Equal(EnglishRightDescription, result.Resource.RightDescription);

            var rightNames = result.DirectRights.Concat(result.IndirectRights).Select(r => r.Right.Name).ToList();
            Assert.NotEmpty(rightNames);
            Assert.Contains(rightNames, n => n != null && (n.Contains("Read") || n.Contains("Write")));
            Assert.DoesNotContain(rightNames, n => n != null && (n.Contains("Les") || n.Contains("Skriv")));
        }

        /// <summary>
        /// Jinx reads the instance rights Kaos has delegated to Josephine with Accept-Language nn.
        /// Expects the Nynorsk resource name.
        /// </summary>
        [Fact]
        public async Task GetInstanceRights_WithNynorskAcceptLanguage_ReturnsTranslatedResource()
        {
            var result = await Get<ExtInstanceRightDto>("nn", $"/resources/instances/rights?{KaosToJosephine}&resource={SiriusRefId}&instance={SiriusInstanceId}");

            Assert.Equal(NynorskName, result.Resource.Name);
        }
    }
}
