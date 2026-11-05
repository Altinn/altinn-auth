using System.Net;
using System.Text.Json;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration;

/// <summary>
/// Regression tests for the generated swagger documents. The v1 document is
/// extracted by APIM under the name "v1" at <c>/swagger/v1/swagger.json</c>,
/// so its URL, document name and path set must stay stable. Schema and
/// operation contents are owned by the endpoints themselves and are not
/// pinned here, except how <c>NonExhaustiveEnum</c> is described, which
/// depends on host configuration. Swagger is only mapped in the Development environment, which
/// is the default environment for the <see cref="ApiFixture"/> test host.
/// </summary>
public class SwaggerDocTest
{
    public const string ClientDelegationV1Route = "/accessmanagement/api/v1/enduser/clientdelegations";

    public const string ClientDelegationV2Route = "/accessmanagement/api/v2/enduser/clientdelegations";

    public const string AuthorizedPartiesV1Route = "/accessmanagement/api/v1/enduser/authorizedparties";

    public const string MaskinportenV2Route = "/accessmanagement/api/v2/enduser/maskinporten";

    /// <summary>
    /// Fetches a swagger document and returns the response together with the
    /// path keys of the parsed document.
    /// </summary>
    private static async Task<(HttpResponseMessage Response, List<string> Paths)> GetSwaggerDoc(HttpClient client, string docName)
    {
        var response = await client.GetAsync($"/swagger/{docName}/swagger.json", TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var doc = JsonDocument.Parse(body);
        var paths = doc.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
        return (response, paths);
    }

    /// <summary>
    /// Tests for the v1 swagger document.
    /// </summary>
    [IntegrationTest]
    public class V1Doc : IClassFixture<ApiFixture>
    {
        public V1Doc(ApiFixture fixture)
        {
            Client = fixture.BuildConfiguration();
        }

        private HttpClient Client { get; }

        [Fact]
        public async Task GetSwaggerJson_V1Doc_Returns200WithClientDelegationV1Paths()
        {
            var (_, paths) = await GetSwaggerDoc(Client, "v1");

            paths.Should().Contain(p => p.StartsWith($"{ClientDelegationV1Route}/", StringComparison.Ordinal));
        }

        [Fact]
        public async Task GetSwaggerJson_V1Doc_Returns200WithUnversionedControllerPaths()
        {
            var (_, paths) = await GetSwaggerDoc(Client, "v1");

            paths.Should().Contain(p => p.StartsWith(AuthorizedPartiesV1Route, StringComparison.Ordinal));
        }

        [Fact]
        public async Task GetSwaggerJson_V1Doc_Returns200WithoutV2Paths()
        {
            var (_, paths) = await GetSwaggerDoc(Client, "v1");

            paths.Should().NotContain(p => p.Contains("/v2/", StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Tests for the v2 swagger document. Only the client delegation and
    /// Maskinporten APIs are versioned, so the v2 document must contain their
    /// /v2/ paths and nothing else.
    /// </summary>
    [IntegrationTest]
    public class V2Doc : IClassFixture<ApiFixture>
    {
        public V2Doc(ApiFixture fixture)
        {
            fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableEnduserMaskinportenAdminApi);
            Client = fixture.BuildConfiguration();
        }

        private HttpClient Client { get; }

        [Fact]
        public async Task GetSwaggerJson_V2Doc_Returns200WithOnlyClientDelegationAndMaskinportenV2Paths()
        {
            var (_, paths) = await GetSwaggerDoc(Client, "v2");

            paths.Should().NotBeEmpty();
            paths.Should().OnlyContain(p =>
                p.StartsWith($"{ClientDelegationV2Route}/", StringComparison.Ordinal) ||
                p.StartsWith($"{MaskinportenV2Route}/", StringComparison.Ordinal));
        }

        [Fact]
        public async Task GetSwaggerJson_V2Doc_Returns200WithMaskinportenV2Paths()
        {
            var (_, paths) = await GetSwaggerDoc(Client, "v2");

            paths.Should().Contain(p => p.StartsWith($"{MaskinportenV2Route}/consumers", StringComparison.Ordinal));
            paths.Should().Contain(p => p.StartsWith($"{MaskinportenV2Route}/suppliers", StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Tests that <c>NonExhaustiveEnum</c> properties are described as the string
    /// they serialize to: one string enum per known value, plus a plain string for
    /// values the client does not know. Without the ModelUtils Swashbuckle support
    /// they are described as an object with <c>isUnknown</c> and <c>value</c>.
    /// </summary>
    [IntegrationTest]
    public class NonExhaustiveEnumSchemas : IClassFixture<ApiFixture>
    {
        public NonExhaustiveEnumSchemas(ApiFixture fixture)
        {
            fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableEnduserRequestV2Api);
            Client = fixture.BuildConfiguration();
        }

        private HttpClient Client { get; }

        [Fact]
        public async Task GetSwaggerJson_V1Doc_RequestStatusV2IsNonExhaustiveString()
        {
            var schemas = await GetSchemas(Client, "v1");

            AssertNonExhaustiveRequestStatus(ResolveProperty(schemas, "AMCoreModels.RequestDto", "statusV2"));
        }

        [Fact]
        public async Task GetSwaggerJson_V1Doc_RequestStatusIsPlainEnum()
        {
            var schemas = await GetSchemas(Client, "v1");

            var status = ResolveProperty(schemas, "AMCoreModels.RequestDto", "status");
            status.GetProperty("type").GetString().Should().Be("string");
            status.GetProperty("enum").EnumerateArray().Select(e => e.GetString()).Should().Contain("Approved").And.NotContain("PartialApprove");
        }

        [Theory]
        [InlineData("AMCoreModels.RequestDto")]
        [InlineData("AMCoreModels.ActivityLogDto")]
        [InlineData("AMCoreModels.ActivityTypeDto")]
        public async Task GetSwaggerJson_V2Doc_StatusIsNonExhaustiveString(string schemaName)
        {
            var schemas = await GetSchemas(Client, "v2");

            AssertNonExhaustiveRequestStatus(ResolveProperty(schemas, schemaName, "status"));
        }

        private static async Task<JsonElement> GetSchemas(HttpClient client, string docName)
        {
            var body = await client.GetStringAsync($"/swagger/{docName}/swagger.json", TestContext.Current.CancellationToken);
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("components").GetProperty("schemas").Clone();
        }

        /// <summary>
        /// Returns the schema of a property, following its <c>$ref</c>.
        /// </summary>
        private static JsonElement ResolveProperty(JsonElement schemas, string schemaName, string propertyName)
        {
            var property = schemas.GetProperty(schemaName).GetProperty("properties").GetProperty(propertyName);
            if (!property.TryGetProperty("$ref", out var reference))
            {
                return property;
            }

            return schemas.GetProperty(reference.GetString()!.Split('/').Last());
        }

        private static void AssertNonExhaustiveRequestStatus(JsonElement schema)
        {
            schema.TryGetProperty("properties", out _).Should().BeFalse("a NonExhaustiveEnum serializes as a string, not an object");

            var oneOf = schema.GetProperty("oneOf").EnumerateArray().ToList();
            oneOf.Select(s => s.GetProperty("type").GetString()).Should().OnlyContain(type => type == "string");

            // Each known value is a single-value enum; the alternative without an enum takes unknown values.
            var knownValues = oneOf.Where(s => s.TryGetProperty("enum", out _)).Select(s => s.GetProperty("enum")[0].GetString()).ToList();
            knownValues.Should().Contain("PartialApprove");
            oneOf.Count.Should().Be(knownValues.Count + 1, "unknown values must be allowed");
        }
    }

    /// <summary>
    /// Tests that the status filter of the request endpoints is versioned: version 1 only
    /// offers the statuses it was published with, while version 2 also offers those added later.
    /// </summary>
    [IntegrationTest]
    public class RequestStatusFilter : IClassFixture<ApiFixture>
    {
        public RequestStatusFilter(ApiFixture fixture)
        {
            fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentResource);
            fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableRequestAssignmentPackage);
            fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnableEnduserRequestV2Api);
            Client = fixture.BuildConfiguration();
        }

        private HttpClient Client { get; }

        [Theory]
        [InlineData("v1", false)]
        [InlineData("v2", true)]
        public async Task GetSwaggerJson_SentRequestsStatusFilter_OffersPartialApproveOnlyInV2(string version, bool offersPartialApprove)
        {
            var body = await Client.GetStringAsync($"/swagger/{version}/swagger.json", TestContext.Current.CancellationToken);
            using var doc = JsonDocument.Parse(body);
            var schemas = doc.RootElement.GetProperty("components").GetProperty("schemas");

            var parameter = doc.RootElement
                .GetProperty("paths")
                .GetProperty($"/accessmanagement/api/{version}/enduser/request/sent")
                .GetProperty("get")
                .GetProperty("parameters")
                .EnumerateArray()
                .Single(p => p.GetProperty("name").GetString() == "status");

            var items = parameter.GetProperty("schema").GetProperty("items");
            var statuses = items.TryGetProperty("$ref", out var reference)
                ? schemas.GetProperty(reference.GetString()!.Split('/').Last())
                : items;

            var values = statuses.GetProperty("enum").EnumerateArray().Select(e => e.GetString()).ToList();
            values.Should().Contain("Approved");
            values.Contains("PartialApprove").Should().Be(offersPartialApprove);
        }
    }
}
