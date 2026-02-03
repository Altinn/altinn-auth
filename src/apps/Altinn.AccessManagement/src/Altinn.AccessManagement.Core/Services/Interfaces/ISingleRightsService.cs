using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Microsoft.AspNetCore.Mvc;

namespace Altinn.AccessManagement.Core.Services.Interfaces
{
    /// <summary>
    /// Service for operations regarding single rights delegations
    /// </summary>
    public interface ISingleRightsService
    {
        /// <summary>
        /// Performs a delegation check for the authenticated user on behalf of the from party, to find if and what rights the user can delegate to the to party, for the given resource.
        /// </summary>
        /// <param name="authenticatedUserId">The user id of the authenticated user performing the delegation</param>
        /// <param name="authenticatedUserAuthlevel">The authentication level of the authenticated user performing the delegation</param>
        /// <param name="request">The model describing the right delegation check to perform</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>The result of the delegation status check</returns>
        public Task<DelegationCheckResponse> RightsDelegationCheck(int authenticatedUserId, int authenticatedUserAuthlevel, RightsDelegationCheckRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Takes a List of rules and enrich it with uuids and try to write the rules as delegation policy rules
        /// </summary>
        /// <param name="rules">Listy of rules</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>The stored rules</returns>
        public Task<List<Rule>> EnrichAndTryWriteDelegationPolicyRules(List<Rule> rules, CancellationToken cancellationToken);

        /// <summary>
        /// Takes entities and a list of actionKeys and tries to write delegation policy
        /// </summary>
        /// <param name="from">From (OfferedBy)</param>
        /// <param name="to">To (CoveredBy)</param>
        /// <param name="resource">Resource to delegate</param>
        /// <param name="actionIds">Actions on resource to delegsate</param>
        /// <param name="performedBy">Performed by</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>The stored rules</returns>
        Task<List<Rule>> TryWriteDelegationPolicyRules(Entity from, Entity to, Resource resource, List<string> actionIds, Entity performedBy, CancellationToken cancellationToken);

        /// <summary>
        /// Enrich delete request with Performed by uuid and call PAP to delete rules
        /// </summary>
        /// <param name="rulesToDelete">Entity to define which rules to be deleted</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>The list of rules with created Id and result status</returns>
        Task<List<Rule>> EnrichAndTryDeleteDelegationPolicyRules(List<RequestToDelete> rulesToDelete, CancellationToken cancellationToken = default);

        /// <summary>
        /// Enrich delete request with Performed by uuid and call PAP to delete policies
        /// </summary>
        /// <param name="policiesToDelete">entity containing match for all the policies to delete</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>A list containing all the policies that is deleted</returns>
        Task<List<Rule>> EnrichAndTryDeleteDelegationPolicies(List<RequestToDelete> policiesToDelete, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs the delegation on behalf of the from party
        /// </summary>
        /// <param name="authenticatedUserId">The user id of the authenticated user performing the delegation</param>
        /// <param name="authenticatedUserPartyUuid">the party uuid of the delegating user</param>
        /// <param name="authenticatedUserAuthlevel">The authentication level of the authenticated user performing the delegation</param>
        /// <param name="delegation">The delegation</param>
        /// <param name="cancellationToken">CancellationToken</param>
        /// <returns>The result of the delegation</returns>
        public Task<DelegationActionResult> DelegateRights(int authenticatedUserId, Guid authenticatedUserPartyUuid, int authenticatedUserAuthlevel, DelegationLookup delegation, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all offered single rights delegations for a reportee
        /// </summary>
        /// <param name="reportee">reportee</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>list of delgations</returns>
        Task<IEnumerable<RightDelegation>> GetOfferedRights(AttributeMatch reportee, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all received single rights delegations for a reportee
        /// </summary>
        /// <param name="reportee">reportee</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>list of delgations</returns>
        public Task<List<RightDelegation>> GetReceivedRights(AttributeMatch reportee, CancellationToken cancellationToken = default);

        /// <summary>
        /// Operation to revoke a single rights delegation
        /// </summary>
        /// <param name="authenticatedUserId">authenticed user id</param>
        /// <param name="authenticatedUserPartyUuid">authenticated user party uuid</param>
        /// <param name="delegation">delegation</param>
        /// <returns>The result of the deletion</returns>
        /// <param name="cancellationToken">http context token</param>
        Task<ValidationProblemDetails> RevokeRightsDelegation(int authenticatedUserId, Guid authenticatedUserPartyUuid, DelegationLookup delegation, CancellationToken cancellationToken);
    }
}
