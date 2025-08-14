namespace Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;

/// <summary>
/// Adds audit properties to model
/// </summary>
public interface IAudit
{
    /// <summary>
    /// ValidFrom
    /// </summary>
    public DateTime Audit_ValidFrom { get; set; }

    /// <summary>
    /// ValidTo
    /// </summary>
    public DateTime? Audit_ValidTo { get; set; }

    /// <summary>
    /// ChangedBy
    /// </summary>
    public Guid? Audit_ChangedBy { get; set; }

    /// <summary>
    /// ChangedBySystem
    /// </summary>
    public Guid? Audit_ChangedBySystem { get; set; }

    /// <summary>
    /// ChangeOperation
    /// </summary>
    public string Audit_ChangeOperation { get; set; }

    /// <summary>
    /// DeletedBy
    /// </summary>
    public Guid? Audit_DeletedBy { get; set; }

    /// <summary>
    /// DeletedBySystem
    /// </summary>
    public Guid? Audit_DeletedBySystem { get; set; }

    /// <summary>
    /// DeleteOperation
    /// </summary>
    public string Audit_DeleteOperation { get; set; }
}
