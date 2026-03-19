using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

public interface IResourceService
{
    ValueTask<Resource> GetResource(Guid id, CancellationToken cancellationToken);

    ValueTask<Resource> GetResource(string refId, CancellationToken cancellationToken);

    ValueTask<Resource> GetResource(RequestReferenceDto reference, CancellationToken cancellationToken);
}
