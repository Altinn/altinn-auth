using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Contracts;
using Altinn.AccessMgmt.Persistence.Services.Models;

namespace Altinn.AccessMgmt.Persistence.Services;

/// <summary>
/// Service for managing connections.
/// </summary>
public class ConnectionService(
    IConnectionRepository connectionRepository,
    IConnectionPackageRepository connectionPackageRepository,
    IConnectionResourceRepository connectionResourceRepository,
    IPackageRepository packageRepository,
    IAssignmentService assignmentService,
    IRoleRepository roleRepository,
    IAssignmentRepository assignmentRepository,
    IDelegationRepository delegationRepository,
    IAssignmentPackageRepository assignmentPackageRepository,
    IDelegationPackageRepository delegationPackageRepository
    ) : IConnectionService
{
    /// <inheritdoc />
    public async Task<ExtConnection> Get(Guid Id, CancellationToken cancellationToken = default)
    {
        var filter = connectionRepository.CreateFilterBuilder();
        filter.Equal(t => t.Id, Id);
        var res = await connectionRepository.GetExtended(filter, cancellationToken: cancellationToken);
        return res.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ExtConnection>> Get(Guid? fromId = null, Guid? toId = null, Guid? facilitatorId = null, CancellationToken cancellationToken = default)
    {
        var filter = connectionRepository.CreateFilterBuilder();

        if (fromId.HasValue)
        {
            filter.Equal(t => t.FromId, fromId.Value);
        }
        else
        {
            filter.IsNull(t => t.FromId);
        }

        if (toId.HasValue)
        {
            filter.Equal(t => t.ToId, toId.Value);
        }
        else
        {
            filter.IsNull(t => t.ToId);
        }

        if (facilitatorId.HasValue)
        {
            filter.Equal(t => t.FacilitatorId, facilitatorId.Value);
        }
        else
        {
            filter.IsNull(t => t.FacilitatorId);
        }

        if (!filter.Any())
        {
            throw new ArgumentException("You need to define a filter");
        }

        return await connectionRepository.GetExtended(filter, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ExtConnection>> GetGiven(Guid toId, CancellationToken cancellationToken = default)
    {
        return await Get(toId: toId, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ExtConnection>> GetReceived(Guid fromId, CancellationToken cancellationToken = default)
    {
        return await Get(fromId: fromId, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ExtConnection>> GetFacilitated(Guid id, CancellationToken cancellationToken = default)
    {
        return await Get(facilitatorId: id, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ConnectionPackage>> GetPackages(Guid? fromId, Guid? toId, CancellationToken cancellationToken = default)
    {
        return await GetConnectionPackages(fromId, toId, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AddPackage(Guid fromId, Guid toId, string roleCode, Guid packageId, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        //// Will create assignment even if GetOrCreateAssignmentPackage fails.
        var assignment = await assignmentService.GetOrCreateAssignmentInternal(fromId, toId, roleCode, options, cancellationToken: cancellationToken);
        var res = await GetOrCreateAssignmentPackage(assignment.Id, packageId, options, cancellationToken: cancellationToken);
        if (res == null)
        {
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> AddPackage(Guid fromId, Guid toId, string roleCode, string packageUrn, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        var filter = packageRepository.CreateFilterBuilder();
        filter.Add(t => t.Urn, packageUrn, Core.Helpers.FilterComparer.EndsWith);
        var packageResult = await packageRepository.Get(filter);

        if (packageResult == null || !packageResult.Any() || packageResult.Count() > 1)
        {
            throw new ArgumentException();
        }

        var package = packageResult.First();

        return await AddPackage(fromId, toId, roleCode, package.Id, options, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> RemovePackage(Guid fromId, Guid toId, string roleCode, Guid packageId, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        var assignment = await assignmentService.GetAssignment(fromId, toId, roleCode, cancellationToken: cancellationToken);
        if (assignment == null)
        {
            Console.WriteLine("Nothing to delete");
            return true;
        }

        return await RemoveAssignmentPackage(assignment.Id, packageId, options, cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> AddPackage(Guid connectionId, Guid packageId, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        //// Fix with new connection model

        var assignment = assignmentRepository.Get(connectionId, cancellationToken: cancellationToken);
        var delegation = delegationRepository.Get(connectionId, cancellationToken: cancellationToken);
        
        if (assignment != null)
        {
            var res = await GetOrCreateAssignmentPackage(connectionId, packageId, options, cancellationToken: cancellationToken);
            return true;
        }
        
        if (delegation != null)
        {
            var res = await GetOrCreateDelegationPackage(connectionId, packageId, options, cancellationToken: cancellationToken);
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public async Task<bool> RemovePackage(Guid connectionId, Guid packageId, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        //// Fix with new connection model

        var assignment = assignmentRepository.Get(connectionId, cancellationToken: cancellationToken);
        var delegation = delegationRepository.Get(connectionId, cancellationToken: cancellationToken);

        if (assignment != null)
        {
            var res = await RemoveAssignmentPackage(connectionId, packageId, options, cancellationToken: cancellationToken);
            return true;
        }

        if (delegation != null)
        {
            var res = await RemoveDelegationPackage(connectionId, packageId, options, cancellationToken: cancellationToken);
            return true;
        }

        return false;
    }
    
    private async Task<IEnumerable<ConnectionPackage>> GetConnectionPackages(Guid? fromId, Guid? toId, CancellationToken cancellationToken = default)
    {
        /* Get packages assigned to entity from a specific entity, to see if entity can assign it to another entity */

        /*
        Get all connectionpackages
        Check if package is in list
        FUTURE: Check if connectionPackage.IsAssignable and connectionPackage.CanAssign
        */

        var filter = connectionPackageRepository.CreateFilterBuilder();

        if (fromId.HasValue)
        {
            filter.Equal(t => t.FromId, fromId.Value);
        }
        else
        {
            filter.IsNull(t => fromId);
        }

        if (toId.HasValue)
        {
            filter.Equal(t => t.ToId, toId.Value);
        }
        else
        {
            filter.IsNull(t => t.ToId);
        }

        //var options = new RequestOptions()
        //{
        //    PageNumber = 1,
        //    PageSize = 50,
        //    UsePaging = true
        //};

        return await connectionPackageRepository.Get(filter, cancellationToken: cancellationToken);
    }

    private async Task<IEnumerable<ExtConnectionPackage>> GetConnectionPackages(Guid fromId, Guid toId, Guid packageId, CancellationToken cancellationToken = default)
    {
        /* Get packages assigned to entity from a specific entity, to see if entity can assign it to another entity */

        var filter = connectionPackageRepository.CreateFilterBuilder();
        filter.Equal(t => t.FromId, fromId);
        filter.Equal(t => t.ToId, toId);
        filter.Equal(t => t.PackageId, packageId);
        return await connectionPackageRepository.GetExtended(filter, cancellationToken: cancellationToken);

    }

    private async Task<bool> RemoveAssignmentPackage(Guid assignmentId, Guid packageId, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        var assignment = await assignmentRepository.Get(assignmentId, cancellationToken: cancellationToken);

        if (assignment == null)
        {
            throw new Exception("Assignment not found");
        }

        var userPackages = await GetConnectionPackages(assignment.FromId, options.ChangedBy, packageId, cancellationToken: cancellationToken);

        if (!userPackages.Any())
        {
            throw new Exception("User does not have the package assigned on this entity");
        }

        // User can also UnAssign?
        if (!userPackages.Any(t => t.CanAssign))
        {
            throw new Exception("User can't assign package");
        }

        var filter = assignmentPackageRepository.CreateFilterBuilder();
        filter.Equal(t => t.AssignmentId, assignmentId);
        filter.Equal(t => t.PackageId, packageId);
        var checkResult = await assignmentPackageRepository.Get(filter, cancellationToken: cancellationToken);
        if (checkResult == null && !checkResult.Any())
        {
            Console.WriteLine("AssignmentPackage does not exist, nothing to delete.");
            return true;
        }

        var deleteResult = await assignmentPackageRepository.DeleteCross(assignmentId, packageId, options, cancellationToken: cancellationToken);
        if (deleteResult > 0)
        {
            return true;
        }

        return false;
    }

    private async Task<bool> RemoveDelegationPackage(Guid delegationId, Guid packageId, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        var delegation = await delegationRepository.Get(delegationId, cancellationToken: cancellationToken);

        if (delegation == null)
        {
            throw new Exception("Delegation not found");
        }

        // other logic?
        var userPackages = await GetConnectionPackages(delegation.FromId, options.ChangedBy, packageId, cancellationToken: cancellationToken);

        if (!userPackages.Any())
        {
            throw new Exception("User does not have the package assigned on this entity");
        }

        //if (!userPackages.Any(t => t.CanDelegate))
        //{
        //    throw new Exception("User can't delegate package");
        //}


        var filter = delegationPackageRepository.CreateFilterBuilder();
        filter.Equal(t => t.DelegationId, delegationId);
        filter.Equal(t => t.PackageId, packageId);
        var checkResult = await delegationPackageRepository.Get(filter, cancellationToken: cancellationToken);
        if (checkResult == null && !checkResult.Any())
        {
            Console.WriteLine("DelegationPackage does not exist, nothing to delete.");
            return true;
        }

        var deleteResult = await delegationPackageRepository.DeleteCross(delegationId, packageId, options, cancellationToken: cancellationToken);
        if (deleteResult > 0)
        {
            return true;
        }

        return false;
    }

    private async Task<AssignmentPackage> GetOrCreateAssignmentPackage(Guid assignmentId, Guid packageId, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        var assignment = await assignmentRepository.Get(assignmentId, cancellationToken: cancellationToken);

        if (assignment == null)
        {
            throw new Exception("Assignment not found");
        }

        var userPackages = await GetConnectionPackages(assignment.FromId, options.ChangedBy, packageId, cancellationToken: cancellationToken);

        //// #568:AC:...
        if (!userPackages.Any())
        {
            throw new Exception("User does not have the package assigned on this entity");
        }

        if (!userPackages.Any(t => t.CanAssign))
        {
            throw new Exception("User can't assign package");
        }

        if (!userPackages.Any(t => t.Package.IsAssignable))
        {
            throw new Exception("Package is not assignable");
        }

        var filter = assignmentPackageRepository.CreateFilterBuilder();
        filter.Equal(t => t.AssignmentId, assignmentId);
        filter.Equal(t => t.PackageId, packageId);
        var checkResult = await assignmentPackageRepository.Get(filter, cancellationToken: cancellationToken);
        if (checkResult != null && checkResult.Any())
        {
            return checkResult.First();
        }

        var createResult = await assignmentPackageRepository.CreateCross(assignmentId, packageId, options, cancellationToken: cancellationToken);
        if (createResult > 0)
        {
            var createCheckResult = await assignmentPackageRepository.Get();
            if (createCheckResult != null && createCheckResult.Any())
            {
                return createCheckResult.First();
            }
        }

        throw new Exception(string.Format("Unable to add package to assignment ('{0}','{1}')", assignmentId, packageId));
    }

    private async Task<DelegationPackage> GetOrCreateDelegationPackage(Guid delegationId, Guid packageId, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        var delegation = await delegationRepository.Get(delegationId, cancellationToken: cancellationToken);

        if (delegation == null)
        {
            throw new Exception("Delegation not found");
        }

        // other logic?
        var userPackages = await GetConnectionPackages(delegation.FromId, options.ChangedBy, packageId, cancellationToken: cancellationToken);

        if (!userPackages.Any())
        {
            throw new Exception("User does not have the package assigned on this entity");
        }

        if (!userPackages.Any(t => t.CanAssign))
        {
            throw new Exception("User can't assign package");
        }

        if (!userPackages.Any(t => t.Package.IsAssignable))
        {
            throw new Exception("Package is not assignable");
        }


        var filter = delegationPackageRepository.CreateFilterBuilder();
        filter.Equal(t => t.DelegationId, delegationId);
        filter.Equal(t => t.PackageId, packageId);
        var checkResult = await delegationPackageRepository.Get(filter, cancellationToken: cancellationToken);
        if (checkResult != null && checkResult.Any())
        {
            return checkResult.First();
        }

        var createResult = await delegationPackageRepository.CreateCross(delegationId, packageId, options, cancellationToken: cancellationToken);
        if (createResult > 0)
        {
            var createCheckResult = await delegationPackageRepository.Get(filter, cancellationToken: cancellationToken);
            if (createCheckResult != null && createCheckResult.Any())
            {
                return createCheckResult.First();
            }
        }

        throw new Exception(string.Format("Unable to add package to delegation ('{0}','{1}')", delegationId, packageId));
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Resource>> GetResources(Guid id, CancellationToken cancellationToken = default)
    {
        return await connectionResourceRepository.GetB(id, cancellationToken: cancellationToken);
    }
}

/// <summary>
/// Convert database models to dto models
/// </summary>
public static class ConnectionConverter
{
    /// <summary>
    /// Convert database model to response model
    /// </summary>
    public static CreateDelegationResponse ConvertToResponseModel(Connection connection)
    {
        return new CreateDelegationResponse()
        {
            DelegationId = connection.Id,
            FromEntityId = connection.FromId
        };
    }

    /// <summary>
    /// Convert database model to dto model
    /// </summary>
    public static ConnectionDto ConvertToDto(ExtConnection connection)
    {
        return new ConnectionDto()
        {
            Id = connection.Id,
            From = connection.From,
            To = connection.To,
            Facilitator = connection.Facilitator,
            Role = ConvertToDto(connection.Role),
            FacilitatorRole = ConvertToDto(connection.FacilitatorRole),
            Delegation = connection.Delegation
        };
    }

    /// <summary>
    /// Convert database model to dto model
    /// </summary>
    public static RoleDto ConvertToDto(Role role)
    {
        return role != null
            ? new RoleDto()
            {
                Id = role.Id,
                Description = role.Description,
                Name = role.Name,
                Code = role.Code,
                Urn = role.Urn,
                IsKeyRole = role.IsKeyRole
            }
            : null;
    }
}
