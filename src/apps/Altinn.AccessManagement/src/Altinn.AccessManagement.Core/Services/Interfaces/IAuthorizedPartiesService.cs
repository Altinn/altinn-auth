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
    /// <param name="filter">Filters to apply when retrieving authorized parties</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedParties(BaseAttribute subjectAttribute, AuthorizedPartiesFilters filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given user can represent in Altinn
    /// </summary>
    /// <param name="subjectUserId">The user id of the user to retrieve the authorized party list for</param>
    /// <param name="filter">Filters to apply when retrieving authorized parties</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesByUserId(int subjectUserId, AuthorizedPartiesFilters filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given user or organization have some access for in Altinn
    /// </summary>
    /// <param name="subjectPartyId">The party id of the user or organization to retrieve the authorized party list for</param>
    /// <param name="filter">Filters to apply when retrieving authorized parties</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesByPartyId(int subjectPartyId, AuthorizedPartiesFilters filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given person can represent in Altinn
    /// </summary>
    /// <param name="subjectPersonId">The national identity number of the person to retrieve the authorized party list for</param>
    /// <param name="filter">Filters to apply when retrieving authorized parties</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesByPersonId(string subjectPersonId, AuthorizedPartiesFilters filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given person can represent in Altinn
    /// </summary>
    /// <param name="subjectPersonUuid">The uuid of the person to retrieve the authorized party list for</param>
    /// <param name="filter">Filters to apply when retrieving authorized parties</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesByPersonUuid(string subjectPersonUuid, AuthorizedPartiesFilters filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given organization can represent in Altinn
    /// </summary>
    /// <param name="subjectOrganizationNumber">The organization number of the organization to retrieve the authorized party list for</param>
    /// <param name="filter">Filters to apply when retrieving authorized parties</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesByOrganizationId(string subjectOrganizationNumber, AuthorizedPartiesFilters filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given organization can represent in Altinn
    /// </summary>
    /// <param name="subjectOrganizationUuid">The organization uuid of the organization to retrieve the authorized party list for</param>
    /// <param name="filter">Filters to apply when retrieving authorized parties</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesByOrganizationUuid(string subjectOrganizationUuid, AuthorizedPartiesFilters filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given enterprise user can represent in Altinn
    /// </summary>
    /// <param name="subjectEnterpriseUsername">The username of the enterprise user to retrieve the authorized party list for</param>
    /// <param name="filter">Filters to apply when retrieving authorized parties</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesByEnterpriseUsername(string subjectEnterpriseUsername, AuthorizedPartiesFilters filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given enterprise user can represent in Altinn
    /// </summary>
    /// <param name="subjectEnterpriseUserUuid">The uuid of the enterprise user to retrieve the authorized party list for</param>
    /// <param name="filter">Filters to apply when retrieving authorized parties</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesByEnterpriseUserUuid(string subjectEnterpriseUserUuid, AuthorizedPartiesFilters filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full unfiltered list of authorized parties the given system user can represent in Altinn
    /// </summary>
    /// <param name="subjectSystemUserUuid">The uuid of the system user to retrieve the authorized party list for</param>
    /// <param name="filter">Filters to apply when retrieving authorized parties</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The unfiltered party list</returns>
    Task<List<AuthorizedParty>> GetAuthorizedPartiesBySystemUserUuid(string subjectSystemUserUuid, AuthorizedPartiesFilters filter, CancellationToken cancellationToken);

    /// <summary>
    /// Gets all relevant filter party UUIDs for the provided party attributes
    /// </summary>
    /// <param name="partyAttributes">The party attributes to lookup party uuids</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>All identified uuids and any parent/mainunit party uuids</returns>
    Task<IEnumerable<Guid>> GetPartyFilterUuids(IEnumerable<BaseAttribute> partyAttributes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all relevant filter party UUIDs for the provided input of party uuids
    /// </summary>
    /// <param name="filterUuids">The input filter party uuids</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>All identified uuids and any parent/mainunit party uuids</returns>
    Task<IEnumerable<Guid>> GetPartyFilterUuids(IEnumerable<Guid> filterUuids, CancellationToken cancellationToken = default);
}
