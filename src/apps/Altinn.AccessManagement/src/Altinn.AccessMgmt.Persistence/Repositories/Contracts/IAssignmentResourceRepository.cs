using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Models;

namespace Altinn.AccessMgmt.Persistence.Repositories.Contracts;

/// <inheritdoc/>
public interface IAssignmentResourceRepository : IDbCrossRepository<AssignmentResource, ExtAssignmentResource, Assignment, Resource> { }
