using Altinn.AccessMgmt.PersistenceEF.Extensions;

namespace Altinn.AccessMgmt.Core.Services.Contracts
{
    public interface IRightImportProgressService
    {
        /// <summary>
        /// Check if a given delegation change has already been processed
        /// </summary>
        /// <param name="delegationChangeId">The identificator unique for the delegation change inside the same origin</param>
        /// <param name="originType">the origin (ResourceRegistry, App or Instance</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<bool> IsImportAlreadyProcessed(long delegationChangeId, string originType, CancellationToken cancellationToken);

        /// <summary>
        /// Mark a given delegation change as already processed
        /// </summary>
        /// <param name="delegationChangeId">The identificator unique for the delegation change inside the same origin</param>
        /// <param name="originType">the origin (ResourceRegistry, App or Instance</param>
        /// <param name="audit">required to call Savechanges not used</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        /// <returns></returns>
        Task<bool> MarkImportAsProcessed(long delegationChangeId, string originType, AuditValues audit, CancellationToken cancellationToken);
    }
}
