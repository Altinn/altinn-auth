using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Definitions;
using Altinn.AccessMgmt.Persistence.Core.Services;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;

namespace Altinn.AccessMgmt.Persistence.Repositories;

/// <summary>
/// Data service for Assignment
/// </summary>
public class AssignmentRepository : ExtendedRepository<Assignment, ExtAssignment>, IAssignmentRepository
{
    /// <inheritdoc/>
    public AssignmentRepository(DbDefinitionRegistry dbDefinitionRegistry, IDbExecutor executor) : base(dbDefinitionRegistry, executor)
    {
    }
}
