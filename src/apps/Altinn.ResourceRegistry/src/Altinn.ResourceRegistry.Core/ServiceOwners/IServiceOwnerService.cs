#nullable enable

using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Core.ServiceOwners;

/// <summary>
/// Service for getting service owners.
/// </summary>
public interface IServiceOwnerService
{
    /// <summary>
    /// Gets a (cached) list of service-owners.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>A (cached) <see cref="OrgList"/>.</returns>
    public ValueTask<ServiceOwnerLookup> GetServiceOwners(CancellationToken cancellationToken = default);
}
