namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection;

/// <summary>
/// Represents a filter for querying connections based on various criteria.
/// </summary>
/// <remarks>This class provides properties to specify different filtering criteria for connection queries, such
/// as filtering by IDs of entities involved in the connection, roles, packages, and resources. It also includes options
/// to control the inclusion of additional data and the uniqueness of results.</remarks>
public sealed class ConnectionQueryFilter
{
    /// <summary>
    /// Gets the collection of source identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> FromIds { get; init; }

    /// <summary>
    /// Gets the collection of destination identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> ToIds { get; init; }

    /// <summary>
    /// Gets the collection of via identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> ViaIds { get; init; }

    /// <summary>
    /// Gets the collection of via role identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> ViaRoleIds { get; init; }

    /// <summary>
    /// Gets the collection of role identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> RoleIds { get; init; }

    /// <summary>
    /// Gets the collection of role identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> ExcludeRoleIds { get; init; }

    /// <summary>
    /// Gets the collection of package identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> PackageIds { get; init; }

    /// <summary>
    /// Gets the collection of resource identifiers.
    /// </summary>
    public IReadOnlyCollection<Guid> ResourceIds { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether only unique results should be returned.
    /// </summary>
    public bool OnlyUniqueResults { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to enrich entities with more details.
    /// </summary>
    public bool EnrichEntities { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include packages.
    /// </summary>
    public bool IncludePackages { get; init; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to include resources.
    /// </summary>
    public bool IncludeResources { get; init; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to include resources connected to packages.
    /// </summary>
    public bool EnrichPackageResources { get; init; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to include or exclude deleted entities.
    /// </summary>
    public bool ExcludeDeleted { get; init; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to include connections from delegations.
    /// </summary>
    public bool IncludeDelegation { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include connections calculated by keyrole.
    /// </summary>
    public bool IncludeKeyRole { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include sub-connections.
    /// </summary>
    public bool IncludeSubConnections { get; init; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include connections through main unit
    /// </summary>
    public bool IncludeMainUnitConnections { get; init; } = true;

    /// <summary>
    /// Returns true if at least one filter is provided.
    /// </summary>
    public bool HasAny =>
        FromIds?.Count > 0 ||
        ToIds?.Count > 0 ||
        RoleIds?.Count > 0 ||
        PackageIds?.Count > 0 ||
        ResourceIds?.Count > 0;

    /// <summary>
    /// Ensures that at least one filter parameter is set.
    /// </summary>
    public void Validate()
    {
        // Add more validation
        if (!HasAny)
        {
            throw new ArgumentException("At least one filter parameter must be set.");
        }
    }
}
