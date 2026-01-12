using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

public interface IProviderService
{
    /// <summary>
    /// Gets a provider by organization id
    /// </summary>
    /// <param name="organizationId">The organization ID</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A provider associated with the specified organization ID.</returns>
    ValueTask<Provider> GetProviderByOrganizationId(string organizationId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets a provider by provider code
    /// </summary>
    /// <param name="providerCode">The provider code</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>A provider associated with the specified provider code.</returns>
    ValueTask<Provider> GetProviderByCode(string providerCode, CancellationToken cancellationToken);
}
