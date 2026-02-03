using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

public interface IResourceService
{
    ValueTask<Resource> GetResource(Guid id, CancellationToken cancellationToken);

    ValueTask<Resource> GetResource(string refId, CancellationToken cancellationToken);
}
