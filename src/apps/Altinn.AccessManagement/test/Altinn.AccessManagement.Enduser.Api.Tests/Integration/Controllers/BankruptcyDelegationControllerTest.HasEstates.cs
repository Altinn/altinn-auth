using System.Net;
using System.Text.Json;
using Altinn.AccessManagement.Api.Enduser.Controllers;
using Altinn.AccessManagement.Core.Constants;
using Altinn.AccessManagement.TestUtils;
using Altinn.AccessManagement.TestUtils.Data;

namespace Altinn.AccessManagement.Enduser.Api.Tests.Integration.Controllers;

/// <summary>
/// Partial test class for <see cref="BankruptcyDelegationController"/>, covering the base route that
/// answers whether a party administrates any bankruptcy estates at all.
/// </summary>
public partial class BankruptcyDelegationControllerTest
{
    /// <summary>
    /// Tests for <see cref="BankruptcyDelegationController.HasBankruptcyEstatesForParty"/>
    /// (<c>GET accessmanagement/api/v1/enduser/bankruptcyestate?party={party}</c>).
    /// </summary>
    /// <remarks>
    /// Reads the shared seed described on <see cref="BankruptcyReadOnlyFixture"/>. The endpoint asks a
    /// narrower question than <c>GET estates</c>: it is true only for a party that holds an
    /// EstateAdministrator assignment, which is what separates
    /// <see cref="TestEntities.PersonMatilde"/> from her agents.
    /// </remarks>
    [IntegrationTest]
    [Collection(BankruptcyReadOnlyCollection.Name)]
    public class HasBankruptcyEstatesForParty
    {
        public HasBankruptcyEstatesForParty(BankruptcyReadOnlyFixture fixture)
        {
            Fixture = fixture;
            Fixture.EnsureSeeded();
        }

        public BankruptcyReadOnlyFixture Fixture { get; }

        /// <summary>
        /// Calls the endpoint as the given party and returns the boolean body.
        /// </summary>
        private async Task<bool> GetHasEstates(Guid party)
        {
            var client = CreateClient(Fixture, party, AuthzConstants.SCOPE_PORTAL_ENDUSER);

            var response = await client.GetAsync($"{Route}?party={party}", TestContext.Current.CancellationToken);

            var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected OK but got {response.StatusCode}. Response body: {content}");

            return JsonSerializer.Deserialize<bool>(content, JsonOptions);
        }

        /// <summary>
        /// The party holds EstateAdministrator assignments for three estates.
        /// </summary>
        [Fact]
        public async Task HasBankruptcyEstatesForParty_ForPartyAdministratingEstates_Returns200True()
        {
            Assert.True(await GetHasEstates(TestEntities.PersonMatilde.Id));
        }

        /// <summary>
        /// A party with no connections at all has no estates.
        /// </summary>
        [Fact]
        public async Task HasBankruptcyEstatesForParty_ForPartyWithoutConnections_Returns200False()
        {
            Assert.False(await GetHasEstates(TestEntities.PersonHenrik.Id));
        }

        /// <summary>
        /// Holding an agent assignment does not make one an estate administrator, even if the agent has been delegated estates.
        /// </summary>
        [Fact]
        public async Task HasBankruptcyEstatesForParty_ForAgentWithDelegatedEstates_Returns200False()
        {
            Assert.False(await GetHasEstates(TestEntities.PersonPaula.Id));
        }

        /// <summary>
        /// An agent without anything delegated is likewise not an estate administrator.
        /// </summary>
        [Fact]
        public async Task HasBankruptcyEstatesForParty_ForAgentWithoutDelegatedEstates_Returns200False()
        {
            Assert.False(await GetHasEstates(TestEntities.PersonOrjan.Id));
        }

        /// <summary>
        /// The estate itself is the "from" side of the EstateAdministrator assignment, not the "to"
        /// side, so asking on behalf of the estate answers false.
        /// </summary>
        [Fact]
        public async Task HasBankruptcyEstatesForParty_ForTheEstateItself_Returns200False()
        {
            Assert.False(await GetHasEstates(TestEntities.OrganizationSolsidenSameie.Id));
        }

        /// <summary>
        /// A party that does not exist is not an error: the query simply finds nothing.
        /// </summary>
        [Fact]
        public async Task HasBankruptcyEstatesForParty_ForUnknownParty_Returns200False()
        {
            Assert.False(await GetHasEstates(Guid.NewGuid()));
        }
    }
}
