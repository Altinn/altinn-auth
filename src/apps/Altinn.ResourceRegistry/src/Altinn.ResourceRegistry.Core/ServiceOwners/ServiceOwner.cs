#nullable enable

using System.Collections.Immutable;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Register;
using Altinn.ResourceRegistry.Extensions;
using CommunityToolkit.Diagnostics;

namespace Altinn.ResourceRegistry.Core.ServiceOwners;

/// <summary>
/// Represents a service owner.
/// </summary>
public sealed record ServiceOwner
{
    /// <summary>
    /// Creates a new instance of <see cref="ServiceOwner"/> from a <see cref="Org"/> and it's orgcode.
    /// </summary>
    /// <param name="orgCode">The orgcode.</param>
    /// <param name="org">The <see cref="Org"/>.</param>
    /// <returns>A new <see cref="ServiceOwner"/>.</returns>
    internal static ServiceOwner Create(string orgCode, Org org)
    {
        if (!OrganizationNumber.TryParse(org.Orgnr, provider: null, out var orgNumber))
        {
            ThrowHelper.ThrowArgumentException(nameof(org), $"Org with orgcode '{orgCode}' has invalid organization number '{org.Orgnr ?? "<null>"}'");
        }

        var name = org.Name.ToImmutableDictionary();
        var envs = org.Environments.ToImmutableHashSet();

        return new ServiceOwner(name, org.Logo, orgCode, orgNumber, org.Homepage, envs);
    }

    private ServiceOwner(
        IReadOnlyDictionary<string, string> name,
        string logo,
        string orgCode,
        OrganizationNumber organizationNumber,
        string homepage,
        ImmutableHashSet<string> environments)
    {
        Name = name;
        Logo = logo;
        OrgCode = orgCode;
        OrganizationNumber = organizationNumber;
        Homepage = homepage;
        Environments = environments;
    }

    /// <summary>
    /// Gets the name of the organization.
    /// </summary>
    /// <remarks>
    /// Dictionary due to language support.
    /// </remarks>
    public IReadOnlyDictionary<string, string> Name { get; }

    /// <summary>
    /// Gets the logo of the organization.
    /// </summary>
    public string Logo { get; }

    /// <summary>
    /// Gets the service-owner code.
    /// </summary>
    public string OrgCode { get; }

    /// <summary>
    /// Gets the organization number.
    /// </summary>
    public OrganizationNumber OrganizationNumber { get; }

    /// <summary>
    /// Gets the homepage of the organization.
    /// </summary>
    public string Homepage { get; }

    /// <summary>
    /// Gets the environments available for the organization.
    /// </summary>
    public IReadOnlySet<string> Environments { get; }

    /// <inheritdoc/>
    public bool Equals(ServiceOwner? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (other is null)
        {
            return false;
        }

        if (!string.Equals(OrgCode, other.OrgCode, StringComparison.Ordinal)
            || !string.Equals(Logo, other.Logo, StringComparison.Ordinal)
            || !string.Equals(Homepage, other.Homepage, StringComparison.Ordinal)
            || !OrganizationNumber.Equals(other.OrganizationNumber)
            || Name.Count != other.Name.Count
            || Environments.Count != other.Environments.Count)
        {
            return false;
        }

        if (!ReferenceEquals(Environments, other.Environments))
        {
            foreach (var env in Environments)
            {
                if (!other.Environments.Contains(env))
                {
                    return false;
                }
            }
        }

        if (!ReferenceEquals(Name, other.Name))
        {
            foreach (var (key, value) in Name)
            {
                if (!other.Name.TryGetValue(key, out var otherValue)
                    || !string.Equals(value, otherValue, StringComparison.Ordinal))
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        HashCode hash = default;
        hash.Add(OrgCode, StringComparer.OrdinalIgnoreCase);
        hash.Add(Logo, StringComparer.Ordinal);
        hash.Add(Homepage, StringComparer.Ordinal);
        hash.Add(OrganizationNumber);
        hash.AddSet(Environments, StringComparer.Ordinal, StringComparer.Ordinal);
        hash.AddDictionary(Name, StringComparer.Ordinal, StringComparer.Ordinal, StringComparer.Ordinal);

        return hash.ToHashCode();
    }
}
