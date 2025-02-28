using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;

namespace Altinn.AccessMgmt.Persistence.Repositories.Contracts;

/// <inheritdoc/>
public interface IAssignmentResourceRepository : IDbCrossRepository<AssignmentResource, ExtAssignmentResource, Assignment, Resource> { }
