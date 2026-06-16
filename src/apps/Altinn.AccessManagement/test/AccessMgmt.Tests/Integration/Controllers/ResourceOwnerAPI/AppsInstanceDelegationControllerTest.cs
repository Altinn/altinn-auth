using System.Net;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Altinn.AccessManagement.Controllers;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.Models;
using Altinn.AccessManagement.Tests.Data;
using Altinn.AccessManagement.Tests.Mocks;
using Altinn.AccessManagement.Tests.Utils;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.ProblemDetails;
using Altinn.Common.AccessToken.Services;
using Altinn.Common.PEP.Interfaces;
using AltinnCore.Authentication.JwtCookie;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

// Migrated from CustomWebApplicationFactory<AppsInstanceDelegationController> to ApiFixture
// as part of Phase 2.2 (Sub-step 16.2a — AccessMgmt.Tests WAF consolidation, Group A
// single-configuration migrations). All tests share a single mock set (the previous
// `WithPDPMock` extension point was dead code), so DI is registered once in the
// constructor; per-test HttpClients are built via fixture.CreateClient().
// See: overhaul part-1 step 16
namespace Altinn.AccessManagement.Tests.Integration.Controllers;

[IntegrationTest]
public class AppsInstanceDelegationControllerTest : IClassFixture<ApiFixture>
{
    private readonly ApiFixture _fixture;
    private readonly JsonSerializerOptions options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Constructor setting up the shared <see cref="ApiFixture"/> with the mocks
    /// required by this controller's tests.
    /// </summary>
    /// <param name="fixture">Shared <see cref="ApiFixture"/>.</param>
    public AppsInstanceDelegationControllerTest(ApiFixture fixture)
    {
        _fixture = fixture;
        fixture.WithAppsettings(builder => builder.AddJsonFile("appsettings.test.json", optional: false));
        fixture.ConfigureServices(services =>
        {
            services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointMock>();
            services.AddSingleton<IDelegationMetadataRepository, DelegationMetadataRepositoryMock>();
            services.AddSingleton<IPolicyFactory, PolicyFactoryMock>();
            services.AddSingleton<IPostConfigureOptions<JwtCookieOptions>, JwtCookiePostConfigureOptionsStub>();
            services.AddSingleton<IPartiesClient, PartiesClientMock>();
            services.AddSingleton<IAMPartyService, AMPartyServiceMock>();
            services.AddSingleton<IProfileClient, ProfileClientMock>();
            services.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
            services.AddSingleton<IAltinnRolesClient, AltinnRolesClientMock>();
            services.AddSingleton<IPDP, PdpPermitMock>();
            services.AddSingleton<IAltinn2RightsClient, Tests.Mocks.Altinn2RightsClientMock>();

            // ApiFixture registers PublicSigningKeyProviderMock by default, but these
            // tests sign tokens via PrincipalUtil.GetAccessToken which requires the
            // issuer-cert-backed SigningKeyResolverMock.
            services.RemoveAll<IPublicSigningKeyProvider>();
            services.AddSingleton<IPublicSigningKeyProvider, SigningKeyResolverMock>();
        });

        fixture.EnsureSeedOnce<AppsInstanceDelegationControllerTest>(db =>
        {
            db.Entities.AddRange(TestEntities);
            db.SaveChanges();
        });
    }

    // Entities referenced by test data: parties used as From/To in delegation requests
    // and delegation changes returned by DelegationMetadataRepositoryMock.
    private static readonly Entity[] TestEntities =
    [
        new()
        {
            Id = Guid.Parse("0268B99A-5817-4BBF-9B62-D90B16D527EA"),
            Name = "KOLBJØRNSVIK OG ROAN",
            OrganizationIdentifier = "810419342",
            RefId = "810419342",
            PartyId = 50004226,
            TypeId = EntityTypeConstants.Organization,
            VariantId = EntityVariantConstants.AS,
            IsDeleted = false,
        },
        new()
        {
            Id = Guid.Parse("CE4BA72B-D111-404F-95B5-313FB3847FA1"),
            Name = "MARGRETHE THORUD",
            PersonIdentifier = "01025181049",
            RefId = "01025181049",
            PartyId = 50002115,
            TypeId = EntityTypeConstants.Person,
            VariantId = EntityVariantConstants.Person,
            IsDeleted = false,
        },
        // From party used by DelegationMetadataRepositoryMock for RevokeAll/Get tests
        new()
        {
            Id = Guid.Parse("B537C953-03C4-4822-B028-C15182ADC356"),
            Name = "Test App Delegator",
            OrganizationIdentifier = "000000001",
            RefId = "000000001",
            TypeId = EntityTypeConstants.Organization,
            VariantId = EntityVariantConstants.AS,
            IsDeleted = false,
        },
        // Synthetic To parties used by DelegationMetadataRepositoryMock for RevokeAll test
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0001-000000000010"),
            Name = "Test Person 10",
            TypeId = EntityTypeConstants.Person,
            VariantId = EntityVariantConstants.Person,
            IsDeleted = false,
        },
        new()
        {
            Id = Guid.Parse("00000000-0000-0000-0001-000000000012"),
            Name = "Test Person 12",
            TypeId = EntityTypeConstants.Person,
            VariantId = EntityVariantConstants.Person,
            IsDeleted = false,
        },
    ];

    /// <summary>
    /// Test case:  GET apps/instancedelegation/{resourceId}/{instanceId}/delegationcheck
    ///             with a valid PlatformAccessToken for an app having xacml rules specifying rights available for delegation
    /// Expected:   - Should return 200 OK
    /// Reason:     See testdata cases for details
    /// </summary>
    [Theory]
    [MemberData(nameof(TestDataAppsInstanceDelegation.DelegationCheck_Ok), MemberType = typeof(TestDataAppsInstanceDelegation))]
    public async Task DelegationCheck_ValidToken_OK(string platformToken, string resourceId, string instanceId, Paginated<ResourceRightDelegationCheckResultDto> expected)
    {
        var client = GetTestClient(platformToken);

        // Act
        HttpResponseMessage response = await client.GetAsync($"accessmanagement/api/v1/app/delegationcheck/resource/{resourceId}/instance/{instanceId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Paginated<ResourceRightDelegationCheckResultDto> actual = JsonSerializer.Deserialize<Paginated<ResourceRightDelegationCheckResultDto>>(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), options);

        AssertionUtil.AssertPagination(expected, actual, AssertionUtil.AssertResourceRightDelegationCheckResultDto);
    }

    /// <summary>
    /// Test case:  POST apps/instancedelegation/{resourceId}/{instanceId}
    ///             with a valid delegation
    /// Expected:   - Should return 200 OK
    /// Reason:     See testdat cases for details
    /// </summary>
    [Theory]
    [MemberData(nameof(TestDataAppsInstanceDelegation.DelegateParallelReadForAppNoExistingPolicy), MemberType = typeof(TestDataAppsInstanceDelegation))]
    [MemberData(nameof(TestDataAppsInstanceDelegation.DelegateParallelSignForAppExistingPolicy), MemberType = typeof(TestDataAppsInstanceDelegation))]
    [MemberData(nameof(TestDataAppsInstanceDelegation.DelegateNormalReadForAppNoExistingPolicy), MemberType = typeof(TestDataAppsInstanceDelegation))]
    [MemberData(nameof(TestDataAppsInstanceDelegation.DelegateNormalSignForAppExistingPolicy), MemberType = typeof(TestDataAppsInstanceDelegation))]
    [MemberData(nameof(TestDataAppsInstanceDelegation.DelegateNormalReadForAppNoExistingPolicyOrganizatonNumber), MemberType = typeof(TestDataAppsInstanceDelegation))]
    public async Task AppsInstanceDelegationController_ValidToken_Delegate_OK(string platformToken, AppsInstanceDelegationRequestDto request, string resourceId, string instanceId, AppsInstanceDelegationResponseDto expected)
    {
        var client = GetTestClient(platformToken);

        // Act
        HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/app/delegations/resource/{resourceId}/instance/{instanceId}", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, MediaTypeNames.Application.Json), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AppsInstanceDelegationResponseDto actual = JsonSerializer.Deserialize<AppsInstanceDelegationResponseDto>(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), options);
        AssertionUtil.AssertAppsInstanceDelegationResponseDto(expected, actual);
    }

    /// <summary>
    /// Test case:  POST apps/instancedelegation/{resourceId}/{instanceId}
    ///             with a valid delegation
    /// Expected:   - Should return 200 OK
    /// Reason:     See testdat cases for details
    /// </summary>
    [Theory]
    [MemberData(nameof(TestDataAppsInstanceDelegation.RevokeReadForAppOnlyExistingPolicyRevokeLast), MemberType = typeof(TestDataAppsInstanceDelegation))]
    [MemberData(nameof(TestDataAppsInstanceDelegation.RevokeReadForAppMultipleExistingPolicyRevoke), MemberType = typeof(TestDataAppsInstanceDelegation))]
    [MemberData(nameof(TestDataAppsInstanceDelegation.RevokeReadForAppNoExistingPolicyRevokeLast), MemberType = typeof(TestDataAppsInstanceDelegation))]
    public async Task AppsInstanceDelegationController_ValidToken_Revoke_OK(string platformToken, AppsInstanceDelegationRequestDto request, string resourceId, string instanceId, AppsInstanceRevokeResponseDto expected)
    {
        var client = GetTestClient(platformToken);

        // Act
        HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/app/delegationrevoke/resource/{resourceId}/instance/{instanceId}", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, MediaTypeNames.Application.Json), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        AppsInstanceRevokeResponseDto actual = JsonSerializer.Deserialize<AppsInstanceRevokeResponseDto>(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), options);
        AssertionUtil.AssertAppsInstanceRevokeResponseDto(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TestDataAppsInstanceDelegation.RevokeAll), MemberType = typeof(TestDataAppsInstanceDelegation))]
    public async Task AppsInstanceDelegationController_ValidToken_RevokeAll_OK(string platformToken, string resourceId, string instanceId, Paginated<AppsInstanceRevokeResponseDto> expected)
    {
        var client = GetTestClient(platformToken);

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"accessmanagement/api/v1/app/delegationrevoke/resource/{resourceId}/instance/{instanceId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Paginated<AppsInstanceRevokeResponseDto> actual = JsonSerializer.Deserialize<Paginated<AppsInstanceRevokeResponseDto>>(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), options);
        AssertionUtil.AssertPagination(expected, actual, AssertionUtil.AssertAppsInstanceRevokeResponseDto);
    }

    [Theory]
    [MemberData(nameof(TestDataAppsInstanceDelegation.RevokeAllToManyPolicyFiles), MemberType = typeof(TestDataAppsInstanceDelegation))]
    public async Task AppsInstanceDelegationController_ValidToken_RevokeAll_ToManyPolicyFilesToUpdate(string platformToken, string resourceId, string instanceId, AltinnProblemDetails expected)
    {
        var client = GetTestClient(platformToken);

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"accessmanagement/api/v1/app/delegationrevoke/resource/{resourceId}/instance/{instanceId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        AltinnProblemDetails actual = JsonSerializer.Deserialize<AltinnProblemDetails>(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), options);
        TestDataAppsInstanceDelegation.AssertAltinnProblemDetailsEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TestDataAppsInstanceDelegation.RevokeAllUnathorized), MemberType = typeof(TestDataAppsInstanceDelegation))]
    public async Task AppsInstanceDelegationController_NoToken_RevokeAll_Unauthorized(string resourceId, string instanceId)
    {
        var client = GetTestClient(null);

        // Act
        HttpResponseMessage response = await client.DeleteAsync($"accessmanagement/api/v1/app/delegationrevoke/resource/{resourceId}/instance/{instanceId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test case:  POST apps/instancedelegation/{resourceId}/{instanceId}
    ///             with a valid delegation
    /// Expected:   - Should return 200 OK
    /// Reason:     See testdat cases for details
    /// </summary>
    [Theory]
    [MemberData(nameof(TestDataAppsInstanceDelegation.DelegateReadForAppNoExistingPolicyNoResponceDBWrite), MemberType = typeof(TestDataAppsInstanceDelegation))]
    public async Task AppsInstanceDelegationController_ValidToken_Delegate_DBWriteFails(string platformToken, AppsInstanceDelegationRequestDto request, string resourceId, string instanceId, AppsInstanceDelegationResponseDto expected)
    {
        var client = GetTestClient(platformToken);

        // Act
        HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/app/delegations/resource/{resourceId}/instance/{instanceId}", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, MediaTypeNames.Application.Json), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.PartialContent, response.StatusCode);

        AppsInstanceDelegationResponseDto actual = JsonSerializer.Deserialize<AppsInstanceDelegationResponseDto>(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), options);
        AssertionUtil.AssertAppsInstanceDelegationResponseDto(expected, actual);
    }

    /// <summary>
    /// Test case:  POST apps/instancedelegation/{resourceId}/{instanceId}
    ///             with a valid delegation
    /// Expected:   - Should return 200 OK
    /// Reason:     See testdat cases for details
    /// </summary>
    [Theory]
    [MemberData(nameof(TestDataAppsInstanceDelegation.DelegateToPartyNotExisting), MemberType = typeof(TestDataAppsInstanceDelegation))]
    public async Task AppsInstanceDelegationController_ValidToken_Delegate_InvalidParty(string platformToken, AppsInstanceDelegationRequestDto request, string resourceId, string instanceId, AltinnProblemDetails expected)
    {
        var client = GetTestClient(platformToken);

        // Act
        HttpResponseMessage response = await client.PostAsync($"accessmanagement/api/v1/app/delegations/resource/{resourceId}/instance/{instanceId}", new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, MediaTypeNames.Application.Json), TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        AltinnProblemDetails actual = JsonSerializer.Deserialize<AltinnProblemDetails>(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), options);
        TestDataAppsInstanceDelegation.AssertAltinnProblemDetailsEqual(expected, actual);
    }

    [Theory]
    [MemberData(nameof(TestDataAppsInstanceDelegation.GetAllAppDelegatedInstances), MemberType = typeof(TestDataAppsInstanceDelegation))]
    public async Task AppsInstanceDelegationController_ValidToken_Get_OK(string platformToken, string resourceId, string instanceId, Paginated<AppsInstanceDelegationResponseDto> expected)
    {
        var client = GetTestClient(platformToken);

        // Act
        HttpResponseMessage response = await client.GetAsync($"accessmanagement/api/v1/app/delegations/resource/{resourceId}/instance/{instanceId}", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Paginated<AppsInstanceDelegationResponseDto> actual = JsonSerializer.Deserialize<Paginated<AppsInstanceDelegationResponseDto>>(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), options);
        AssertionUtil.AssertPagination(expected, actual, AssertionUtil.AssertAppsInstanceDelegationResponseDto);
    }

    private HttpClient GetTestClient(string token)
    {
        HttpClient client = _fixture.CreateClient(new() { AllowAutoRedirect = false });

        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (token != null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        client.DefaultRequestHeaders.Add("PlatformAccessToken", token);
        return client;
    }
}
