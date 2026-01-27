using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Services.Contracts;

public interface IResourceService
{
    ValueTask<Resource> GetResource(Guid id, CancellationToken cancellationToken);

    ValueTask<Resource> GetResource(string refId, CancellationToken cancellationToken);
}
