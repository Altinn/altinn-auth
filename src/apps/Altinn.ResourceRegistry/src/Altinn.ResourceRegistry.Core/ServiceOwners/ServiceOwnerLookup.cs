#nullable enable

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Register;

namespace Altinn.ResourceRegistry.Core.ServiceOwners;

/// <summary>
/// A lookup for service owners.
/// </summary>
public sealed class ServiceOwnerLookup
{
    /// <summary>
    /// Gets an empty <see cref="ServiceOwnerLookup"/>.
    /// </summary>
    internal static ServiceOwnerLookup Empty { get; } = new(
        ImmutableDictionary<string, ServiceOwner>.Empty, 
        ImmutableDictionary<OrganizationNumber, ImmutableArray<ServiceOwner>>.Empty);

    private readonly ImmutableDictionary<string, ServiceOwner> _byName;
    private readonly ImmutableDictionary<OrganizationNumber, ImmutableArray<ServiceOwner>> _byOrgNumber;

    /// <summary>
    /// Creates a new <see cref="ServiceOwnerLookup"/> from an <see cref="OrgList"/>.
    /// </summary>
    /// <param name="orgList">The org list.</param>
    /// <returns>A <see cref="ServiceOwnerLookup"/>.</returns>
    internal static ServiceOwnerLookup Create(OrgList orgList)
    {
        if (orgList.Orgs is null or { Count: 0 })
        {
            return Empty;
        }

        var byNameBuilder = ImmutableDictionary.CreateBuilder<string, ServiceOwner>(StringComparer.OrdinalIgnoreCase);
        var byOrgNumberBuilder = new Dictionary<OrganizationNumber, ImmutableArray<ServiceOwner>.Builder>();

        foreach (var (orgCode, org) in orgList.Orgs)
        {
            if (string.Equals(orgCode, "ttd", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrEmpty(org.Orgnr))
            {
                org.Orgnr = orgList.Orgs["digdir"].Orgnr;
            }

            var serviceOwner = ServiceOwner.Create(orgCode, org);

            byNameBuilder.Add(orgCode, serviceOwner);
            if (!byOrgNumberBuilder.TryGetValue(serviceOwner.OrganizationNumber, out var serviceOwnersWithSameOrgNumber))
            {
                serviceOwnersWithSameOrgNumber = ImmutableArray.CreateBuilder<ServiceOwner>();
                byOrgNumberBuilder.Add(serviceOwner.OrganizationNumber, serviceOwnersWithSameOrgNumber);
            }

            serviceOwnersWithSameOrgNumber.Add(serviceOwner);
        }

        var byName = byNameBuilder.ToImmutable();
        var byOrgNumber = byOrgNumberBuilder.ToImmutableDictionary(
            static kvp => kvp.Key,
            static kvp => kvp.Value.ToImmutable());

        return new ServiceOwnerLookup(byName, byOrgNumber);
    }

    private ServiceOwnerLookup(
        ImmutableDictionary<string, ServiceOwner> byName, 
        ImmutableDictionary<OrganizationNumber, ImmutableArray<ServiceOwner>> byOrgNumber)
    {
        _byName = byName.WithComparers(StringComparer.OrdinalIgnoreCase);
        _byOrgNumber = byOrgNumber;
    }

    /// <summary>
    /// Try to get a service owner by organization code.
    /// </summary>
    /// <param name="orgCode">The organization code (for instance ttd).</param>
    /// <param name="serviceOwner">The found <see cref="ServiceOwner"/>, or <see langword="null"/>.</param>
    /// <returns>Whether or not a <see cref="ServiceOwner"/> was found.</returns>
    public bool TryGet(string orgCode, [NotNullWhen(true)] out ServiceOwner? serviceOwner)
        => _byName.TryGetValue(orgCode, out serviceOwner);

    /// <summary>
    /// Try to get service owners by organization number.
    /// </summary>
    /// <param name="organizationNumber">The organization number.</param>
    /// <param name="serviceOwners">
    /// An <see cref="ImmutableArray{T}"/> of <see cref="ServiceOwner"/>s that
    /// has the organization number <paramref name="organizationNumber"/>, or
    /// <see langword="default"/> if none was found.</param>
    /// <returns>Whether or not any <see cref="ServiceOwner"/>s was found.</returns>
    public bool TryFind(OrganizationNumber organizationNumber, out ImmutableArray<ServiceOwner> serviceOwners)
        => _byOrgNumber.TryGetValue(organizationNumber, out serviceOwners);

    /// <summary>
    /// Gets a list of service owners by organization number.
    /// </summary>
    /// <param name="organizationNumber">The <see cref="OrganizationNumber"/>.</param>
    /// <returns>An <see cref="ImmutableArray{T}"/> of <see cref="ServiceOwner"/>s.</returns>
    public ImmutableArray<ServiceOwner> Find(OrganizationNumber organizationNumber)
        => _byOrgNumber.TryGetValue(organizationNumber, out var serviceOwners) ? serviceOwners : ImmutableArray<ServiceOwner>.Empty;
}
