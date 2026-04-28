using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.Register;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessManagement.ServiceOwner.Api.Tests.Controllers;

/// <summary>
/// Tests for <see cref="ConnectionsController"/> in the ServiceOwner API.
/// </summary>
public class ServiceOwnerConnectionsControllerTest
{
    public const string Route = "accessmanagement/api/v1/serviceowner/connections";

    #region POST accessmanagement/api/v1/serviceowner/connections/accesspackages

    /// <summary>
    /// Tests for <see cref="ConnectionsController.AddPackages(ServiceOwnerAccessPackageDelegation, CancellationToken)"/>
    /// </summary>
    public class AddRevokePackages : IClassFixture<ApiFixture>
    {
        public AddRevokePackages(ApiFixture fixture)
        {
            Fixture = fixture;

            // Configure the whitelist for the test service owner
            Fixture.WithInMemoryAppsettings(dict =>
            {
                dict[$"ServiceOwnerDelegation:PackageWhiteList:{TestData.StorMektigTenesteeier.Entity.OrganizationIdentifier}:0"] = PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Entity.Code;
                dict[$"ServiceOwnerDelegation:PackageWhiteList:{TestData.StorMektigTenesteeier.Entity.OrganizationIdentifier}:1"] = PackageConstants.InnbyggerBankFinans.Entity.Code;
                dict[$"ServiceOwnerDelegation:PackageWhiteList:{TestData.StorMektigTenesteeier.Entity.OrganizationIdentifier}:2"] = PackageConstants.Agriculture.Entity.Code;
            });

            Fixture.EnsureSeedOnce<AddRevokePackages>(db =>
            {
                // Seed any initial data needed for tests
                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.Org, "SKD"));
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_SERVICEOWNER_PACKAGE_DELEGATION_WRITE));
                claims.Add(new Claim("consumer", GetConsumerClaimJson(TestData.StorMektigTenesteeier.Entity.OrganizationIdentifier)));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        private static string GetConsumerClaimJson(string orgNumber)
        {
            return $$"""{ "authority":"iso6523-actorid-upis", "ID":"0192:{{orgNumber}}"}""";
        }

        [Fact]
        public async Task AddPackage_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var client = CreateClient();

            ServiceOwnerConnectionPartyUrn.PersonId from = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.VegardSolberg.Entity.PersonIdentifier));
            ServiceOwnerConnectionPartyUrn.PersonId to = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.IngerNygard.Entity.PersonIdentifier));
            AccessPackageUrn.AccessPackage package = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier(PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Entity.Code));

            ServiceOwnerAccessPackageDelegation request = new()
            {
                From = from,
                To = to,
                PackageUrn = package
            };

            // Act
            var response = await client.PostAsJsonAsync($"{Route}/accesspackages", request, TestContext.Current.CancellationToken);

            // Assert
            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify the assignment package was created in the database
            await Fixture.QueryDb(async db =>
            {
                var assignmentPackage = await db.AssignmentPackages
                    .Include(ap => ap.Assignment)
                    .Where(ap => ap.Assignment.FromId == TestData.VegardSolberg.Id)
                    .Where(ap => ap.Assignment.ToId == TestData.IngerNygard.Id)
                    .Where(ap => ap.PackageId == PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

                Assert.NotNull(assignmentPackage);
            });
        }

        [Fact]
        public async Task AddPackage_WithExistingAssignment_ReturnsExistingPackage()
        {
            // Arrange
            var client = CreateClient();

            // First, seed an existing assignment with package
            await Fixture.QueryDb(async db =>
            {
                var existingAssignment = new Assignment()
                {
                    FromId = TestData.BjornMoe.Id,
                    ToId = TestData.LarsBakke.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.Assignments.Add(existingAssignment);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);

                var existingPackage = new AssignmentPackage()
                {
                    AssignmentId = existingAssignment.Id,
                    PackageId = PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Id,
                };
                db.AssignmentPackages.Add(existingPackage);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            });

            ServiceOwnerConnectionPartyUrn.PersonId from = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.BjornMoe.Entity.PersonIdentifier));
            ServiceOwnerConnectionPartyUrn.PersonId to = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.LarsBakke.Entity.PersonIdentifier));
            AccessPackageUrn.AccessPackage package = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier(PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Entity.Code));

            ServiceOwnerAccessPackageDelegation request = new()
            {
                From = from,
                To = to,
                PackageUrn = package
            };

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync($"{Route}/accesspackages", request, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AddPackage_WithInvalidPackageUrn_ReturnsForbidden()
        {
            // Arrange
            var client = CreateClient();

            ServiceOwnerConnectionPartyUrn.PersonId from = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.BjornMoe.Entity.PersonIdentifier));
            ServiceOwnerConnectionPartyUrn.PersonId to = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.LarsBakke.Entity.PersonIdentifier));
            AccessPackageUrn.AccessPackage package = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier("nonexistent-package"));

            ServiceOwnerAccessPackageDelegation request = new()
            {
                From = from,
                To = to,
                PackageUrn = package
            };

            // Act
            var response = await client.PostAsJsonAsync($"{Route}/accesspackages", request, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AddPackage_WithFromAsOrganizationIdentifiersForPersonPackage_BadRequest()
        {
            // Arrange
            var client = CreateClient();

            ServiceOwnerConnectionPartyUrn.OrganizationId from = ServiceOwnerConnectionPartyUrn.OrganizationId.Create(OrganizationNumber.Parse(TestData.MittRegnskap.Entity.OrganizationIdentifier));
            ServiceOwnerConnectionPartyUrn.OrganizationId to = ServiceOwnerConnectionPartyUrn.OrganizationId.Create(OrganizationNumber.Parse(TestData.RpcAS.Entity.OrganizationIdentifier));
            AccessPackageUrn.AccessPackage package = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier(PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Entity.Code));

            ServiceOwnerAccessPackageDelegation request = new()
            {
                From = from,
                To = to,
                PackageUrn = package
            };

            // Act
            var response = await client.PostAsJsonAsync($"{Route}/accesspackages", request, TestContext.Current.CancellationToken);

            // Assert - The current implementation only handles person identifiers, so organization identifiers should return BadRequest
            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            
            // Update this assertion once organization support is added
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddPackage_ToOrganisation_ReturnsOk()
        {
            // Arrange
            var client = CreateClient();

            ServiceOwnerConnectionPartyUrn.PersonId from = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.VegardSolberg.Entity.PersonIdentifier));
            ServiceOwnerConnectionPartyUrn.OrganizationId to = ServiceOwnerConnectionPartyUrn.OrganizationId.Create(OrganizationNumber.Parse(TestData.SvendsenAutomobil.Entity.OrganizationIdentifier));
            AccessPackageUrn.AccessPackage package = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier(PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Entity.Code));

            ServiceOwnerAccessPackageDelegation request = new()
            {
                From = from,
                To = to,
                PackageUrn = package
            };

            // Act
            var response = await client.PostAsJsonAsync($"{Route}/accesspackages", request, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await Fixture.QueryDb(async db =>
            {
                var assignmentPackage = await db.AssignmentPackages
                    .Include(ap => ap.Assignment)
                    .Where(ap => ap.Assignment.FromId == TestData.VegardSolberg.Id)
                    .Where(ap => ap.Assignment.ToId == TestData.SvendsenAutomobil.Id)
                    .Where(ap => ap.PackageId == PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

                Assert.NotNull(assignmentPackage);
            });
        }

        [Fact]
        public async Task AddPackage_FromOrganisationOnPackageThatSupportsOrganisation_ReturnsOk()
        {
            // Arrange
            var client = CreateClient();

            ServiceOwnerConnectionPartyUrn.OrganizationId from = ServiceOwnerConnectionPartyUrn.OrganizationId.Create(OrganizationNumber.Parse(TestData.FredriksonsFabrikk.Entity.OrganizationIdentifier));
            ServiceOwnerConnectionPartyUrn.OrganizationId to = ServiceOwnerConnectionPartyUrn.OrganizationId.Create(OrganizationNumber.Parse(TestData.RegnskapNorge.Entity.OrganizationIdentifier));
            AccessPackageUrn.AccessPackage package = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier(PackageConstants.Agriculture.Entity.Code));

            ServiceOwnerAccessPackageDelegation request = new()
            {
                From = from,
                To = to,
                PackageUrn = package
            };

            // Act
            var response = await client.PostAsJsonAsync($"{Route}/accesspackages", request, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            await Fixture.QueryDb(async db =>
            {
                var assignmentPackage = await db.AssignmentPackages
                    .Include(ap => ap.Assignment)
                    .Where(ap => ap.Assignment.FromId == TestData.FredriksonsFabrikk.Id)
                    .Where(ap => ap.Assignment.ToId == TestData.RegnskapNorge.Id)
                    .Where(ap => ap.PackageId == PackageConstants.Agriculture.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

                Assert.NotNull(assignmentPackage);
            });
        }

        [Fact]
        public async Task RevokePackage_WhereAssignmentIsRevoked_ReturnsNoContent()
        {
            // Arrange - First create a package delegation, then revoke it
            var client = CreateClient();

            ServiceOwnerConnectionPartyUrn.PersonId from = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.SiljeHaugen.Entity.PersonIdentifier));
            ServiceOwnerConnectionPartyUrn.PersonId to = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.EinarBerg.Entity.PersonIdentifier));
            AccessPackageUrn.AccessPackage package = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier(PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Entity.Code));

            ServiceOwnerAccessPackageDelegation addRequest = new()
            {
                From = from,
                To = to,
                PackageUrn = package
            };

            // First add the package
            var addResponse = await client.PostAsJsonAsync($"{Route}/accesspackages", addRequest, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            // Act - Revoke the package
            var revokeResponse = await client.PostAsJsonAsync($"{Route}/accesspackages/revoke", addRequest, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

            await Fixture.QueryDb(async db =>
            {
                var assignment = await db.Assignments
                    .Where(ap => ap.FromId == TestData.SiljeHaugen.Id)
                    .Where(ap => ap.ToId == TestData.EinarBerg.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

                Assert.Null(assignment);
            });
        }

        [Fact]
        public async Task RevokePackage_WhereAssignmentIsNotRevoked_ReturnsNoContent()
        {
            // Arrange - Create an assignment with two packages via service owner, revoke one
            var client = CreateClient();

            ServiceOwnerConnectionPartyUrn.PersonId from = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.ToneKvam.Entity.PersonIdentifier));
            ServiceOwnerConnectionPartyUrn.PersonId to = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.ArneLund.Entity.PersonIdentifier));

            // Add first package
            ServiceOwnerAccessPackageDelegation addRequest1 = new()
            {
                From = from,
                To = to,
                PackageUrn = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier(PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Entity.Code))
            };

            var addResponse1 = await client.PostAsJsonAsync($"{Route}/accesspackages", addRequest1, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, addResponse1.StatusCode);

            // Add second package
            ServiceOwnerAccessPackageDelegation addRequest2 = new()
            {
                From = from,
                To = to,
                PackageUrn = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier(PackageConstants.InnbyggerBankFinans.Entity.Code))
            };

            var addResponse2 = await client.PostAsJsonAsync($"{Route}/accesspackages", addRequest2, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, addResponse2.StatusCode);

            // Act - Revoke the first package (assignment should still exist if there were other packages)
            var revokeResponse = await client.PostAsJsonAsync($"{Route}/accesspackages/revoke", addRequest1, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

            await Fixture.QueryDb(async db =>
            {
                var assignmentPackage = await db.AssignmentPackages
                    .Include(ap => ap.Assignment)
                    .Where(ap => ap.Assignment.FromId == TestData.ToneKvam.Id)
                    .Where(ap => ap.Assignment.ToId == TestData.ArneLund.Id)
                    .Where(ap => ap.PackageId == PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

                Assert.Null(assignmentPackage);
            });

            await Fixture.QueryDb(async db =>
            {
                var assignmentPackage = await db.AssignmentPackages
                    .Include(ap => ap.Assignment)
                    .Where(ap => ap.Assignment.FromId == TestData.ToneKvam.Id)
                    .Where(ap => ap.Assignment.ToId == TestData.ArneLund.Id)
                    .Where(ap => ap.PackageId == PackageConstants.InnbyggerBankFinans.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

                Assert.NotNull(assignmentPackage);
            });
        }

        [Fact]
        public async Task RevokePackage_WhereAssignmentDoesNotExist_ReturnsNoContent()
        {
            // Arrange
            var client = CreateClient();

            ServiceOwnerConnectionPartyUrn.PersonId from = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.OddHalvorsen.Entity.PersonIdentifier));
            ServiceOwnerConnectionPartyUrn.PersonId to = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.LivKristiansen.Entity.PersonIdentifier));
            AccessPackageUrn.AccessPackage package = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier(PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Entity.Code));

            ServiceOwnerAccessPackageDelegation request = new()
            {
                From = from,
                To = to,
                PackageUrn = package
            };

            // Act
            var response = await client.PostAsJsonAsync($"{Route}/accesspackages/revoke", request, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task RevokePackage_WhereAssignmentPackageDoesNotExist_ReturnsNoContent()
        {
            // Arrange - Create an assignment with one package, then try to revoke a different package
            var client = CreateClient();

            ServiceOwnerConnectionPartyUrn.PersonId from = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.HelgeNilsen.Entity.PersonIdentifier));
            ServiceOwnerConnectionPartyUrn.PersonId to = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.SteinarAndreassen.Entity.PersonIdentifier));

            // Add a package to create an assignment
            ServiceOwnerAccessPackageDelegation addRequest = new()
            {
                From = from,
                To = to,
                PackageUrn = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier(PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Entity.Code))
            };
            var addResponse = await client.PostAsJsonAsync($"{Route}/accesspackages", addRequest, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);

            ServiceOwnerAccessPackageDelegation revokeRequest = new()
            {
                From = from,
                To = to,
                PackageUrn = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier(PackageConstants.InnbyggerBankFinans.Entity.Code))
            };

            // Act
            var revokeResponse = await client.PostAsJsonAsync($"{Route}/accesspackages/revoke", revokeRequest, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, revokeResponse.StatusCode);

            await Fixture.QueryDb(async db =>
            {
                var assignmentPackage = await db.AssignmentPackages
                    .Include(ap => ap.Assignment)
                    .Where(ap => ap.Assignment.FromId == TestData.HelgeNilsen.Id)
                    .Where(ap => ap.Assignment.ToId == TestData.SteinarAndreassen.Id)
                    .Where(ap => ap.PackageId == PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Id)
                    .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

                Assert.NotNull(assignmentPackage);
            });
        }

        [Fact]
        public async Task RevokePackage_WhereAssignmentWasNotPerformedByServiceOwner_ReturnsBadRequest()
        {
            // Arrange - Seed an assignment+package that was NOT created by the service owner
            var client = CreateClient();

            await Fixture.QueryDb(async db =>
            {
                var assignment = new Assignment()
                {
                    FromId = TestData.GeirPedersen.Id,
                    ToId = TestData.MaritEriksen.Id,
                    RoleId = RoleConstants.Rightholder,
                };
                db.Assignments.Add(assignment);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);

                var pkg = new AssignmentPackage()
                {
                    AssignmentId = assignment.Id,
                    PackageId = PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Id,
                };
                db.AssignmentPackages.Add(pkg);
                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            });

            ServiceOwnerConnectionPartyUrn.PersonId from = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.GeirPedersen.Entity.PersonIdentifier));
            ServiceOwnerConnectionPartyUrn.PersonId to = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.MaritEriksen.Entity.PersonIdentifier));
            AccessPackageUrn.AccessPackage package = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier(PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Entity.Code));

            ServiceOwnerAccessPackageDelegation request = new()
            {
                From = from,
                To = to,
                PackageUrn = package
            };

            // Act
            var response = await client.PostAsJsonAsync($"{Route}/accesspackages/revoke", request, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(TestContext.Current.CancellationToken);
            Assert.Equal("AM-00038", problemDetails.Extensions["code"].ToString());
        }

        [Fact]
        public async Task AddPackage_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var client = Fixture.Server.CreateClient(); // No auth token

            ServiceOwnerConnectionPartyUrn.PersonId from = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.BjornMoe.Entity.PersonIdentifier));
            ServiceOwnerConnectionPartyUrn.PersonId to = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.LarsBakke.Entity.PersonIdentifier));
            AccessPackageUrn.AccessPackage package = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier(PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Entity.Code));

            ServiceOwnerAccessPackageDelegation request = new()
            {
                From = from,
                To = to,
                PackageUrn = package
            };

            // Act
            var response = await client.PostAsJsonAsync($"{Route}/accesspackages", request, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AddPackage_WithWrongScope_ReturnsForbidden()
        {
            // Arrange
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestData.MittRegnskap.Id.ToString()));
                claims.Add(new Claim("scope", "some:other:scope")); // Wrong scope
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            ServiceOwnerConnectionPartyUrn.PersonId from = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.BjornMoe.Entity.PersonIdentifier));
            ServiceOwnerConnectionPartyUrn.PersonId to = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.LarsBakke.Entity.PersonIdentifier));
            AccessPackageUrn.AccessPackage package = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier(PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Entity.Code));

            ServiceOwnerAccessPackageDelegation request = new()
            {
                From = from,
                To = to,
                PackageUrn = package
            };

            // Act
            var response = await client.PostAsJsonAsync($"{Route}/accesspackages", request, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AddPackage_WithPackageNotInWhitelist_ReturnsForbidden()
        {
            // Arrange
            var client = CreateClient();

            ServiceOwnerConnectionPartyUrn.PersonId from = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.BjornMoe.Entity.PersonIdentifier));
            ServiceOwnerConnectionPartyUrn.PersonId to = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.LarsBakke.Entity.PersonIdentifier));
            AccessPackageUrn.AccessPackage package = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier("package-not-in-whitelist"));

            ServiceOwnerAccessPackageDelegation request = new()
            {
                From = from,
                To = to,
                PackageUrn = package
            };

            // Act
            var response = await client.PostAsJsonAsync($"{Route}/accesspackages", request, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task AddPackage_WithServiceOwnerNotInWhitelist_ReturnsForbidden()
        {
            // Arrange - Create client with a different organization that's not in the whitelist
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.Org, "OTHER"));
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_SERVICEOWNER_PACKAGE_DELEGATION_WRITE));
                claims.Add(new Claim("consumer", GetConsumerClaimJson(TestData.BakerJohnsen.Entity.OrganizationIdentifier))); // Not in whitelist
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            ServiceOwnerConnectionPartyUrn.PersonId from = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.BjornMoe.Entity.PersonIdentifier));
            ServiceOwnerConnectionPartyUrn.PersonId to = ServiceOwnerConnectionPartyUrn.PersonId.Create(PersonIdentifier.Parse(TestData.LarsBakke.Entity.PersonIdentifier));
            AccessPackageUrn.AccessPackage package = AccessPackageUrn.AccessPackage.Create(new AccessPackageIdentifier(PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Entity.Code));

            ServiceOwnerAccessPackageDelegation request = new()
            {
                From = from,
                To = to,
                PackageUrn = package
            };

            // Act
            var response = await client.PostAsJsonAsync($"{Route}/accesspackages", request, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    #endregion
}
