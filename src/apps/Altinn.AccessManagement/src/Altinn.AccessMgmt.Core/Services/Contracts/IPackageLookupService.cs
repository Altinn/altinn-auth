using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

/// <summary>
/// Finds access packages by the values clients send: URN, code or id.
/// </summary>
/// <remarks>
/// Reads all packages from the database and caches them for 10 minutes, so a package added to the
/// database is found within 10 minutes.
/// </remarks>
public interface IPackageLookupService
{
    /// <summary>
    /// Gets all packages, with their entity type.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    Task<IReadOnlyList<Package>> GetPackages(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a package by URN. Accepts the full URN, ":code" or "code", ignoring case.
    /// </summary>
    /// <param name="urn">The URN value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The package, or <c>null</c> when no package matches.</returns>
    Task<Package> FindByUrn(string urn, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds packages for several URN values. Each value accepts the same forms as <see cref="FindByUrn"/>.
    /// </summary>
    /// <param name="urns">The URN values.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The found packages, keyed by the value as given. Values without a match are left out.</returns>
    Task<IReadOnlyDictionary<string, Package>> FindByUrns(IEnumerable<string> urns, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a package by URN, code or id, tried in that order.
    /// </summary>
    /// <param name="value">Full URN, ":code", code or id.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>The package, or <c>null</c> when no package matches.</returns>
    Task<Package> Find(string value, CancellationToken cancellationToken = default);
}
