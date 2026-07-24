namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection;

/// <summary>
/// The ConnectionQuery supports two directions of queries: FromOthers and ToOthers. This enum is used to specify the direction of the query.
/// </summary>
public enum ConnectionQueryDirection
{
    FromOthers,
    ToOthers
}
