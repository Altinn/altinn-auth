namespace Altinn.AccessMgmt.PersistenceEF.Audit;

/// <summary>
/// Attribute to decorate actions
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class AuditStaticDbAttribute : Attribute
{
    /// <summary>
    /// Token claim that gets set to ChangedBy. Claim must be UUID.
    /// </summary>
    public string ChangedBy { get; set; }

    /// <summary>
    /// Which system that initiates the request.
    /// </summary>
    public string System { get; set; }
}
