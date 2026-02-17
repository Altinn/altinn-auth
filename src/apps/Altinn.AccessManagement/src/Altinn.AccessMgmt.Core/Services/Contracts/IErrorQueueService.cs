using Altinn.AccessManagement.Core.Models;
using Altinn.AccessMgmt.Core.Utils.Models;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Services.Contracts
{
    public interface IErrorQueueService
    {
        /// <summary>
        /// Add an error to the queue
        /// </summary>
        /// <param name="error">the model to log</param>
        /// <param name="values">Audit values</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<bool> AddErrorQueue(ErrorQueue error, AuditValues values, CancellationToken cancellationToken);

        /// <summary>
        /// Retrieves all errors marked for ReProcessing of the defined type.
        /// </summary>
        /// <param name="type">the type to retrieve</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<List<ErrorQueue>> RetrieveItemsForReProcessing(string type, CancellationToken cancellationToken);

        /// <summary>
        /// Mark ErrorQueue element as processed
        /// </summary>
        /// <param name="id">The id of the error queue element to mark as processed</param>
        /// <param name="values">Audit values</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<bool> MarkErrorQueueElementProcessed(Guid id, AuditValues values, CancellationToken cancellationToken);
    }
}
