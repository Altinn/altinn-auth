using Altinn.AccessMgmt.PersistenceEF.Models.Base;

namespace Altinn.AccessMgmt.PersistenceEF.Models;

/// <summary>
/// Extended Assignment
/// </summary>
public class Assignment : BaseAssignment
{
    /// <summary>
    /// Role
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// ActiveFrom (Entity)
    /// </summary>
    public Entity From { get; set; }

    /// <summary>
    /// ActiveTo (Entity)
    /// </summary>
    public Entity To { get; set; }
}
