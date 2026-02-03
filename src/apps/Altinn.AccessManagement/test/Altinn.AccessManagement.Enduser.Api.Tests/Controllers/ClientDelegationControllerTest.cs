using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;
using Altinn.AccessManagement.TestUtils.Fixtures;
using Altinn.AccessMgmt.Core;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Controllers;

public class ClientDelegationControllerTest
{
    public const string Route = "accessmanagement/api/v1/enduser/clientdelegations";

    #region GET accessmanagement/api/v1/enduser/clientdelegations/clients

    /// <summary>
    /// <see cref="ClientDelegationController.GetClients(Guid, AccessManagement.Api.Enduser.Models.PagingInput, CancellationToken)"/>
    /// </summary>
    public class GetClients : IClassFixture<ApiFixture>
    {
        public GetClients(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerClientDelegation);
            Fixture.EnsureSeedOnce(db =>
            {
                var rightholderfromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                var accountantFromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Accountant,
                };
                var agentFromPaulaToNordis = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                };

                db.Assignments.Add(rightholderfromNordisToVerdiq);
                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(agentFromPaulaToNordis);

                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderfromNordisToVerdiq.Id,
                    PackageId = PackageConstants.Customs,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact(Skip = "PDP returns 500 if party is missing")]
        public async Task ListClient_ForOrganization_MissingQueryParamPartyBadRequest()
        {
            var client = CreateClient();

            var result = await client.GetAsync($"{Route}/clients", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task ListClient_ForOrganizationWithCCR_ReturnsOk()
        {
            var rolepkgs = new List<RolePackage>();
            await Fixture.QueryDb(async db =>
            {
                rolepkgs = await db.RolePackages
                    .Where(rp => rp.RoleId == RoleConstants.Accountant.Id)
                    .ToListAsync();

                await db.SaveChangesAsync(TestContext.Current.CancellationToken);
            });

            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<Authorization.Api.Contracts.AccessManagement.ClientDto>>(data);

            var connection = result.Items.SelectMany(p => p.Access).FirstOrDefault(a => a.Role.Id == RoleConstants.Accountant);
            Assert.NotNull(connection);
            Assert.Equal(RoleConstants.Accountant.Id, connection.Role.Id);
            foreach (var rolePkg in rolepkgs)
            {
                Assert.Contains(rolePkg.PackageId, connection.Packages.Select(p => p.Id));
            }
        }

        [Fact]
        public async Task ListClient_ForOrganizationWithRightholderFilter_ReturnsOk()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}&roles=rettighetshaver", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<Authorization.Api.Contracts.AccessManagement.ClientDto>>(data);
            Assert.NotEmpty(result.Items);
            foreach (var item in result.Items)
            {
                Assert.NotEmpty(item.Access);
                foreach (var role in item.Access)
                {
                    Assert.Equal(RoleConstants.Rightholder.Id, role.Role.Id);
                }
            }
        }

        [Fact]
        public async Task ListClient_ForOrganizationWithRightholderAssignment_ReturnsOk()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/clients?party={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<Authorization.Api.Contracts.AccessManagement.ClientDto>>(data);

            var connection = result.Items.FirstOrDefault(p => p.Client.Id == TestEntities.OrganizationNordisAS.Id);
            Assert.NotNull(connection);
            Assert.Equal(connection.Client.Id, TestEntities.OrganizationNordisAS.Id);

            var access = connection.Access.FirstOrDefault(a => a.Role.Id == RoleConstants.Rightholder);
            Assert.NotNull(access);
            Assert.Equal(RoleConstants.Rightholder.Id, access.Role.Id);
            Assert.Equal(PackageConstants.Customs.Id, access.Packages.First().Id);
            Assert.Equal(PackageConstants.Customs.Entity.Urn, access.Packages.First().Urn);
            Assert.Equal(PackageConstants.Customs.Entity.AreaId, access.Packages.First().AreaId);
        }
    }
    #endregion

    #region GET accessmanagement/api/v1/enduser/clientdelegations/agents

    /// <summary>
    /// <see cref="ClientDelegationController.GetAgents(Guid, AccessManagement.Api.Enduser.Models.PagingInput, CancellationToken)"/>
    /// </summary>
    public class GetAgents : IClassFixture<ApiFixture>
    {
        public GetAgents(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerClientDelegation);
            Fixture.EnsureSeedOnce(db =>
            {
                var rightholderfromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                var accountantFromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Accountant,
                };
                var agentFromPaulaToNordis = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                };

                db.Assignments.Add(rightholderfromNordisToVerdiq);
                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(agentFromPaulaToNordis);

                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderfromNordisToVerdiq.Id,
                    PackageId = PackageConstants.Customs,
                });

                db.SaveChanges();
            });
        }

        private HttpClient CreateClient()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        public ApiFixture Fixture { get; }

        [Fact(Skip = "PDP returns 500 if party is missing")]
        public async Task ListAgents_ForOrganization_MissingQueryParamPartyBadRequest()
        {
            var client = CreateClient();

            var response = await client.GetAsync($"{Route}/clients", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ListAgent_ForPersonWithAgentAssignment_ReturnsOk()
        {
            var client = CreateClient();
            var response = await client.GetAsync($"{Route}/agents?party={TestEntities.OrganizationNordisAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<AgentDto>>(data);

            var connection = result.Items.FirstOrDefault(p => p.Agent.Id == TestEntities.PersonPaula);
            Assert.NotNull(connection);
            Assert.Equal(TestEntities.PersonPaula.Id, connection.Agent.Id);

            var access = connection.Access.FirstOrDefault(r => r.Role.Id == RoleConstants.Agent);
            Assert.NotNull(access);
            Assert.Empty(access.Packages);
        }

        [Fact]
        public async Task ListAgent_ForPersonWithAgentAssignmentToAnotherOrganization_ReturnsOk()
        {
            var client = CreateClient();
            var response = await client.GetAsync($"{Route}/agents?party={TestEntities.OrganizationVerdiqAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<AgentDto>>(data);

            var connection = result.Items.FirstOrDefault(p => p.Agent.Id == TestEntities.PersonPaula);
            Assert.Null(connection);
        }

        [Fact]
        public async Task ListAgent_ForNoAgentAssignment_ReturnsOk()
        {
            var client = CreateClient();
            var response = await client.GetAsync($"{Route}/agents?party={TestEntities.OrganizationNordisAS.Id}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var result = JsonSerializer.Deserialize<PaginatedResult<AgentDto>>(data);

            var connection = result.Items.FirstOrDefault(p => p.Agent.Id == TestEntities.PersonOrjan);
            Assert.Null(connection);
        }
    }
    #endregion

    #region POST accessmanagement/api/v1/enduser/clientdelegations/agents

    /// <summary>
    /// <see cref="ClientDelegationController.AddAgent(Guid, Guid?, AccessManagement.Api.Enduser.Models.PersonInput?, CancellationToken)"/>
    /// </summary>
    public class AddAgent : IClassFixture<ApiFixture>
    {
        public AddAgent(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerClientDelegation);
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim("scope", $"{AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE} {AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ}"));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }
    }
    #endregion

    #region DELETE accessmanagement/api/v1/enduser/clientdelegations/agents

    /// <summary>
    /// <see cref="ClientDelegationController.RemoveAgent(Guid, Guid, bool, CancellationToken)"/>
    /// </summary>
    public class DeleteAgent : IClassFixture<ApiFixture>
    {
        public DeleteAgent(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerClientDelegation);
            Fixture.EnsureSeedOnce(db =>
            {
                var rightholderfromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                var accountantFromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Accountant,
                };
                var agentFromVerdiqToPaula = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                };

                db.Assignments.Add(rightholderfromNordisToVerdiq);
                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(agentFromVerdiqToPaula);

                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderfromNordisToVerdiq.Id,
                    PackageId = PackageConstants.Customs.Id,
                });

                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderfromNordisToVerdiq.Id,
                    PackageId = PackageConstants.ExplicitServiceDelegation.Id,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestEntities.PersonPaula.Id.ToString()));
                claims.Add(new Claim("scope", $"{AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE} {AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ}"));
            });

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task RemoveAgentWithExistingDelegations_WithCascadeFalse_ReturnsBadRequest()
        {
            // Create Delegation
            var client = CreateClient();
            var response = await client.PostAsJsonAsync(
                $"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&from={TestEntities.OrganizationNordisAS}&to={TestEntities.PersonPaula}",
                new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Rightholder.Entity.Code,
                            Packages = [PackageConstants.Customs.Entity.Urn]
                        }
                    ]
                },
                TestContext.Current.CancellationToken
            );

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify Delegation exists
            var getDelegationsToAgent = await client.GetAsync($"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&to={TestEntities.PersonPaula}", TestContext.Current.CancellationToken);
            var delegationsToAgentPayload = await getDelegationsToAgent.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var delegationToAgentResult = JsonSerializer.Deserialize<PaginatedResult<Authorization.Api.Contracts.AccessManagement.ClientDto>>(delegationsToAgentPayload);

            Assert.NotEmpty(delegationToAgentResult.Items);

            // Delete Delegation without cascade
            var deleteResponse = await client.DeleteAsync($"{Route}/agents?party={TestEntities.OrganizationVerdiqAS}&to={TestEntities.PersonPaula}&cascade=false", TestContext.Current.CancellationToken);
            var deleteResponsePayload = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);

            // Ensure proper error returned
            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(deleteResponsePayload);
            Assert.Single(problem.Errors);
            Assert.All(problem.Errors, error =>
            {
                Assert.Equal(ValidationErrors.DelegationHasActiveConnections.ErrorCode, error.ErrorCode);
            });

            // Delete with cascade true
            deleteResponse = await client.DeleteAsync($"{Route}/agents?party={TestEntities.OrganizationVerdiqAS}&to={TestEntities.PersonPaula}&cascade=true", TestContext.Current.CancellationToken);
            deleteResponsePayload = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Ensure Agent is deleted
            var getAgents = await client.GetAsync($"{Route}/agents?party={TestEntities.OrganizationVerdiqAS}", TestContext.Current.CancellationToken);
            var getAgentsPayload = await getAgents.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var agentResult = JsonSerializer.Deserialize<PaginatedResult<Authorization.Api.Contracts.AccessManagement.ClientDto>>(getAgentsPayload);

            Assert.Equal(HttpStatusCode.OK, getAgents.StatusCode);
            Assert.Empty(agentResult.Items);
        }
    }
    #endregion

    #region GET accessmanagement/api/v1/enduser/clientdelegations/clients/accesspackages
    #endregion

    #region GET accessmanagement/api/v1/enduser/clientdelegations/agents/accesspackages
    #endregion

    #region POST accessmanagement/api/v1/enduser/clientdelegations/agents/accesspackages

    /// <summary>
    /// <see cref="ClientDelegationController.DelegateAccessPackageToAgent(Guid, Guid, Guid, DelegationBatchInputDto, CancellationToken)"/>
    /// </summary>
    public class DelegateAccessPackageToAgentWithAgentRole : IClassFixture<ApiFixture>
    {
        public DelegateAccessPackageToAgentWithAgentRole(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerClientDelegation);
            Fixture.EnsureSeedOnce(db =>
            {
                var rightholderfromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                var accountantFromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Accountant,
                };
                var agentFromVerdiqToPaula = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                };

                db.Assignments.Add(rightholderfromNordisToVerdiq);
                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(agentFromVerdiqToPaula);

                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderfromNordisToVerdiq.Id,
                    PackageId = PackageConstants.Customs.Id,
                });

                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderfromNordisToVerdiq.Id,
                    PackageId = PackageConstants.ExplicitServiceDelegation.Id,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestEntities.PersonPaula.Id.ToString()));
                claims.Add(new Claim("scope", $"{AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ} {AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE}"));
            });

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task DelegateAccessPackageToAgent_WithValidInput_ReturnsOk()
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync(
                $"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&from={TestEntities.OrganizationNordisAS}&to={TestEntities.PersonPaula}",
                new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Rightholder.Entity.Code,
                            Packages = [PackageConstants.Customs.Entity.Urn]
                        }
                    ]
                },
                TestContext.Current.CancellationToken
            );

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var getDelegationsToAgent = await client.GetAsync($"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&to={TestEntities.PersonPaula}", TestContext.Current.CancellationToken);
            var delegationsToAgentPayload = await getDelegationsToAgent.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var delegationToAgentResult = JsonSerializer.Deserialize<PaginatedResult<Authorization.Api.Contracts.AccessManagement.ClientDto>>(delegationsToAgentPayload);

            Assert.NotEmpty(delegationToAgentResult.Items);

            var agentAccess = delegationToAgentResult.Items.FirstOrDefault();
            Assert.Equal(TestEntities.OrganizationNordisAS.Id, agentAccess.Client.Id);
            Assert.Equal(RoleConstants.Rightholder.Entity.Code, agentAccess.Access.FirstOrDefault()?.Role?.Code);
            Assert.Equal(PackageConstants.Customs.Entity.Urn, agentAccess.Access.FirstOrDefault()?.Packages?.FirstOrDefault().Urn);

            var getDelegationFromClient = await client.GetAsync($"{Route}/clients/accesspackages?party={TestEntities.OrganizationVerdiqAS.Id}&from={TestEntities.OrganizationNordisAS.Id}", TestContext.Current.CancellationToken);
            var delegationsFromClientPayload = await getDelegationFromClient.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var delegationsFromClientResult = JsonSerializer.Deserialize<PaginatedResult<AgentDto>>(delegationsFromClientPayload);

            Assert.NotEmpty(delegationsFromClientResult.Items);

            var accessToClient = delegationsFromClientResult.Items.FirstOrDefault();
            Assert.Equal(TestEntities.PersonPaula.Id, accessToClient.Agent.Id);
            Assert.Equal(RoleConstants.Rightholder.Entity.Code, accessToClient.Access.FirstOrDefault()?.Role?.Code);
            Assert.Equal(PackageConstants.Customs.Entity.Urn, accessToClient.Access.FirstOrDefault()?.Packages?.FirstOrDefault().Urn);
        }

        [Fact]
        public async Task DelegateAccessPackageToAgent_WithPackageClientHaventDelegatedToParty_ReturnsBadRequest()
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync(
                $"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&from={TestEntities.OrganizationNordisAS}&to={TestEntities.PersonPaula}",
                new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Rightholder.Entity.Code,
                            Packages = [
                                PackageConstants.Accident.Entity.Urn,
                                PackageConstants.CreditAndSettlementArrangements.Entity.Urn,
                            ]
                        }
                    ]
                },
                TestContext.Current.CancellationToken
            );

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(data);

            Assert.Equal(2, problem.Errors.Count);
            Assert.All(problem.Errors, error =>
            {
                Assert.Equal(ValidationErrors.UserNotAuthorized.ErrorCode, error.ErrorCode);
            });
        }

        [Fact]
        public async Task DelegateAccessPackageToAgent_WithCanDelegateFalse_ReturnsBadRequest()
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync(
                $"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&from={TestEntities.OrganizationNordisAS}&to={TestEntities.PersonPaula}",
                new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Rightholder.Entity.Code,
                            Packages = [
                                PackageConstants.AccessManager.Entity.Urn,
                                PackageConstants.DelegableMaskinportenScopes.Entity.Urn,
                            ]
                        }
                    ]
                },
                TestContext.Current.CancellationToken
            );

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(data);

            Assert.Equal(2, problem.Errors.Count);
            Assert.All(problem.Errors, error =>
            {
                Assert.Equal(ValidationErrors.PackageIsNotDelegable.ErrorCode, error.ErrorCode);
            });
        }
    }

    public class DelegateAccessPackageToAgentWithCCRRole : IClassFixture<ApiFixture>
    {
        public DelegateAccessPackageToAgentWithCCRRole(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerClientDelegation);
            Fixture.EnsureSeedOnce(db =>
            {
                var accountantFromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Accountant,
                };
                var agentFromVerdiqToPaula = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                };
                var agentFromVerdiqToOrjan = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonOrjan,
                    RoleId = RoleConstants.Agent,
                };

                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(agentFromVerdiqToPaula);
                db.Assignments.Add(agentFromVerdiqToOrjan);

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestEntities.PersonPaula.Id.ToString()));
                claims.Add(new Claim("scope", $"{AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ} {AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE}"));
            });

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task DelegateAccessPackageToAgent_WithValidInput_ReturnsOk()
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync(
                $"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&from={TestEntities.OrganizationNordisAS}&to={TestEntities.PersonPaula}",
                new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Accountant.Entity.Code,
                            Packages = [
                                PackageConstants.AccountantWithSigningRights.Entity.Urn,
                                PackageConstants.AccountantWithoutSigningRights.Entity.Urn
                            ]
                        }
                    ]
                },
                TestContext.Current.CancellationToken
            );

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            response = await client.PostAsJsonAsync(
                $"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&from={TestEntities.OrganizationNordisAS}&to={TestEntities.PersonOrjan}",
                new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                            {
                                Role = RoleConstants.Accountant.Entity.Code,
                                Packages = [
                                    PackageConstants.AccountantWithSigningRights.Entity.Urn,
                                ]
                            }
                    ]
                },
                TestContext.Current.CancellationToken
            );

            data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var getDelegationsToAgent = await client.GetAsync($"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&to={TestEntities.PersonOrjan}", TestContext.Current.CancellationToken);
            var delegationsToAgentPayload = await getDelegationsToAgent.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var delegationToAgentResult = JsonSerializer.Deserialize<PaginatedResult<Authorization.Api.Contracts.AccessManagement.ClientDto>>(delegationsToAgentPayload);

            Assert.NotEmpty(delegationToAgentResult.Items);

            var orgs = delegationToAgentResult.Items.FirstOrDefault();
            Assert.Single(orgs.Access);
            Assert.Single(orgs.Access.FirstOrDefault()?.Packages);
            Assert.Equal(TestEntities.OrganizationNordisAS.Id, orgs.Client.Id);
            Assert.Equal(RoleConstants.Accountant.Entity.Code, orgs.Access.FirstOrDefault()?.Role?.Code);
            Assert.Equal(PackageConstants.AccountantWithSigningRights.Entity.Urn, orgs.Access.FirstOrDefault()?.Packages?.FirstOrDefault().Urn);

            var getDelegationFromClient = await client.GetAsync($"{Route}/clients/accesspackages?party={TestEntities.OrganizationVerdiqAS.Id}&from={TestEntities.OrganizationNordisAS.Id}", TestContext.Current.CancellationToken);
            var delegationsFromClientPayload = await getDelegationFromClient.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var delegationsFromClientResult = JsonSerializer.Deserialize<PaginatedResult<AgentDto>>(delegationsFromClientPayload);

            Assert.NotEmpty(delegationsFromClientResult.Items);

            var paulasAccess = delegationsFromClientResult.Items.FirstOrDefault(p => p.Agent.Id == TestEntities.PersonPaula);
            var orjanAccess = delegationsFromClientResult.Items.FirstOrDefault(p => p.Agent.Id == TestEntities.PersonOrjan);
            Assert.NotNull(paulasAccess);
            Assert.NotNull(orjanAccess);

            Assert.Single(paulasAccess.Access);
            Assert.Equal(2, paulasAccess.Access.FirstOrDefault()?.Packages.Count());

            Assert.Single(orjanAccess.Access);
            Assert.Single(orjanAccess.Access.FirstOrDefault()?.Packages);
        }

        [Fact]
        public async Task DelegateAccessPackageToAgent_WithPackageClientHaventDelegatedToParty_ReturnsBadRequest()
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync(
                $"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&from={TestEntities.OrganizationNordisAS}&to={TestEntities.PersonPaula}",
                new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Accountant.Entity.Code,
                            Packages = [
                                PackageConstants.Accident.Entity.Urn,
                                PackageConstants.CreditAndSettlementArrangements.Entity.Urn,
                            ]
                        }
                    ]
                },
                TestContext.Current.CancellationToken
            );

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(data);

            Assert.Equal(2, problem.Errors.Count);
            Assert.All(problem.Errors, error =>
            {
                Assert.Equal(ValidationErrors.UserNotAuthorized.ErrorCode, error.ErrorCode);
            });
        }

        [Fact]
        public async Task DelegateAccessPackageToAgent_WithCanDelegateFalse_ReturnsBadRequest()
        {
            var client = CreateClient();
            var response = await client.PostAsJsonAsync(
                $"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&from={TestEntities.OrganizationNordisAS}&to={TestEntities.PersonPaula}",
                new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Accountant.Entity.Code,
                            Packages = [
                                PackageConstants.AccessManager.Entity.Urn,
                                PackageConstants.DelegableMaskinportenScopes.Entity.Urn,
                            ]
                        }
                    ]
                },
                TestContext.Current.CancellationToken
            );

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var problem = JsonSerializer.Deserialize<AltinnValidationProblemDetails>(data);

            Assert.Equal(2, problem.Errors.Count);
            Assert.All(problem.Errors, error =>
            {
                Assert.Equal(ValidationErrors.PackageIsNotDelegable.ErrorCode, error.ErrorCode);
            });
        }
    }

    #endregion

    #region DELETE accessmanagement/api/v1/enduser/clientdelegations/agents/accesspackages

    /// <summary>
    /// <see cref="ClientDelegationController.DeleteAgentAccessPackage(Guid, Guid, Guid, DelegationBatchInputDto, CancellationToken)"/>
    /// </summary>
    public class DeleteAgentAccessPackage : IClassFixture<ApiFixture>
    {
        public DeleteAgentAccessPackage(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithEnabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerClientDelegation);
            Fixture.EnsureSeedOnce(db =>
            {
                var rightholderfromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Rightholder,
                };

                var accountantFromNordisToVerdiq = new Assignment()
                {
                    FromId = TestEntities.OrganizationNordisAS.Id,
                    ToId = TestEntities.OrganizationVerdiqAS.Id,
                    RoleId = RoleConstants.Accountant,
                };
                var agentFromVerdiqToPaula = new Assignment()
                {
                    FromId = TestEntities.OrganizationVerdiqAS.Id,
                    ToId = TestEntities.PersonPaula,
                    RoleId = RoleConstants.Agent,
                };

                db.Assignments.Add(rightholderfromNordisToVerdiq);
                db.Assignments.Add(accountantFromNordisToVerdiq);
                db.Assignments.Add(agentFromVerdiqToPaula);

                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderfromNordisToVerdiq.Id,
                    PackageId = PackageConstants.Customs.Id,
                });

                db.AssignmentPackages.Add(new()
                {
                    AssignmentId = rightholderfromNordisToVerdiq.Id,
                    PackageId = PackageConstants.ExplicitServiceDelegation.Id,
                });

                db.SaveChanges();
            });
        }

        public ApiFixture Fixture { get; }

        private HttpClient CreateClient()
        {
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim(AltinnCoreClaimTypes.PartyUuid, TestEntities.PersonPaula.Id.ToString()));
                claims.Add(new Claim("scope", $"{AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ} {AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_WRITE}"));
            });

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
            return client;
        }

        [Fact]
        public async Task DelegateAccessPackageToAgent_WithValidInput_ReturnsOk()
        {
            // Create Delegation
            var client = CreateClient();
            var response = await client.PostAsJsonAsync(
                $"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&from={TestEntities.OrganizationNordisAS}&to={TestEntities.PersonPaula}",
                new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Rightholder.Entity.Code,
                            Packages = [PackageConstants.Customs.Entity.Urn]
                        }
                    ]
                },
                TestContext.Current.CancellationToken
            );

            var data = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify Delegation exists
            var getDelegationsToAgent = await client.GetAsync($"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&to={TestEntities.PersonPaula}", TestContext.Current.CancellationToken);
            var delegationsToAgentPayload = await getDelegationsToAgent.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            var delegationToAgentResult = JsonSerializer.Deserialize<PaginatedResult<Authorization.Api.Contracts.AccessManagement.ClientDto>>(delegationsToAgentPayload);

            Assert.NotEmpty(delegationToAgentResult.Items.FirstOrDefault()?.Access.FirstOrDefault()?.Packages);

            // Delete Delegation
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&from={TestEntities.OrganizationNordisAS}&to={TestEntities.PersonPaula}")
            {
                Content = JsonContent.Create(new DelegationBatchInputDto()
                {
                    Values = [
                        new()
                        {
                            Role = RoleConstants.Rightholder.Entity.Code,
                            Packages = [PackageConstants.Customs.Entity.Urn]
                        }
                    ]
                })
            };

            var deleteResponse = await client.SendAsync(request, TestContext.Current.CancellationToken);
            var deleteResponsePayload = await deleteResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
            var deleteResult = JsonSerializer.Deserialize<List<DelegationDto>>(deleteResponsePayload);

            Assert.NotEmpty(deleteResult);

            // Verify Delegation deleted
            getDelegationsToAgent = await client.GetAsync($"{Route}/agents/accesspackages?party={TestEntities.OrganizationVerdiqAS}&to={TestEntities.PersonPaula}", TestContext.Current.CancellationToken);
            delegationsToAgentPayload = await getDelegationsToAgent.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            delegationToAgentResult = JsonSerializer.Deserialize<PaginatedResult<Authorization.Api.Contracts.AccessManagement.ClientDto>>(delegationsToAgentPayload);

            Assert.Empty(delegationToAgentResult.Items);
        }
    }
    #endregion

    #region Feature Flag Disabled Tests
    public class FeatureFlagClientDelegationEnabledCollection : IClassFixture<ApiFixture>
    {
        public FeatureFlagClientDelegationEnabledCollection(ApiFixture fixture)
        {
            Fixture = fixture;
            Fixture.WithDisabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerClientDelegation);
        }

        [Fact]
        public async Task ListClient_WithDisabledFeatureFlag_ReturnsNotFound()
        {
            Fixture.WithDisabledFeatureFlag(AccessMgmtFeatureFlags.EnduserControllerClientDelegation);
            var client = Fixture.Server.CreateClient();
            var token = TestTokenGenerator.CreateToken(new ClaimsIdentity("mock"), claims =>
            {
                claims.Add(new Claim("scope", AuthzConstants.SCOPE_ENDUSER_CLIENTDELEGATION_READ));
            });
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");

            var result = await client.GetAsync($"{Route}/clients?party={Guid.NewGuid()}", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        }

        public ApiFixture Fixture { get; }
    }

    #endregion
}
