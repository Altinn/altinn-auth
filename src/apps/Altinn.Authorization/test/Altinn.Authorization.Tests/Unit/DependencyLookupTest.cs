using System.Collections;
using System.Net;
using Altinn.ApiClients.Maskinporten.Interfaces;
using Altinn.ApiClients.Maskinporten.Models;
using Altinn.Authorization.Api.Contracts.Authorization;
using Altinn.Authorization.Api.Contracts.Register;
using Altinn.Authorization.Api.Contracts.ResourceRegistry;
using Altinn.Authorization.Services.Implementation;
using Altinn.Common.AccessTokenClient.Services;
using Altinn.Platform.Authorization.Clients;
using Altinn.Platform.Authorization.Configuration;
using Altinn.Platform.Authorization.Exceptions;
using Altinn.Platform.Authorization.Helpers;
using Altinn.Platform.Authorization.Models.AccessManagement;
using Altinn.Platform.Authorization.Repositories;
using Altinn.Platform.Authorization.Services;
using Altinn.Platform.Authorization.Services.Implementation;
using Altinn.Platform.Authorization.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Altinn.Authorization.Tests.Unit;

/// <summary>
/// Every lookup the decision makes against another service must tell "the service failed" apart from
/// "the service has nothing for this request". These tests run each lookup against a stubbed HTTP response.
/// </summary>
[UnitTest]
public sealed class DependencyLookupTest : IDisposable
{
    private const string Endpoint = "http://localhost/";

    private const string RrResource = "resource-registry: resource";
    private const string RrPolicy = "resource-registry: policy";
    private const string RrMembershipsForResource = "resource-registry: access list memberships for resource";
    private const string RrMembershipsForParty = "resource-registry: access list memberships for party";
    private const string ProfileByUserId = "profile: user by user id";
    private const string ProfileByPersonId = "profile: user by person id";
    private const string RegisterPartiesById = "register: parties by party id";
    private const string RegisterPartiesByUuid = "register: parties by party uuid";
    private const string RegisterParty = "register: party";
    private const string RegisterLookup = "register: party lookup";
    private const string OedRoles = "oed-authz: role assignments";
    private const string AmDelegationChanges = "access-management: delegation changes";
    private const string AmRolesAndPackages = "access-management: roles and access packages";
    private const string StorageAuthInfo = "storage: auth info";

    private static readonly string[] Lookups =
    [
        RrResource, RrPolicy, RrMembershipsForResource, RrMembershipsForParty,
        ProfileByUserId, ProfileByPersonId,
        RegisterPartiesById, RegisterPartiesByUuid, RegisterParty, RegisterLookup,
        OedRoles, AmDelegationChanges, AmRolesAndPackages, StorageAuthInfo,
    ];

    private readonly MemoryCache _memoryCache = new(new MemoryCacheOptions());

    public static TheoryData<string, string> FailedLookups()
    {
        TheoryData<string, string> data = [];
        foreach (string lookup in Lookups)
        {
            foreach (string failure in new[] { "500", "503", "429", "401", "timeout", "connection" })
            {
                data.Add(lookup, failure);
            }
        }

        return data;
    }

    public static TheoryData<string> AllLookups() => [.. Lookups];

    public void Dispose() => _memoryCache.Dispose();

    [Theory]
    [MemberData(nameof(FailedLookups))]
    public async Task Lookup_ServiceFails_ThrowsDependencyFailed(string lookup, string failure)
    {
        StubHandler handler = new(failure);

        DependencyFailedException ex = await Assert.ThrowsAsync<DependencyFailedException>(() => Invoke(lookup, handler));

        Assert.Equal(lookup.Split(':')[0], ex.Dependency);
        Assert.Equal(int.TryParse(failure, out int statusCode) ? (HttpStatusCode)statusCode : null, ex.StatusCode);
    }

    [Theory]
    [MemberData(nameof(AllLookups))]
    public async Task Lookup_ServiceHasNothing_IsNotAFailedLookup(string lookup)
    {
        StubHandler handler = new("404");

        Exception ex = await Record.ExceptionAsync(() => Invoke(lookup, handler));

        Assert.IsNotType<DependencyFailedException>(ex);
    }

    [Theory]
    [InlineData(RrResource)]
    [InlineData(RrPolicy)]
    [InlineData(RrMembershipsForResource)]
    [InlineData(RrMembershipsForParty)]
    [InlineData(ProfileByUserId)]
    [InlineData(ProfileByPersonId)]
    [InlineData(RegisterPartiesById)]
    [InlineData(RegisterPartiesByUuid)]
    [InlineData(RegisterParty)]
    [InlineData(OedRoles)]
    public async Task Lookup_ServiceHasNothing_ReturnsNothing(string lookup)
    {
        StubHandler handler = new("404");

        object result = await Invoke(lookup, handler);

        Assert.True(result is null || (result is IEnumerable items && !items.Cast<object>().Any()), $"Expected nothing, got {result}");
    }

    [Theory]
    [InlineData(RegisterLookup, typeof(PlatformHttpException))]
    [InlineData(StorageAuthInfo, typeof(PlatformHttpException))]
    [InlineData(AmDelegationChanges, typeof(HttpRequestException))]
    [InlineData(AmRolesAndPackages, typeof(HttpRequestException))]
    public async Task Lookup_ServiceHasNothing_KeepsTheExceptionItThrewBefore(string lookup, Type expected)
    {
        StubHandler handler = new("404");

        Exception ex = await Record.ExceptionAsync(() => Invoke(lookup, handler));

        Assert.IsType(expected, ex);
    }

    [Fact]
    public async Task GetResourceAsync_FailedLookup_IsNotCached()
    {
        StubHandler handler = new("500", "200:{\"identifier\":\"ttd-resource\"}");
        ResourceRegistryWrapper sut = CreateResourceRegistry(handler);

        await Assert.ThrowsAsync<DependencyFailedException>(() => sut.GetResourceAsync("ttd-resource", TestContext.Current.CancellationToken));
        var resource = await sut.GetResourceAsync("ttd-resource", TestContext.Current.CancellationToken);

        Assert.NotNull(resource);
        Assert.Equal("ttd-resource", resource.Identifier);
        Assert.Equal(2, handler.Calls);
    }

    [Fact]
    public async Task GetParty_FailedLookup_IsNotWrappedInAggregateException()
    {
        RegisterService sut = CreateRegister(new StubHandler("503"));

        Exception ex = await Record.ExceptionAsync(() => sut.GetParty(50001337, TestContext.Current.CancellationToken));

        Assert.IsType<DependencyFailedException>(ex);
    }

    private async Task<object> Invoke(string lookup, StubHandler handler)
    {
        CancellationToken ct = TestContext.Current.CancellationToken;

        switch (lookup)
        {
            case RrResource:
                return await CreateResourceRegistry(handler).GetResourceAsync("ttd-resource", ct);
            case RrPolicy:
                return await CreateResourceRegistry(handler).GetResourcePolicyAsync("ttd-resource", ct);
            case RrMembershipsForResource:
                PartyUrn.TryParse($"urn:altinn:party:uuid:{Guid.NewGuid()}", out PartyUrn party);
                ResourceIdUrn.TryParse("urn:altinn:resource:ttd-resource", out ResourceIdUrn resource);
                return await CreateResourceRegistry(handler).GetMembershipsForResourceForParty(party, resource, ct);
            case RrMembershipsForParty:
                return await CreateResourceRegistry(handler).GetMembershipsForParty(PartyUrn.PartyUuid.Create(Guid.NewGuid()), ct);
            case ProfileByUserId:
                return await CreateProfile(handler).GetUserProfile(20000490, ct);
            case ProfileByPersonId:
                return await CreateProfile(handler).GetUserProfileByPersonId("01017012345", ct);
            case RegisterPartiesById:
                return await CreateRegister(handler).GetPartiesAsync(new List<int> { 50001337 }, false, ct);
            case RegisterPartiesByUuid:
                return await CreateRegister(handler).GetPartiesAsync(new List<Guid> { Guid.NewGuid() }, false, ct);
            case RegisterParty:
                return await CreateRegister(handler).GetParty(50001337, ct);
            case RegisterLookup:
                return await CreateRegister(handler).PartyLookup("910459880", null, ct);
            case OedRoles:
                return await CreateOed(handler).GetOedRoleAssignments("01017012345", "02027012345");
            case AmDelegationChanges:
                return await CreateAccessManagement(handler).GetAllDelegationChanges(new DelegationChangeInputDto(), ct);
            case AmRolesAndPackages:
                return await CreateAccessManagement(handler).GetRolesAndAccessPackages(Guid.NewGuid(), Guid.NewGuid(), cancellationToken: ct);
            case StorageAuthInfo:
                return await new InstanceMetadataRepository(NullLogger<InstanceMetadataRepository>.Instance, new HttpClient(handler), PlatformOptions()).GetAuthInfo("50001337/7dd3c208-0062-4ff6-9ed9-e2f1a8d1dd7b");
            default:
                throw new ArgumentOutOfRangeException(nameof(lookup), lookup, null);
        }
    }

    private static IOptions<PlatformSettings> PlatformOptions() => Options.Create(new PlatformSettings
    {
        ApiAccessManagementEndpoint = Endpoint,
        ApiProfileEndpoint = Endpoint,
        ApiRegisterEndpoint = Endpoint,
        ApiResourceRegistryEndpoint = Endpoint,
        ApiStorageEndpoint = Endpoint,
    });

    private static IOptions<GeneralSettings> GeneralOptions() => Options.Create(new GeneralSettings
    {
        OedAuthzApiEndpoint = Endpoint,
        PolicyCacheTimeout = 5,
        RoleCacheTimeout = 5,
        RuntimeCookieName = "AltinnStudioRuntime",
    });

    private ResourceRegistryWrapper CreateResourceRegistry(StubHandler handler) =>
        new(new ResourceRegistryClient(new HttpClient(handler), PlatformOptions()), Mock.Of<IAccessTokenGenerator>(), _memoryCache, GeneralOptions());

    private static ProfileWrapper CreateProfile(StubHandler handler) =>
        new(new ProfileClient(new HttpClient(handler), PlatformOptions()), NullLogger<ProfileWrapper>.Instance);

    private RegisterService CreateRegister(StubHandler handler) =>
        new(
            new HttpClient(handler),
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            Mock.Of<IAccessTokenGenerator>(),
            GeneralOptions(),
            PlatformOptions(),
            NullLogger<IRegisterService>.Instance,
            _memoryCache);

    private static OedRoleAssignmentWrapper CreateOed(StubHandler handler)
    {
        Mock<IMaskinportenService> maskinporten = new();
        maskinporten.SetReturnsDefault(Task.FromResult(new TokenResponse { AccessToken = "token" }));

        return new OedRoleAssignmentWrapper(
            NullLogger<OedRoleAssignmentWrapper>.Instance,
            new OedAuthzClient(new HttpClient(handler), GeneralOptions()),
            maskinporten.Object,
            Options.Create(new OedAuthzMaskinportenClientSettings()));
    }

    private AccessManagementWrapper CreateAccessManagement(StubHandler handler) =>
        new(
            GeneralOptions(),
            new AccessManagementClient(new HttpClient(handler), PlatformOptions()),
            new HttpContextAccessor { HttpContext = new DefaultHttpContext() },
            _memoryCache);

    /// <summary>
    /// Answers each request with the next scripted outcome, and repeats the last one. An outcome is a status
    /// code, optionally followed by a colon and a JSON body, or "timeout" or "connection".
    /// </summary>
    private sealed class StubHandler(params string[] outcomes) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string outcome = outcomes[Math.Min(Calls, outcomes.Length - 1)];
            Calls++;

            switch (outcome)
            {
                case "timeout":
                    throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout", new TimeoutException());
                case "connection":
                    throw new HttpRequestException("Connection refused");
            }

            string[] parts = outcome.Split(':', 2);
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)int.Parse(parts[0]))
            {
                Content = new StringContent(parts.Length > 1 ? parts[1] : string.Empty, System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}
