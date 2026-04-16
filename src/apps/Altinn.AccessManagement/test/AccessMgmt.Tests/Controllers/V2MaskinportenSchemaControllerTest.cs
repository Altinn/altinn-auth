using System.Net.Http.Json;
using Altinn.AccessManagement.Controllers;
using Altinn.AccessManagement.Core.Helpers.Extensions;
using Altinn.AccessManagement.Models;
using Altinn.AccessManagement.Tests.Fixtures;
using Altinn.AccessManagement.Tests.Scenarios;
using Altinn.AccessManagement.Tests.Seeds;

namespace Altinn.AccessManagement.Tests.Controllers;

/// <summary>
/// <see cref="MaskinportenSchemaController"/>
/// </summary>
public class V2MaskinportenSchemaControllerTest(WebApplicationFixture fixture) : IClassFixture<WebApplicationFixture>
{
    private WebApplicationFixture Fixture { get; } = fixture;

    /// <summary>
    /// Verifies that a delegation from and to party exists in database
    /// </summary>
    public static Action<AcceptanceCriteriaComposer> WithAssertDbContainsDelegationsToParty(int from, int to, string resource) => test =>
    {
        test.ApiAssertions.Add(async host =>
        {
            var actual = await host.Repository.DelegationMetadataRepository.GetAllCurrentResourceRegistryDelegationChanges(from.SingleToList(), resource.SingleToList(), to.SingleToList());
            Assert.NotEmpty(actual);
        });
    };

    /// <summary>
    /// Assert response
    /// </summary>
    /// <param name="from">delegated from</param>
    /// <param name="to">delegation to</param>
    /// <returns></returns>
    public static Action<AcceptanceCriteriaComposer> WithAssertResponseContainsDelegations(IParty from, IParty to) => test =>
    {
        test.ResponseAssertions.Add(async response =>
        {
            var body = await response.Content.ReadFromJsonAsync<IEnumerable<MaskinportenSchemaDelegationExternal>>();

            Assert.True(
                body.Any(delegation => delegation.OfferedByPartyId == from.Party.PartyId && delegation.CoveredByPartyId == to.Party.PartyId),
                $"Body did not contain a delegation from party with id {from.Party.PartyId} to party with id {to.Party.PartyId}");
        });
    };

    /// <summary>
    /// Seeds for <see cref="GET_GetOfferedMaskinportenSchemaDelegations"/>
    /// </summary>
    /// <param name="acceptanceCriteria">Acceptance Criteria</param>
    /// <param name="partyId">party id</param>
    /// <param name="actions">modifiers for <see cref="AcceptanceCriteriaComposer"/></param>
    public class SeedGetOfferedMaskinportenSchemaDelegations(string acceptanceCriteria, int partyId, params Action<AcceptanceCriteriaComposer>[] actions) : AcceptanceCriteriaComposer(
            acceptanceCriteria,
            actions,

            WithRequestRoute("accessmanagement", "api", "v1", partyId, "maskinportenschema", "offered"),
            WithRequestVerb(HttpMethod.Get))
    {
        /// <summary>
        /// Seeds
        /// </summary>
        public static TheoryData<SeedGetOfferedMaskinportenSchemaDelegations> Seeds() =>
        [
            new(
                /* Acceptance Criteria */ @"
                GIVEN that organization Voss consulting has delegated a MaskinportenSchema resource to Voss Accounting
                WHEN DAGL Olav for voss consulting requests delegations that Voss consulting has offered
                THEN Voss accounting should be in the list of offered delegations",
                OrganizationSeeds.VossConsulting.PartyId,

                WithScenarios(
                    DelegationScenarios.Defaults,
                    DelegationScenarios.FromOrganizationToOrganization(OrganizationSeeds.VossConsulting.Defaults, OrganizationSeeds.VossAccounting.Defaults, ResourceSeeds.MaskinportenSchema.Defaults)),

                WithAssertDbContainsDelegationsToParty(OrganizationSeeds.VossConsulting.PartyId, OrganizationSeeds.VossAccounting.PartyId, ResourceSeeds.MaskinportenSchema.Identifier),
                WithAssertResponseContainsDelegations(OrganizationSeeds.VossConsulting.Defaults, OrganizationSeeds.VossAccounting.Defaults),
                WithAssertResponseStatusCodeSuccessful)
        ];
    }

    /// <summary>
    /// <see cref="MaskinportenSchemaController.GetOfferedMaskinportenSchemaDelegations(string, System.Threading.CancellationToken)"/>
    /// </summary>
    /// <param name="acceptanceCriteria">acceptance criteria</param>
    /// <remarks>
    /// SKIPPED: This test uses DelegationScenarios that require EF resources to exist in the database.
    /// WebApplicationFixture does not seed test data (Resources, Providers, ResourceTypes).
    /// To fix: Either convert to use ApiFixture (requires adding project reference to Altinn.AccessManagement.TestUtils)
    /// or update DelegationScenarios to use mock delegates instead of EF insertion.
    /// Related to issue #2810 - EF migration cleanup.
    /// </remarks>
    [Theory(Skip = "WebApplicationFixture does not seed EF test data required by DelegationScenarios. Use ApiFixture or update test scenarios.")]
    [MemberData(nameof(SeedGetOfferedMaskinportenSchemaDelegations.Seeds), MemberType = typeof(SeedGetOfferedMaskinportenSchemaDelegations))]
    public async Task GET_GetOfferedMaskinportenSchemaDelegations(SeedGetOfferedMaskinportenSchemaDelegations acceptanceCriteria) => await acceptanceCriteria.Test(Fixture);
}
