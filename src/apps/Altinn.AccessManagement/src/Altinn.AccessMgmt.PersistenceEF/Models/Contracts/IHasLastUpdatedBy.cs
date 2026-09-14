namespace Altinn.AccessMgmt.PersistenceEF.Models.Contracts;

/// <summary>
/// Implemented by models that expose the party behind their <c>Audit_ChangedBy</c> value.
/// The party is resolved by the service rather than through an EF relationship, so that no
/// foreign key is placed on an audit column: a stale audit value then leaves the navigation
/// null instead of being able to fail a deployment.
/// </summary>
public interface IHasLastUpdatedBy
{
    /// <summary>
    /// Identifier of the party that last changed the row.
    /// </summary>
    public Guid? Audit_ChangedBy { get; set; }

    /// <summary>
    /// The party that last changed the row, resolved from <see cref="Audit_ChangedBy"/>.
    /// </summary>
    public Entity? LastUpdatedBy { get; set; }
}
