using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Services;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;

namespace Altinn.AccessMgmt.Persistence.Repositories;

/// <summary>
/// Data service for AssignmentResource
/// </summary>
public class AssignmentResourceRepository : CrossRepository<AssignmentResource, ExtAssignmentResource, Assignment, Resource>, IAssignmentResourceRepository
{
    /// <inheritdoc/>
    public AssignmentResourceRepository(DbDefinitionRegistry dbDefinitionRegistry, IDbExecutor executor) : base(dbDefinitionRegistry, executor)
    {
    }
}
