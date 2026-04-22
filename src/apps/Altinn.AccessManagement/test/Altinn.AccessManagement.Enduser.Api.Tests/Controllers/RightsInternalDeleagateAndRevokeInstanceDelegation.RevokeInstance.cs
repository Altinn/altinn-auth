using System.Net;
using System.Net.Mime;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Altinn.AccessManagement.Core.Clients.Interfaces;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Repositories.Interfaces;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessManagement.TestUtils.Mocks;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

/// <summary>
/// Tests for <see cref="RightsInternalDelegateAndRevokeInstanceDelegation"/>.
/// </summary>
/// <remarks>
/// These tests verify the complete flow of revoking instance delegations, including:
/// - Creating and then revoking delegations
/// - Idempotent revocation behavior
/// - Validation of input parameters
/// - Database persistence of revocation operations
/// </remarks>
public class RightsInternalDelegateAndRevokeInstanceDelegation : IClassFixture<ApiFixture>
{
    private const string InstanceId = "fa0678ad-960d-4307-aba2-ba29c9804c9d";
    private const string InstanceUrn = "urn:altinn:correspondence-id:fa0678ad-960d-4307-aba2-ba29c9804c9d";
    private const string RouteDelegation = "/accessmanagement/api/v1/internal/instance/delegation";
    private const string RouteRevoke = "/accessmanagement/api/v1/internal/instance/revoke";

    private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

    public RightsInternalDelegateAndRevokeInstanceDelegation(ApiFixture fixture)
    {
        Fixture = fixture;
        Fixture.ConfiureServices(services =>
        {
            services.AddSingleton<IAltinn2RightsClient, Altinn2RightsClientMock>();
            services.AddSingleton<IResourceRegistryClient, ResourceRegistryClientMock>();
            services.AddSingleton<IPolicyFactory, PolicyFactoryMock>();
            services.AddSingleton<IPolicyRetrievalPoint, PolicyRetrievalPointWithWrittenPoliciesMock>();
        });
    }

    public ApiFixture Fixture { get; }

    private HttpClient CreateClient()
    {
        // Use platform authorization token for internal endpoints
        var client = Fixture.Server.CreateClient();
        var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
        {
            claims.Add(new Claim("urn:altinn:app", "sbl.authorization"));
        });
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        return client;
    }

    /// <summary>
    /// Test case: Successfully revokes an existing instance delegation. since the last connected affiliation with the RightHolder assignment is removed
    /// also the assignment is removed
    /// Creates a delegation first, then revokes it.
    /// Expected: Returns 204 NoContent when revoke is successful.
    /// </summary>
    [Fact]
    public async Task DelegateAndRevokeInstance_ValidRequest_NoAssignmentLeft_ReturnsNoContent()
    {
        // Arrange
        // Access the database to verify
        using var scope = Fixture.Server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var fromUuid = TestData.HanSoloEnterprise.Id;
        var toUuid = TestData.BenSolo.Id;
        var performedBy = TestData.HanSolo.Id;
        var resourceId = TestData.TestdirektoratetCorrespondenceService.RefId;
            
        // First, create a delegation to revoke
        var delegationRequest = new InstanceDelegationRequest
        {
            AuthorizationRuleID = 12345,
            Created = DateTimeOffset.UtcNow,
            FromUuid = fromUuid,
            ToUuid = toUuid,
            PerformedBy = performedBy,
            ResourceId = resourceId,
            InstanceId = InstanceId,
            Actions = new List<string> { "read", "subscribe" }
        };

        var delegationContent = new StringContent(
            JsonSerializer.Serialize(delegationRequest, _jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = CreateClient();
            
        // Create the delegation first
        var delegationResponse = await client.PostAsync(RouteDelegation, delegationContent);
        Assert.Equal(HttpStatusCode.Created, delegationResponse.StatusCode);

        // Verify
        var existingAssignment = await db.Assignments
            .FirstOrDefaultAsync(
                a =>
                    a.FromId == fromUuid &&
                    a.ToId == toUuid &&
                    a.RoleId == RoleConstants.Rightholder, 
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(existingAssignment);

        var existingAssignmentInstance = await db.AssignmentInstances
            .FirstOrDefaultAsync(
                ai =>
                    ai.AssignmentId == existingAssignment.Id &&
                    ai.ResourceId == TestData.TestdirektoratetCorrespondenceService.Id &&
                    ai.InstanceId == InstanceUrn,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(existingAssignmentInstance);

        // Now create the revoke request
        var revokeRequest = new InstanceRevokeRequest
        {
            AuthorizationRuleID = 12345,
            Created = DateTimeOffset.UtcNow,
            FromUuid = fromUuid,
            ToUuid = toUuid,
            PerformedBy = performedBy,
            ResourceId = resourceId,
            InstanceId = InstanceId
        };

        var revokeContent = new StringContent(
            JsonSerializer.Serialize(revokeRequest, _jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        // Act
        var revokeResponse = await client.PostAsync(RouteRevoke, revokeContent, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        var revokedAssignment = await db.Assignments
            .FirstOrDefaultAsync(
                a =>
                    a.FromId == fromUuid &&
                    a.ToId == toUuid &&
                    a.RoleId == RoleConstants.Rightholder,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(revokedAssignment);            
    }

    /// <summary>
    /// Test case: Successfully revokes an existing instance delegation. since the last connected affiliation with the RightHolder assignment is not removed
    /// the assignment is not removedremoved
    /// Creates a delegation first, then revokes it.
    /// Expected: Returns 204 NoContent when revoke is successful.
    /// </summary>
    [Fact]
    public async Task DelegateAndRevokeInstance_ValidRequest_AssignmentLeft_ReturnsNoContent()
    {
        // Arrange
        // Access the database to verify
        using var scope = Fixture.Server.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var fromUuid = TestData.HanSoloEnterprise.Id;
        var toUuid = TestData.LeiaOrgana.Id;
        var performedBy = TestData.HanSolo.Id;
        var resourceId = TestData.TestdirektoratetCorrespondenceService.RefId;
        var instanceId2 = "fae4681e-ff2d-45d4-a34b-23e8e0b9ff9d";
        var instanceUrn2 = $"{AltinnXacmlConstants.MatchAttributeIdentifiers.CorrespondenceInstanceAttribute}:{instanceId2}";

        // First, create two delegations to revoke
        var delegationRequest1 = new InstanceDelegationRequest
        {
            AuthorizationRuleID = 12345,
            Created = DateTimeOffset.UtcNow,
            FromUuid = fromUuid,
            ToUuid = toUuid,
            PerformedBy = performedBy,
            ResourceId = resourceId,
            InstanceId = InstanceId,
            Actions = new List<string> { "read", "subscribe" }
        };

        var delegationRequest2 = new InstanceDelegationRequest
        {
            AuthorizationRuleID = 12345,
            Created = DateTimeOffset.UtcNow,
            FromUuid = fromUuid,
            ToUuid = toUuid,
            PerformedBy = performedBy,
            ResourceId = resourceId,
            InstanceId = instanceId2,
            Actions = new List<string> { "read", "subscribe" }
        };

        var delegationContent1 = new StringContent(
            JsonSerializer.Serialize(delegationRequest1, _jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var delegationContent2 = new StringContent(
            JsonSerializer.Serialize(delegationRequest2, _jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = CreateClient();

        // Create the delegations first
        var delegationResponse1 = await client.PostAsync(RouteDelegation, delegationContent1, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, delegationResponse1.StatusCode);

        var delegationResponse2 = await client.PostAsync(RouteDelegation, delegationContent2, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, delegationResponse1.StatusCode);

        // Verify
        var existingAssignment = await db.Assignments
            .FirstOrDefaultAsync(
                a =>
                    a.FromId == fromUuid &&
                    a.ToId == toUuid &&
                    a.RoleId == RoleConstants.Rightholder,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(existingAssignment);

        var existingAssignmentInstance1 = await db.AssignmentInstances
            .FirstOrDefaultAsync(
                ai =>
                    ai.AssignmentId == existingAssignment.Id &&
                    ai.ResourceId == TestData.TestdirektoratetCorrespondenceService.Id &&
                    ai.InstanceId == InstanceUrn,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(existingAssignmentInstance1);

        var existingAssignmentInstance2 = await db.AssignmentInstances
            .FirstOrDefaultAsync(
                ai =>
                    ai.AssignmentId == existingAssignment.Id &&
                    ai.ResourceId == TestData.TestdirektoratetCorrespondenceService.Id &&
                    ai.InstanceId == instanceUrn2,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(existingAssignmentInstance2);

        // Now create the revoke request
        var revokeRequest = new InstanceRevokeRequest
        {
            AuthorizationRuleID = 12345,
            Created = DateTimeOffset.UtcNow,
            FromUuid = fromUuid,
            ToUuid = toUuid,
            PerformedBy = performedBy,
            ResourceId = resourceId,
            InstanceId = InstanceId
        };

        var revokeContent = new StringContent(
            JsonSerializer.Serialize(revokeRequest, _jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        // Act
        var revokeResponse = await client.PostAsync(RouteRevoke, revokeContent, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

        var revokedAssignment = await db.Assignments
            .FirstOrDefaultAsync(
                a =>
                    a.FromId == fromUuid &&
                    a.ToId == toUuid &&
                    a.RoleId == RoleConstants.Rightholder,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(revokedAssignment);

        var revokedAssignmentInstance1 = await db.AssignmentInstances
            .FirstOrDefaultAsync(
                ai =>
                    ai.AssignmentId == revokedAssignment.Id &&
                    ai.ResourceId == TestData.TestdirektoratetCorrespondenceService.Id &&
                    ai.InstanceId == InstanceUrn,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.Null(revokedAssignmentInstance1);

        var revokedAssignmentInstance2 = await db.AssignmentInstances
            .FirstOrDefaultAsync(
                ai =>
                    ai.AssignmentId == revokedAssignment.Id &&
                    ai.ResourceId == TestData.TestdirektoratetCorrespondenceService.Id &&
                    ai.InstanceId == instanceUrn2,
                cancellationToken: TestContext.Current.CancellationToken);
        Assert.NotNull(revokedAssignmentInstance2);
    }

    /// <summary>
    /// Test case: Revokes a non-existing delegation.
    /// Expected: Should still return 204 NoContent (idempotent operation).
    /// </summary>
    [Fact]
    public async Task RevokeInstance_ValidRequest_NonExistingDelegation_ReturnsNoContent()
    {
        // Arrange
        var fromUuid = TestData.HanSoloEnterprise.Id;
        var toUuid = TestData.LeiaOrgana.Id;
        var performedBy = TestData.HanSolo.Id;
        var resourceId = TestData.TestdirektoratetCorrespondenceService.RefId;

        var revokeRequest = new InstanceRevokeRequest
        {
            AuthorizationRuleID = 99999,
            Created = DateTimeOffset.UtcNow,
            FromUuid = fromUuid,
            ToUuid = toUuid,
            PerformedBy = performedBy,
            ResourceId = resourceId,
            InstanceId = InstanceId
        };

        var content = new StringContent(
            JsonSerializer.Serialize(revokeRequest, _jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = CreateClient();

        // Act
        var response = await client.PostAsync(RouteRevoke, content, TestContext.Current.CancellationToken);
        var responceContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    /// <summary>
    /// Test case: Attempts to revoke with an invalid (empty) resource ID.
    /// Expected: Returns 400 BadRequest with problem details.
    /// </summary>
    [Fact]
    public async Task RevokeInstance_InvalidResourceId_ReturnsBadRequest()
    {
        // Arrange
        var fromUuid = TestData.HanSoloEnterprise.Id;
        var toUuid = TestData.LeiaOrgana.Id;
        var performedBy = TestData.HanSolo.Id;
        var resourceId = TestData.MattilsynetBakeryService.RefId;

        var revokeRequest = new InstanceRevokeRequest
        {
            AuthorizationRuleID = 12345,
            Created = DateTimeOffset.UtcNow,
            FromUuid = fromUuid,
            ToUuid = toUuid,
            PerformedBy = performedBy,
            ResourceId = string.Empty, // Invalid resource ID
            InstanceId = InstanceId
        };

        var content = new StringContent(
            JsonSerializer.Serialize(revokeRequest, _jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = CreateClient();

        // Act
        var response = await client.PostAsync(RouteRevoke, content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test case: Attempts to revoke with an invalid (empty) FromUuid.
    /// Expected: Returns 400 BadRequest with problem details.
    /// </summary>
    [Fact]
    public async Task DelegateInstance_EmptyFromUuid_ReturnsBadRequest()
    {
        // Arrange
        var toUuid = TestData.LeiaOrgana.Id;
        var performedBy = TestData.HanSolo.Id;
        var resourceId = TestData.TestdirektoratetCorrespondenceService.RefId;

        var delegateRequest = new InstanceDelegationRequest
        {
            AuthorizationRuleID = 12345,
            Created = DateTimeOffset.UtcNow,
            FromUuid = Guid.Empty, // Invalid UUID
            ToUuid = toUuid,
            PerformedBy = performedBy,
            ResourceId = resourceId,
            InstanceId = InstanceId,
            Actions = new List<string> { "read", "subscribe" }

        };

        var content = new StringContent(
            JsonSerializer.Serialize(delegateRequest, _jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = CreateClient();

        // Act
        var response = await client.PostAsync(RouteDelegation, content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test case: Attempts to revoke without authentication token.
    /// Expected: Returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task RevokeInstance_WithNoToken_ReturnsUnauthorized()
    {
        // Arrange
        var fromUuid = TestData.HanSoloEnterprise;
        var toUuid = TestData.LeiaOrgana.Id;
        var performedBy = TestData.HanSolo.Id;
        var resourceId = TestData.TestdirektoratetCorrespondenceService.RefId;

        var revokeRequest = new InstanceRevokeRequest
        {
            AuthorizationRuleID = 12345,
            Created = DateTimeOffset.UtcNow,
            FromUuid = fromUuid,
            ToUuid = toUuid,
            PerformedBy = performedBy,
            ResourceId = resourceId,
            InstanceId = InstanceId
        };

        var content = new StringContent(
            JsonSerializer.Serialize(revokeRequest, _jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = Fixture.Server.CreateClient(); // No auth token

        // Act
        var response = await client.PostAsync(RouteRevoke, content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test case: Attempts to delegate without authentication token.
    /// Expected: Returns 401 Unauthorized.
    /// </summary>
    [Fact]
    public async Task DelegateInstance_WithNoToken_ReturnsUnauthorized()
    {
        // Arrange
        var fromUuid = TestData.HanSoloEnterprise;
        var toUuid = TestData.LeiaOrgana.Id;
        var performedBy = TestData.HanSolo.Id;
        var resourceId = TestData.TestdirektoratetCorrespondenceService.RefId;

        var delegationRequest = new InstanceDelegationRequest
        {
            AuthorizationRuleID = 12345,
            Created = DateTimeOffset.UtcNow,
            FromUuid = fromUuid,
            ToUuid = toUuid,
            PerformedBy = performedBy,
            ResourceId = resourceId,
            InstanceId = InstanceId,
            Actions = new List<string> { "read", "subscribe" }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(delegationRequest, _jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = Fixture.Server.CreateClient(); // No auth token

        // Act
        var response = await client.PostAsync(RouteDelegation, content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Test case: Attempts to Delegate with an empty action list
    /// Expected: Returns 400 BadRequest with problem details.
    /// </summary>
    [Fact]
    public async Task DelegateInstance_EmptyActionList_ReturnsBadRequest()
    {
        // Arrange
        var fromUuid = TestData.HanSoloEnterprise.Id;
        var toUuid = TestData.LeiaOrgana.Id;
        var performedBy = TestData.HanSolo.Id;
        var resourceId = TestData.TestdirektoratetCorrespondenceService.RefId;

        var delegationRequest = new InstanceDelegationRequest
        {
            AuthorizationRuleID = 12345,
            Created = DateTimeOffset.UtcNow,
            FromUuid = fromUuid,
            ToUuid = toUuid,
            PerformedBy = performedBy,
            ResourceId = resourceId,
            InstanceId = InstanceId,
            Actions = []
        };

        var content = new StringContent(
            JsonSerializer.Serialize(delegationRequest, _jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = CreateClient();

        // Act
        var response = await client.PostAsync(RouteDelegation, content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    /// <summary>
    /// Test case: Attempts to revoke with a missing instance id
    /// Expected: Returns 400 BadRequest with problem details.
    /// </summary>
    [Fact]
    public async Task DelegateInstance_EmptyIstanceId_ReturnsBadRequest()
    {
        // Arrange
        var fromUuid = TestData.HanSoloEnterprise.Id;
        var toUuid = TestData.LeiaOrgana.Id;
        var performedBy = TestData.HanSolo.Id;
        var resourceId = TestData.TestdirektoratetCorrespondenceService.RefId;

        var delegationRequest = new InstanceDelegationRequest
        {
            AuthorizationRuleID = 12345,
            Created = DateTimeOffset.UtcNow,
            FromUuid = fromUuid,
            ToUuid = toUuid,
            PerformedBy = performedBy,
            ResourceId = resourceId,
            Actions = new List<string> { "read", "subscribe" }
        };

        var content = new StringContent(
            JsonSerializer.Serialize(delegationRequest, _jsonOptions),
            Encoding.UTF8,
            MediaTypeNames.Application.Json);

        var client = CreateClient();

        // Act
        var response = await client.PostAsync(RouteDelegation, content, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
