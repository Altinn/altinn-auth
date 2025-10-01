namespace Altinn.AccessMgmt.PersistenceEF.Audit;

/// <summary>
/// Attribute to decorate actions
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AuditJWTClaimToDbAttribute : Attribute
{
    /// <summary>
    /// Token claim that gets set to ChangedBy. Claim must be UUID.
    /// </summary>
    public string Claim { get; set; }

    /// <summary>
    /// Which system that initiates the request.
    /// </summary>
    public string System { get; set; }
}
