namespace Altinn.AccessMgmt.Persistence.Core.Contracts;

/// <summary>
/// Defines a contract for database definition classes.
/// Implementations of this interface should provide logic to register or initialize the database schema, mappings,
/// and related configuration for an entity.
/// </summary>
public interface IDbDefinition
{
    /// <summary>
    /// Defines or registers the database schema and configuration for the associated entity.
    /// This method is invoked to initialize the necessary metadata for database operations.
    /// </summary>
    void Define();
}
