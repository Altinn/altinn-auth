using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
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
/// Tests that the resource in delegation-check responses is translated to the requested language.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Tests that <see cref="ConnectionsController.CheckResource(Guid, string, CancellationToken)"/> and
    /// <see cref="ConnectionsController.CheckInstance(Guid, string, string, CancellationToken)"/>
    /// translate the returned resource according to the Accept-Language header.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Seed Data:
    /// - English translations of Name, Description and RightDescription for Sirius Skattemelding
    /// - No Nynorsk translations, so Nynorsk requests fall back to Bokmål
    /// </para>
    /// <para>
    /// Actors:
    /// - Malin Emilie: managing director of Dumbo Adventures (can delegate on behalf of Dumbo)
    /// </para>
    /// </remarks>
    [IntegrationTest]
    public class DelegationCheckTranslation : IClassFixture<ApiFixture>
    {
        private const string SiriusInstanceIdForCheck = "urn:altinn:instance-id:50083510/a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5f";
        private const string EnglishName = "Tax return with business specification 2020";
        private const string EnglishDescription = "Tax return with business specification, English description";
        private const string EnglishRightDescription = "Gives access to submit the tax return";

        public DelegationCheckTranslation(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.ConfigureServices(services =>
            {
                services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
            });
            Fixture.EnsureSeedOnce<DelegationCheckTranslation>(db =>
            {
                db.TranslationEntries.AddRange(
                    new TranslationEntry { Id = TestData.SiriusSkattemelding.Id, Type = "Resource", LanguageCode = "eng", FieldName = "Name", Value = EnglishName },
                    new TranslationEntry { Id = TestData.SiriusSkattemelding.Id, Type = "Resource", LanguageCode = "eng", FieldName = "Description", Value = EnglishDescription },
                    new TranslationEntry { Id = TestData.SiriusSkattemelding.Id, Type = "Resource", LanguageCode = "eng", FieldName = "RightDescription", Value = EnglishRightDescription });
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient(Guid partyUuid, string acceptLanguage, params string[] scopes)
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, partyUuid.ToString()));
                claims.Add(new Claim("scope", string.Join(" ", scopes)));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            client.DefaultRequestHeaders.Add("Accept-Language", acceptLanguage);
            return client;
        }

        /// <summary>
        /// Malin checks Sirius Skattemelding with Accept-Language en.
        /// Expects the resource's Name, Description and RightDescription in English.
        /// </summary>
        [Fact]
        public async Task CheckResource_WithEnglishAcceptLanguage_ReturnsTranslatedResource()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, "en", AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result?.Resource);
            Assert.Equal(EnglishName, result.Resource.Name);
            Assert.Equal(EnglishDescription, result.Resource.Description);
            Assert.Equal(EnglishRightDescription, result.Resource.RightDescription);
        }

        /// <summary>
        /// Malin checks Sirius Skattemelding with Accept-Language nb.
        /// Expects the resource's Bokmål Name as stored.
        /// </summary>
        [Fact]
        public async Task CheckResource_WithBokmalAcceptLanguage_ReturnsUntranslatedResource()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, "nb", AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result?.Resource);
            Assert.Equal(TestData.SiriusSkattemelding.Name, result.Resource.Name);
            Assert.Equal(TestData.SiriusSkattemelding.Description, result.Resource.Description);
            Assert.Equal(TestData.SiriusSkattemelding.RightDescription, result.Resource.RightDescription);
        }

        /// <summary>
        /// Malin checks Sirius Skattemelding with Accept-Language nn, for which no translation is stored.
        /// Expects the resource to fall back to its Bokmål Name and Description.
        /// </summary>
        [Fact]
        public async Task CheckResource_WithNynorskAcceptLanguageAndNoTranslation_FallsBackToBokmal()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, "nn", AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/delegationcheck?party={TestData.DumboAdventures.Id}&resource=app_skd_sirius-skattemelding-v1",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            ResourceCheckDto result = JsonSerializer.Deserialize<ResourceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result?.Resource);
            Assert.Equal(TestData.SiriusSkattemelding.Name, result.Resource.Name);
            Assert.Equal(TestData.SiriusSkattemelding.Description, result.Resource.Description);
            Assert.Equal(TestData.SiriusSkattemelding.RightDescription, result.Resource.RightDescription);
        }

        /// <summary>
        /// Malin checks a Sirius Skattemelding instance with Accept-Language en.
        /// Expects the resource's Name, Description and RightDescription in English.
        /// </summary>
        [Fact]
        public async Task CheckInstance_WithEnglishAcceptLanguage_ReturnsTranslatedResource()
        {
            HttpClient client = CreateClient(TestData.MalinEmilie.Id, "en", AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_WRITE);

            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/resources/instances/delegationcheck?party={TestData.DumboAdventures.Id}&resource=app_skd_sirius-skattemelding-v1&instance={SiriusInstanceIdForCheck}",
                TestContext.Current.CancellationToken);

            string responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {responseContent}");

            InstanceCheckDto result = JsonSerializer.Deserialize<InstanceCheckDto>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result?.Resource);
            Assert.Equal(EnglishName, result.Resource.Name);
            Assert.Equal(EnglishDescription, result.Resource.Description);
            Assert.Equal(EnglishRightDescription, result.Resource.RightDescription);
        }
    }
}
