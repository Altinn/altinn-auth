using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
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
                Roles = [.. c.Select(c => ConvertCompactRole(c.Role)).Where(ro => ro is not null).DistinctBy(t => t.Id)],
                Resources = [.. c.SelectMany(c => c.Resources).Select(r => Convert(r)).Where(re => re is not null).DistinctBy(t => t.Id)],
                Packages = [.. c.SelectMany(c => c.Packages).Select(p => Convert(p)).Where(pa => pa is not null).DistinctBy(t => t.Id)],
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
                Roles = [.. c.Select(c => ConvertCompactRole(c.Role)).Where(ro => ro is not null).DistinctBy(t => t.Id)],
                Resources = [.. c.SelectMany(c => c.Resources).Select(r => Convert(r)).Where(re => re is not null).DistinctBy(t => t.Id)],
                Packages = [.. c.SelectMany(c => c.Packages).Select(p => Convert(p)).Where(pa => pa is not null).DistinctBy(t => t.Id)],
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
                    .Where(ro => ro is not null)
                    .DistinctBy(t => t.Id).ToList(),
                Packages = res
                    .Where(t => t.FromId == connection.FromId && t.Packages != null)
                    .SelectMany(t => t.Packages)
                    .Select(p => Convert(p))
                    .Where(pa => pa is not null)
                    .DistinctBy(t => t.Id)
                    .ToList(),
                Resources = res
                    .Where(t => t.FromId == connection.FromId && t.Resources != null)
                    .SelectMany(t => t.Resources)
                    .Select(r => Convert(r))
                    .Where(re => re is not null)
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

    public static List<AgentDto> ConvertToClientDelegationAgentDto(List<ConnectionQueryExtendedRecord> connections)
    {
        var agents = connections.GroupBy(c => c.ToId);
        var result = new List<AgentDto>();

        foreach (var agent in agents)
        {
            var entity = agent.First();
            var party = Convert(entity.To);
            var roles = agent.GroupBy(c => c.Role.Id);
            var roleAccess = new List<AgentDto.AgentRoleAccessPackages>();

            foreach (var role in roles)
            {
                var access = new AgentDto.AgentRoleAccessPackages
                {
                    Role = ConvertCompactRole(role.First().Role),
                    Packages = role.SelectMany(r => r.Packages.Select(p => ConvertCompactPackage(p))).Distinct().ToArray(),
                };

                roleAccess.Add(access);
            }

            result.Add(new AgentDto
            {
                Agent = party,
                Access = roleAccess,
            });
        }

        return result;
    }

    public static List<ClientDto> ConvertToClientDto(List<ConnectionQueryExtendedRecord> connections)
    {
        var clients = connections.GroupBy(c => c.FromId);
        var result = new List<ClientDto>();

        foreach (var client in clients)
        {
            var entity = client.First().From;
            var party = Convert(entity);
            var roles = client.GroupBy(c => c.Role.Id);
            var roleAccess = new List<ClientDto.RoleAccessPackages>();
            foreach (var role in roles)
            {
                var access = new ClientDto.RoleAccessPackages
                {
                    Role = ConvertCompactRole(role.First().Role),
                    Packages = role.SelectMany(r => r.Packages.Select(p => ConvertCompactPackage(p))).Distinct().ToArray(),
                };

                roleAccess.Add(access);
            }

            result.Add(new ClientDto
            {
                Client = party,
                Access = roleAccess,
            });
        }

        return result;
    }
}
