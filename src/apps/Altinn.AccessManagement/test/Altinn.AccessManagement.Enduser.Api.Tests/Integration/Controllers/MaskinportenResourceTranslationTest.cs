using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Integration tests that the Maskinporten consumers and suppliers resource lists, v1 and v2,
/// translate the resource to the language in the Accept-Language header.
/// </summary>
/// <remarks>
/// Seed data: Baker Johnsen (consumer) has delegated one MaskinportenSchema resource to
/// Han Solo Enterprise (supplier). The resource has an English name, description and right
/// description, and a Nynorsk name only, so the Nynorsk description falls back to bokmål.
/// </remarks>
[IntegrationTest]
public class MaskinportenResourceTranslationTest : IClassFixture<ApiFixture>
{
    private const string ResourceRefId = "mp_test_4913_translated_resource";
    private const string BokmalName = "Maskinporten testressurs";
    private const string BokmalDescription = "Maskinporten testressurs, beskrivelse på bokmål";
    private const string BokmalRightDescription = "Gir tilgang til test-API-et";
    private const string EnglishName = "Maskinporten test resource";
    private const string EnglishDescription = "Maskinporten test resource, English description";
    private const string EnglishRightDescription = "Gives access to the test API";
    private const string NynorskName = "Maskinporten testressurs, nynorsk";

    private static readonly Guid ResourceId = Guid.Parse("01984913-0000-7000-8000-000000000001");
    private static readonly Guid Supplier = TestData.HanSoloEnterprise.Id;
    private static readonly Guid Consumer = TestData.BakerJohnsen.Id;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MaskinportenResourceTranslationTest(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableEnduserMaskinportenAdminApi);
        Fixture.EnsureSeedOnce<MaskinportenResourceTranslationTest>(db =>
        {
            var maskinportenType = db.ResourceTypes.FirstOrDefault(t => t.Name == "MaskinportenSchema");
            if (maskinportenType is null)
            {
                maskinportenType = new ResourceType { Id = Guid.CreateVersion7(), Name = "MaskinportenSchema" };
                db.ResourceTypes.Add(maskinportenType);
                db.SaveChanges();
            }

            db.Resources.Add(new Resource
            {
                Id = ResourceId,
                Name = BokmalName,
                Description = BokmalDescription,
                RightDescription = BokmalRightDescription,
                RefId = ResourceRefId,
                TypeId = maskinportenType.Id,
                ProviderId = ProviderConstants.Altinn3.Id,
            });

            var supplierFromConsumer = new Assignment { FromId = Consumer, ToId = Supplier, RoleId = RoleConstants.Supplier.Id };
            db.Assignments.Add(supplierFromConsumer);
            db.SaveChanges();

            db.AssignmentResources.Add(new AssignmentResource { AssignmentId = supplierFromConsumer.Id, ResourceId = ResourceId });
            db.TranslationEntries.AddRange(
                new TranslationEntry { Id = ResourceId, Type = "Resource", LanguageCode = "eng", FieldName = "Name", Value = EnglishName },
                new TranslationEntry { Id = ResourceId, Type = "Resource", LanguageCode = "eng", FieldName = "Description", Value = EnglishDescription },
                new TranslationEntry { Id = ResourceId, Type = "Resource", LanguageCode = "eng", FieldName = "RightDescription", Value = EnglishRightDescription },
                new TranslationEntry { Id = ResourceId, Type = "Resource", LanguageCode = "nno", FieldName = "Name", Value = NynorskName });
            db.SaveChanges();
        });
    }

    public ApiFixture Fixture { get; }

    public static TheoryData<string, string> Routes => new()
    {
        { $"accessmanagement/api/v1/enduser/maskinportenconsumers/resources?party={Supplier}", "consumers" },
        { $"accessmanagement/api/v2/enduser/maskinporten/consumers/resources?party={Supplier}", "consumers" },
        { $"accessmanagement/api/v1/enduser/maskinportensuppliers/resources?party={Consumer}", "suppliers" },
        { $"accessmanagement/api/v2/enduser/maskinporten/suppliers/resources?party={Consumer}", "suppliers" },
    };

    private HttpClient CreateClient(string side, string acceptLanguage)
    {
        var client = Fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, (side == "consumers" ? Supplier : Consumer).ToString()));
            claims.Add(new Claim("scope", side == "consumers" ? AuthzConstants.SCOPE_ENDUSER_MASKINPORTENCONSUMERS_READ : AuthzConstants.SCOPE_ENDUSER_MASKINPORTENSUPPLIERS_READ));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        client.DefaultRequestHeaders.Add("Accept-Language", acceptLanguage);
        return client;
    }

    private async Task<ResourceDto> GetResource(string route, string side, string acceptLanguage)
    {
        HttpResponseMessage response = await CreateClient(side, acceptLanguage).GetAsync(route, TestContext.Current.CancellationToken);
        string body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {body}");

        List<ResourcePermissionDto> result = JsonSerializer.Deserialize<List<ResourcePermissionDto>>(body, JsonOptions);
        Assert.NotNull(result);
        return Assert.Single(result, r => r.Resource.RefId == ResourceRefId).Resource;
    }

    /// <summary>
    /// Lists the resources with Accept-Language en.
    /// Expects the English name, description and right description.
    /// </summary>
    [Theory]
    [MemberData(nameof(Routes))]
    public async Task GetResources_WithEnglishAcceptLanguage_ReturnsTranslatedResource(string route, string side)
    {
        ResourceDto resource = await GetResource(route, side, "en");

        Assert.Equal(EnglishName, resource.Name);
        Assert.Equal(EnglishDescription, resource.Description);
        Assert.Equal(EnglishRightDescription, resource.RightDescription);
    }

    /// <summary>
    /// Lists the resources with Accept-Language nn.
    /// Expects the Nynorsk name and the bokmål description, which has no Nynorsk translation.
    /// </summary>
    [Theory]
    [MemberData(nameof(Routes))]
    public async Task GetResources_WithNynorskAcceptLanguage_ReturnsTranslatedNameAndBokmalFallback(string route, string side)
    {
        ResourceDto resource = await GetResource(route, side, "nn");

        Assert.Equal(NynorskName, resource.Name);
        Assert.Equal(BokmalDescription, resource.Description);
    }

    /// <summary>
    /// Lists the resources with Accept-Language nb.
    /// Expects the resource as stored, in bokmål.
    /// </summary>
    [Theory]
    [MemberData(nameof(Routes))]
    public async Task GetResources_WithBokmalAcceptLanguage_ReturnsUntranslatedResource(string route, string side)
    {
        ResourceDto resource = await GetResource(route, side, "nb");

        Assert.Equal(BokmalName, resource.Name);
        Assert.Equal(BokmalDescription, resource.Description);
    }
}
