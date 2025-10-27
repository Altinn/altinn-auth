using Altinn.AccessManagement.Core.Models;

namespace Altinn.AccessManagement.Core.Services.Interfaces;

/// <summary>
/// Service for operations regarding retrieval of authorized parties (aka reporteelist)
/// </summary>
public interface IAuthorizedPartiesService
{
    /// <summary>
    /// Gets the full unfiltered list of all authorized parties a party have some access for in Altinn
    /// </summary>
    /// <param name="subjectAttribute">Attribute identifying the party retrieve the authorized party list for</param>
    /// <param name="includeAltinn2">Whether Authorized Parties from Altinn 2 should be included in the result set</param>
    /// <param name="includeAltinn3">Whether Authorized Parties from Altinn 3 should be included in the result set</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedParties(BaseAttribute subjectAttribute, bool includeAltinn2, bool includeAltinn3 = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given user can represent in Altinn
    /// </summary>
    /// <param name="subjectUserId">The user id of the user to retrieve the authorized party list for</param>
    /// <param name="includeAltinn2">Whether Authorized Parties from Altinn 2 should be included in the result set</param>
    /// <param name="includeAltinn3">Whether Authorized Parties from Altinn 3 should be included in the result set</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesByUserId(int subjectUserId, bool includeAltinn2, bool includeAltinn3 = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given user or organization have some access for in Altinn
    /// </summary>
    /// <param name="subjectPartyId">The party id of the user or organization to retrieve the authorized party list for</param>
    /// <param name="includeAltinn2">Whether Authorized Parties from Altinn 2 should be included in the result set</param>
    /// <param name="includeAltinn3">Whether Authorized Parties from Altinn 3 should be included in the result set</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesByPartyId(int subjectPartyId, bool includeAltinn2, bool includeAltinn3 = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given person can represent in Altinn
    /// </summary>
    /// <param name="subjectNationalId">The national identity number of the person to retrieve the authorized party list for</param>
    /// <param name="includeAltinn2">Whether Authorized Parties from Altinn 2 should be included in the result set</param>
    /// <param name="includeAltinn3">Whether Authorized Parties from Altinn 3 should be included in the result set</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesByPersonId(string subjectNationalId, bool includeAltinn2, bool includeAltinn3 = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given person can represent in Altinn
    /// </summary>
    /// <param name="subjectPersonUuid">The uuid of the person to retrieve the authorized party list for</param>
    /// <param name="includeAltinn2">Whether Authorized Parties from Altinn 2 should be included in the result set</param>
    /// <param name="includeAltinn3">Whether Authorized Parties from Altinn 3 should be included in the result set</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesByPersonUuid(string subjectPersonUuid, bool includeAltinn2, bool includeAltinn3 = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given organization can represent in Altinn
    /// </summary>
    /// <param name="subjectOrganizationNumber">The organization number of the organization to retrieve the authorized party list for</param>
    /// <param name="includeAltinn2">Whether Authorized Parties from Altinn 2 should be included in the result set</param>
    /// <param name="includeAltinn3">Whether Authorized Parties from Altinn 3 should be included in the result set</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesByOrganizationId(string subjectOrganizationNumber, bool includeAltinn2, bool includeAltinn3 = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given organization can represent in Altinn
    /// </summary>
    /// <param name="subjectOrganizationUuid">The organization uuid of the organization to retrieve the authorized party list for</param>
    /// <param name="includeAltinn2">Whether Authorized Parties from Altinn 2 should be included in the result set</param>
    /// <param name="includeAltinn3">Whether Authorized Parties from Altinn 3 should be included in the result set</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesByOrganizationUuid(string subjectOrganizationUuid, bool includeAltinn2, bool includeAltinn3 = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given enterprise user can represent in Altinn
    /// </summary>
    /// <param name="subjectEnterpriseUsername">The username of the enterprise user to retrieve the authorized party list for</param>
    /// <param name="includeAltinn2">Whether Authorized Parties from Altinn 2 should be included in the result set</param>
    /// <param name="includeAltinn3">Whether Authorized Parties from Altinn 3 should be included in the result set</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesByEnterpriseUsername(string subjectEnterpriseUsername, bool includeAltinn2, bool includeAltinn3 = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given enterprise user can represent in Altinn
    /// </summary>
    /// <param name="subjectEnterpriseUserUuid">The uuid of the enterprise user to retrieve the authorized party list for</param>
    /// <param name="includeAltinn2">Whether Authorized Parties from Altinn 2 should be included in the result set</param>
    /// <param name="includeAltinn3">Whether Authorized Parties from Altinn 3 should be included in the result set</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesByEnterpriseUserUuid(string subjectEnterpriseUserUuid, bool includeAltinn2, bool includeAltinn3 = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given system user can represent in Altinn
    /// </summary>
    /// <param name="subjectSystemUserUuid">The uuid of the system user to retrieve the authorized party list for</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesBySystemUserUuid(string subjectSystemUserUuid, CancellationToken cancellationToken);
}
