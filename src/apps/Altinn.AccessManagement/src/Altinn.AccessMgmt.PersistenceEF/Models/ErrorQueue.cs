using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended delegation
/// </summary>
public class ErrorQueue : BaseErrorQueue
{
    /// <summary>
    /// The associated delegationChangeId (app/resource/instance)
    /// </summary>
    public long DelegationChangeId { get; set; }

    /// <summary>
    /// The origin of the change (app/resource/instance)
    /// </summary>
    public string OriginType { get; set; }

    /// <summary>
    /// Json containing the instance/app/resource
    /// </summary>
    public string ErrorItem { get; set; }

    /// <summary>
    /// Logs the error message associated with the failed processing attempt.
    /// </summary>
    public string ErrorMessage { get; set; }

    /// <summary>
    /// Flag to toggle ErrorQueue messages to reprocess instead of running new import.
    /// </summary>
    public bool ReProcess { get; set; }

    /// <summary>
    /// Flag to mark a element as reprocessed to see if element is still failing after reprocessing
    /// </summary>
    public bool Processed { get; set; }
}
