using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Utils;

/// <summary>
/// In-memory lookup over a list of packages, by the values clients send: URN, code or id.
/// URN matching accepts the full URN, ":code" or "code", ignoring case.
/// </summary>
internal sealed class PackageIndex
{
    private const string UrnPrefix = "urn:altinn:accesspackage";

    private readonly Dictionary<string, Package> _byUrn = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Package> _byCode = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Guid, Package> _byId = [];

    public PackageIndex(IEnumerable<Package> packages)
    {
        Packages = packages.ToList();

        foreach (var package in Packages)
        {
            if (!string.IsNullOrEmpty(package.Urn))
            {
                _byUrn.TryAdd(package.Urn, package);
            }

            if (!string.IsNullOrEmpty(package.Code))
            {
                _byCode.TryAdd(package.Code, package);
            }

            _byId.TryAdd(package.Id, package);
        }
    }

    /// <summary>
    /// All packages in the index.
    /// </summary>
    public IReadOnlyList<Package> Packages { get; }

    /// <summary>
    /// Finds a package by URN. Accepts the full URN, ":code" or "code", ignoring case.
    /// </summary>
    /// <returns>The package, or <c>null</c> when no package matches.</returns>
    public Package FindByUrn(string urn)
    {
        if (string.IsNullOrEmpty(urn))
        {
            return null;
        }

        if (_byUrn.TryGetValue(urn, out var package))
        {
            return package;
        }

        // ":code"
        if (urn.StartsWith(':') && urn.Split(':').Length == 2 && _byUrn.TryGetValue(UrnPrefix + urn, out package))
        {
            return package;
        }

        // "code"
        return _byUrn.TryGetValue($"{UrnPrefix}:{urn}", out package) ? package : null;
    }

    /// <summary>
    /// Finds a package by URN, code or id, tried in that order. URN accepts the same forms as
    /// <see cref="FindByUrn"/>, and code is matched ignoring case.
    /// </summary>
    /// <returns>The package, or <c>null</c> when no package matches.</returns>
    public Package Find(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        if (FindByUrn(value) is { } package)
        {
            return package;
        }

        if (_byCode.TryGetValue(value, out package))
        {
            return package;
        }

        return Guid.TryParse(value, out var id) && _byId.TryGetValue(id, out package) ? package : null;
    }
}
