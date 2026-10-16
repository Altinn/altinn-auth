using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Altinn.ResourceRegistry.Controllers;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Constants;
using Altinn.ResourceRegistry.Core.Enums;
using Altinn.ResourceRegistry.Core.Errors;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.ScopePrefixes;
using Altinn.ResourceRegistry.Core.Services;
using Altinn.ResourceRegistry.TestUtils;
using Altinn.ResourceRegistry.Tests.Mocks;
using Altinn.ResourceRegistry.Tests.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.ResourceRegistry.Tests.ScopePrefixes;

/// <summary>
/// POST and PUT of resources with Maskinporten scopes, with
/// <see cref="FeatureFlags.ValidateScopePrefixWithMaskinportenApi"/> turned on. The claim-based
/// check used while the flag is off is covered by <see cref="ResourceControllerTest"/>.
/// </summary>
public class ResourceControllerScopePrefixTests(DbFixture dbFixture, WebApplicationFixture webApplicationFixture)
    : WebApplicationTests(dbFixture, webApplicationFixture)
{
    private const string Skatteetaten = "974761076";
    private const string Digdir = "991825827";

    private const string WriteScope = "altinn:resourceregistry/resource.write";
    private const string AdminScope = "altinn:resourceregistry/resource.admin";

    // Exists in RegisterResourceRepositoryMock, owned by skd
    private const string ExistingResource = "altinn_access_management";
    private const string NewResource = "superdupertjenestene";

    private readonly SamarbeidsportalenClientMock _prefixApi = new();
    private readonly CapturingLoggerProvider _logs = new();

    protected override void ConfigureTestConfiguration(IConfigurationBuilder builder)
    {
        builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            [$"FeatureManagement:{FeatureFlags.ValidateScopePrefixWithMaskinportenApi}"] = "true",
        });
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton<IResourceRegistryRepository, RegisterResourceRepositoryMock>();
        services.AddSingleton<ISamarbeidsportalenClient>(_prefixApi);
        services.AddSingleton<ILoggerProvider>(_logs);

        base.ConfigureTestServices(services);
    }

    private void GivenPrefixes()
        => _prefixApi.Respond(
        [
            new() { Prefix = "skd", OwnerOrgNo = Skatteetaten, Active = true },
            new() { Prefix = "skatteetaten", OwnerOrgNo = Skatteetaten, Active = false },
            new() { Prefix = "altinn", OwnerOrgNo = Digdir, Active = true },
        ]);

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task ServiceOwner_OwnsPrefixes_Succeeds(string method)
    {
        GivenPrefixes();

        var response = await Send(method, Token("skd", Skatteetaten, WriteScope), Skatteetaten, "skd:scope.one", "skd:scope.two");

        Assert.Equal(SuccessStatus(method), response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task ServiceOwner_PrefixOwnedByOtherOrganization_Returns400(string method)
    {
        GivenPrefixes();

        var response = await Send(method, Token("skd", Skatteetaten, WriteScope), Skatteetaten, "skd:scope", "altinn:scope");

        await AssertInvalidPrefix(response, "altinn:scope");
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task ServiceOwner_PrefixClaimInToken_IsNotTrusted(string method)
    {
        GivenPrefixes();

        // the token says skd owns altinn, the prefix API says it does not
        var response = await Send(method, Token("skd", Skatteetaten, WriteScope, prefixes: ["skd", "altinn"]), Skatteetaten, "altinn:scope");

        await AssertInvalidPrefix(response, "altinn:scope");
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task Admin_ResourceOwnerOwnsPrefixes_Succeeds(string method)
    {
        GivenPrefixes();

        var response = await Send(method, Token("digdir", Digdir, AdminScope), Skatteetaten, "skd:scope");

        Assert.Equal(SuccessStatus(method), response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task Admin_ResourceOwnerDoesNotOwnPrefix_Returns400(string method)
    {
        GivenPrefixes();

        // DigDir owns altinn, but the resource owner (skd) does not. Before the flag, admin skipped the check.
        var response = await Send(method, Token("digdir", Digdir, AdminScope), Skatteetaten, "altinn:scope");

        await AssertInvalidPrefix(response, "altinn:scope");
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task InactivePrefix_Returns400(string method)
    {
        GivenPrefixes();

        var response = await Send(method, Token("skd", Skatteetaten, WriteScope), Skatteetaten, "skatteetaten:scope");

        await AssertInvalidPrefix(response, "skatteetaten:scope");
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task OwnerNotInPrefixList_Returns400(string method)
    {
        _prefixApi.Respond([new() { Prefix = "altinn", OwnerOrgNo = Digdir, Active = true }]);

        var response = await Send(method, Token("skd", Skatteetaten, WriteScope), Skatteetaten, "skd:scope");

        await AssertInvalidPrefix(response, "skd:scope");
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task PrefixApiUnavailable_Returns503(string method)
    {
        _prefixApi.Fail();

        var response = await Send(method, Token("skd", Skatteetaten, WriteScope), Skatteetaten, "skd:scope");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains(Problems.ScopePrefixList_Unavailable.ErrorCode.ToString()!, await response.Content.ReadAsStringAsync());
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task PrefixApiUnavailable_LogsError(string method)
    {
        _prefixApi.Fail();

        var response = await Send(method, Token("skd", Skatteetaten, WriteScope), Skatteetaten, "skd:scope");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        // why the fetch failed, logged by the validator
        Assert.Contains(_logs.Entries, e =>
            e.Category == typeof(ScopePrefixValidator).FullName
            && e.Level == LogLevel.Error
            && e.Exception is HttpRequestException);

        // which request could not be validated, logged by the controller
        string resource = method == HttpMethod.Post.Method ? NewResource : ExistingResource;
        Assert.Contains(_logs.Entries, e =>
            e.Category == typeof(ResourceController).FullName
            && e.Level == LogLevel.Error
            && e.Exception is ScopePrefixDataUnavailableException { InnerException: HttpRequestException }
            && e.Message.Contains(resource));
    }

    [Theory]
    [MemberData(nameof(Methods))]
    public async Task NoMaskinportenScopes_DoesNotCallPrefixApi(string method)
    {
        _prefixApi.Fail();

        var response = await Send(method, Token("skd", Skatteetaten, WriteScope), Skatteetaten);

        Assert.Equal(SuccessStatus(method), response.StatusCode);
        Assert.Equal(0, _prefixApi.CallCount);
    }

    [Fact]
    public async Task PrefixList_IsFetchedOnceForSeveralRequests()
    {
        GivenPrefixes();
        string token = Token("skd", Skatteetaten, WriteScope);

        await Send(HttpMethod.Put.Method, token, Skatteetaten, "skd:scope");
        await Send(HttpMethod.Put.Method, token, Skatteetaten, "skd:scope");
        await Send(HttpMethod.Post.Method, token, Skatteetaten, "skd:scope");

        Assert.Equal(1, _prefixApi.CallCount);
    }

    public static TheoryData<string> Methods => new() { HttpMethod.Post.Method, HttpMethod.Put.Method };

    private static HttpStatusCode SuccessStatus(string method)
        => method == HttpMethod.Post.Method ? HttpStatusCode.Created : HttpStatusCode.OK;

    private static string Token(string org, string orgNumber, string scope, string[]? prefixes = null)
        => PrincipalUtil.GetOrgToken(org, orgNumber, scope, prefixes);

    private async Task<HttpResponseMessage> Send(string method, string token, string ownerOrgNumber, params string[] scopes)
    {
        string identifier = method == HttpMethod.Post.Method ? NewResource : ExistingResource;
        string requestUri = method == HttpMethod.Post.Method
            ? "resourceregistry/api/v1/resource/"
            : $"resourceregistry/api/v1/resource/{identifier}";

        ServiceResource resource = new()
        {
            Identifier = identifier,
            Title = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            Status = "Active",
            Homepage = "www.altinn.no",
            IsPartOf = "Altinn",
            Keywords = [],
            Description = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            RightDescription = new Dictionary<string, string> { { "en", "English" }, { "nb", "Bokmal" }, { "nn", "Nynorsk" } },
            ContactPoints = [new ContactPoint { Category = "Support", ContactPage = "support.skd.no", Email = "support@skd.no", Telephone = "+4790012345" }],
            HasCompetentAuthority = new CompetentAuthority { Organization = ownerOrgNumber, Orgcode = "skd" },
            ResourceType = ResourceType.GenericAccessResource,
            ResourceReferences = scopes
                .Select(scope => new ResourceReference
                {
                    ReferenceSource = ReferenceSource.Altinn2,
                    ReferenceType = ReferenceType.MaskinportenScope,
                    Reference = scope,
                })
                .ToList(),
        };

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using HttpRequestMessage request = new(new HttpMethod(method), requestUri)
        {
            Content = new StringContent(JsonSerializer.Serialize(resource), Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Accept", "application/json");

        return await client.SendAsync(request);
    }

    private static async Task AssertInvalidPrefix(HttpResponseMessage response, params string[] expectedScopes)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ValidationProblemDetails? problem = JsonSerializer.Deserialize<ValidationProblemDetails>(
            await response.Content.ReadAsStringAsync(),
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        Assert.NotNull(problem);
        Assert.Equal(expectedScopes, problem.Errors["InvalidPrefix"]);
    }
}
