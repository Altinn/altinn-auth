using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Partial test class for ConnectionsController, covering the fallback that resolves roles the
/// constants do not know about.
/// </summary>
public partial class ConnectionsControllerTest
{
    /// <summary>
    /// Connection enrichment resolves roles from <see cref="RoleConstants"/> rather than the
    /// database. StaticDataIngest never deletes, so a role dropped from the constants in an
    /// earlier release can still exist in the database and be referenced by an assignment made
    /// while it was current. Enrichment has to keep working for those rows.
    /// </summary>
    /// <remarks>
    /// Uses its own <see cref="ApiFixture"/> rather than the shared read-only collection, because
    /// it writes a role that deliberately has no counterpart in the constants.
    /// </remarks>
    [IntegrationTest]
    public class GetRolesRoleFallback : IClassFixture<ApiFixture>
    {
        private static readonly Guid StaleRoleId = new("0199d1c4-6f2a-7a10-9f3d-4c1e5b8a2d70");

        public GetRolesRoleFallback(ApiFixture fixture)
        {
            Fixture = fixture;
        }

        public ApiFixture Fixture { get; }

        [Fact]
        public async Task GetRoles_RoleMissingFromConstants_ResolvesFromDatabase()
        {
            // A role that exists only in the database, as one retired from the constants would be.
            Assert.False(RoleConstants.TryGetById(StaleRoleId, out _), "the fixture role must not be a constant, otherwise the fallback is never exercised");

            await Fixture.QueryDb(async db =>
            {
                db.Roles.Add(new Role
                {
                    Id = StaleRoleId,
                    Name = "Retired role",
                    Code = "retired-role",
                    Description = "Role kept in the database after being removed from RoleConstants.",
                    Urn = "urn:altinn:role:retired-role",
                    IsKeyRole = false,
                    IsAssignable = true,
                    IsAvailableForServiceOwners = false,
                    EntityTypeId = EntityTypeConstants.Organization,
                    ProviderId = ProviderConstants.Altinn3,
                });

                db.Assignments.Add(new Assignment
                {
                    FromId = TestData.HanSoloEnterprise.Id,
                    ToId = TestData.BenSolo.Id,
                    RoleId = StaleRoleId,
                });

                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            });

            HttpClient client = CreateClient(TestData.HanSolo.Id, AuthzConstants.SCOPE_ENDUSER_CONNECTIONS_TOOTHERS_READ);
            HttpResponseMessage response = await client.GetAsync(
                $"{Route}/roles?party={TestData.HanSoloEnterprise.Id}&from={TestData.HanSoloEnterprise.Id}&to={TestData.BenSolo.Id}",
                TestContext.Current.CancellationToken);

            string content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Body: {content}");

            var result = JsonSerializer.Deserialize<PaginatedResult<RolePermissionDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(result);

            var stale = Assert.Single(result.Items.Where(r => r.Role.Id == StaleRoleId));
            Assert.Equal("Retired role", stale.Role.Name);
            Assert.Equal("retired-role", stale.Role.Code);

            // The fallback loads the same graph the constants projection builds, so the provider
            // and its type must be filled in for a database-resolved role too.
            Assert.NotNull(stale.Role.Provider);
            Assert.Equal("sys-altinn3", stale.Role.Provider.Code);
            Assert.NotNull(stale.Role.Provider.Type);

            // Roles that do come from the constants must still resolve alongside it.
            Assert.Contains(result.Items, r => r.Role.Code == RoleConstants.ReporterSender.Entity.Code);
        }

        private HttpClient CreateClient(Guid partyUuid, params string[] scopes)
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, partyUuid.ToString()));
                claims.Add(new Claim("scope", string.Join(" ", scopes)));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }
    }
}
