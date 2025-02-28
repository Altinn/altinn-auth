namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Delegation between two assignments
/// </summary>
public class Delegation
{
    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Assignment to delegate from
    /// </summary>
    public Guid FromId { get; set; }

    /// <summary>
    /// Assignment to delegate to
    /// </summary>
    public Guid ToId { get; set; }
}

/// <summary>
/// Extended delegation
/// </summary>
public class ExtDelegation : Delegation
{
    /// <summary>
    /// Assignment to delegate from
    /// </summary>
    public Assignment From { get; set; }

    /// <summary>
    /// Assignment to delegate to
    /// </summary>
    public Assignment To { get; set; }
}
