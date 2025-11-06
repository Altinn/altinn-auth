using Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Utils;

/// <inheritdoc/>
public partial class DtoMapper : IDtoMapper
{
    public static List<ConnectionDto> ConvertToOthers(IEnumerable<ConnectionQueryExtendedRecord> connections)
    {
        var result = connections.Where(t => t.Reason != ConnectionReason.KeyRole && t.Reason != ConnectionReason.Delegation).GroupBy(res => res.ToId).Select(c =>
        {
            var connection = c.First();
            var subconnections = connections.Where(c => c.ViaId == connection.ToId && (c.Reason == ConnectionReason.KeyRole || c.Reason == ConnectionReason.Delegation));
            return new ConnectionDto()
            {
                Party = Convert(connection.From),
                Roles = [.. c.Select(c => ConvertCompactRole(c.Role)).DistinctBy(t => t.Id)],
                Resources = [.. c.SelectMany(c => c.Resources).Select(r => Convert(r)).DistinctBy(t => t.Id)],
                Packages = [.. c.SelectMany(c => c.Packages).Select(p => Convert(p)).DistinctBy(t => t.Id)],
                Connections = ConvertSubConnections(subconnections),
            };
        });

        return result.ToList();
    }

    public static List<ConnectionDto> ConvertFromOthers(IEnumerable<ConnectionQueryExtendedRecord> connections)
    {
        var result = connections.Where(t => t.Reason != ConnectionReason.Hierarchy).GroupBy(res => res.FromId).Select(c =>
        {
            var connection = c.First();
            var subconnections = connections.Where(c => c.ViaId == connection.FromId && c.Reason == ConnectionReason.Hierarchy);
            return new ConnectionDto()
            {
                Party = Convert(connection.To),
                Roles = [.. c.Select(c => ConvertCompactRole(c.Role)).DistinctBy(t => t.Id)],
                Resources = [.. c.SelectMany(c => c.Resources).Select(r => Convert(r)).DistinctBy(t => t.Id)],
                Packages = [.. c.SelectMany(c => c.Packages).Select(p => Convert(p)).DistinctBy(t => t.Id)],
                Connections = ConvertSubConnections(subconnections),
            };
        });

        return result.ToList();
    }
    
    public static List<PackagePermissionDto> ConvertPackages(IEnumerable<ConnectionQueryExtendedRecord> res)
    {
        return res
            .SelectMany(connection =>
            {
                var permission = ConvertToPermission(connection);
                return connection.Packages.Select(pkg => new PackagePermissionDto
                {
                    Package = new CompactPackageDto
                    {
                        Id = pkg.Id,
                        Urn = pkg.Urn,
                        AreaId = pkg.AreaId
                    },
                    Permissions = [permission]
                });
            })
            .GroupBy(p => p.Package.Id)
            .Select(p => new PackagePermissionDto
            {
                Package = p.First().Package,
                Permissions = [.. p.SelectMany(x => x.Permissions).DistinctBy(p => (p.From?.Id, p.To?.Id, p.Via?.Id, p.ViaRole?.Id, p.Role?.Id))]
            })
            .ToList();
    }

    public static List<ConnectionDto> ConvertSubConnections(IEnumerable<ConnectionQueryExtendedRecord> res)
    {
        var result = res
            .Select(relation => new ConnectionDto()
            {
                Party = Convert(relation.From),
                Roles = res
                    .Where(t => t.FromId == relation.FromId)
                    .Select(t => ConvertCompactRole(t.Role))
                    .DistinctBy(t => t.Id).ToList(),
                Packages = res
                    .Where(t => t.FromId == relation.FromId && t.Packages != null)
                    .SelectMany(t => t.Packages)
                    .Select(p => Convert(p))
                    .DistinctBy(t => t.Id)
                    .ToList(),
                Resources = res
                    .Where(t => t.FromId == relation.FromId && t.Resources != null)
                    .SelectMany(t => t.Resources)
                    .Select(r => Convert(r))
                    .DistinctBy(t => t.Id)
                    .ToList(),

                Connections = new()
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
