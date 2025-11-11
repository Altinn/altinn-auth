using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Utils;

/// <inheritdoc/>
public partial class DtoMapper : IDtoMapper
{
    public static List<ConnectionDto> ConvertToOthers(IEnumerable<ConnectionQueryExtendedRecord> connections, bool getSingle = false)
    {
        var result = connections.Where(t => getSingle || (t.Reason != ConnectionReason.KeyRole && t.Reason != ConnectionReason.Delegation)).GroupBy(res => res.ToId).Select(c =>
        {
            var connection = c.First();
            var subconnections = connections.Where(c => c.ViaId == connection.ToId && (c.Reason == ConnectionReason.KeyRole || c.Reason == ConnectionReason.Delegation));
            return new ConnectionDto()
            {
                Party = Convert(connection.To),
                Roles = [.. c.Select(c => ConvertCompactRole(c.Role)).DistinctBy(t => t.Id)],
                Resources = [.. c.SelectMany(c => c.Resources).Select(r => Convert(r)).DistinctBy(t => t.Id)],
                Packages = [.. c.SelectMany(c => c.Packages).Select(p => Convert(p)).DistinctBy(t => t.Id)],
                Connections = ConvertSubConnectionsToOthers(subconnections),
            };
        });

        return result.ToList();
    }

    public static List<ConnectionDto> ConvertFromOthers(IEnumerable<ConnectionQueryExtendedRecord> connections, bool getSingle = false)
    {
        var result = connections.Where(t => getSingle || (t.Reason != ConnectionReason.Hierarchy)).GroupBy(res => res.FromId).Select(c =>
        {
            var connection = c.First();
            var subconnections = connections.Where(c => c.ViaId == connection.FromId && c.Reason == ConnectionReason.Hierarchy);
            return new ConnectionDto()
            {
                Party = Convert(connection.From),
                Roles = [.. c.Select(c => ConvertCompactRole(c.Role)).DistinctBy(t => t.Id)],
                Resources = [.. c.SelectMany(c => c.Resources).Select(r => Convert(r)).DistinctBy(t => t.Id)],
                Packages = [.. c.SelectMany(c => c.Packages).Select(p => Convert(p)).DistinctBy(t => t.Id)],
                Connections = ConvertSubConnectionsFromOthers(subconnections),
            };
        });

        return result.ToList();
    }
    
    public static List<PackagePermissionDto> ConvertPackages(IEnumerable<ConnectionQueryExtendedRecord> res)
    {
        var records = res.ToList();

        return records
            .SelectMany(r => r.Packages)
            .DistinctBy(p => p.Id)
            .Select(pkg => new PackagePermissionDto
            {
                Package = ConvertCompactPackage(pkg),
                Permissions = records
                    .Where(r => r.Packages.Any(p => p.Id == pkg.Id))
                    .Select(ConvertToPermission)
            })
            .ToList();
    }

    public static List<ConnectionDto> ConvertSubConnectionsFromOthers(IEnumerable<ConnectionQueryExtendedRecord> res)
    {
        var result = res.GroupBy(res => res.FromId).Select(c =>
        {
            var connection = c.First();
            return new ConnectionDto()
            {
                Party = Convert(connection.From),
                Roles = res
                    .Where(t => t.FromId == connection.FromId)
                    .Select(t => ConvertCompactRole(t.Role))
                    .DistinctBy(t => t.Id).ToList(),
                Packages = res
                    .Where(t => t.FromId == connection.FromId && t.Packages != null)
                    .SelectMany(t => t.Packages)
                    .Select(p => Convert(p))
                    .DistinctBy(t => t.Id)
                    .ToList(),
                Resources = res
                    .Where(t => t.FromId == connection.FromId && t.Resources != null)
                    .SelectMany(t => t.Resources)
                    .Select(r => Convert(r))
                    .DistinctBy(t => t.Id)
                    .ToList(),

                Connections = new()
            };
        });

        return result.ToList();
    }

    public static List<ConnectionDto> ConvertSubConnectionsToOthers(IEnumerable<ConnectionQueryExtendedRecord> res)
    {
        // Defensive: handle null input
        res ??= Enumerable.Empty<ConnectionQueryExtendedRecord>();

        var result = res.GroupBy(res => res.ToId).Select(c =>
        {
            var connection = c.First();
            return new ConnectionDto()
            {
                Party = Convert(connection.To),
                Roles = res
                    .Where(t => t.ToId == connection.ToId && t.Role != null)
                    .Select(t => ConvertCompactRole(t.Role))
                    .Where(r => r is not null)
                    .DistinctBy(t => t.Id)
                    .ToList(),
                Packages = res
                    .Where(t => t.ToId == connection.ToId && t.Packages != null)
                    .SelectMany(t => t.Packages)
                    .Select(p => Convert(p))
                    .Where(p => p is not null)
                    .DistinctBy(t => t.Id)
                    .ToList(),
                Resources = res
                    .Where(t => t.ToId == connection.ToId && t.Resources != null)
                    .SelectMany(t => t.Resources)
                    .Select(r => Convert(r))
                    .Where(r => r is not null)
                    .DistinctBy(t => t.Id)
                    .ToList(),

                Connections = new()
            };
        });

        return result.ToList();
    }

    public static AccessPackageDto Convert(ConnectionQueryPackage package)
    {
        return new AccessPackageDto()
        {
            Id = package.Id,
            Urn = package.Urn,
            AreaId = package.AreaId,
        };
    }

    public static ResourceDto Convert(ConnectionQueryResource resource)
    {
        return new ResourceDto()
        {
            Id = resource.Id,
            Name = resource.Name,
        };
    }
}
