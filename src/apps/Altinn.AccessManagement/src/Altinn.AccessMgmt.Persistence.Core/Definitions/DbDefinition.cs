namespace Altinn.AccessMgmt.Persistence.Core.Definitions;

/// <summary>
/// Represents the database definition for an entity, including its mapping details,
/// schema configuration, column definitions, relationships, and additional metadata
/// such as translation and history settings.
/// </summary>
/// <remarks>
/// This class provides the metadata required to map an entity to a database table,
/// specifying the table's schema, columns, foreign keys, unique constraints, and relations.
/// It also holds configuration settings for translation and history tracking.
/// </remarks>
public class DbDefinition(Type type)
{
    /// <summary>
    /// Gets or sets the CLR type that this database definition represents.
    /// </summary>
    public Type ModelType { get; set; } = type;

    /// <summary>
    /// Version
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Gets or sets the collection of column definitions for the entity.
    /// </summary>
    public List<DbPropertyDefinition> Properties { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of foreign key definitions representing relationships to other tables.
    /// </summary>
    public List<DbRelationDefinition> Relations { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of unique constraint definitions for the entity.
    /// </summary>
    public List<DbConstraintDefinition> Constraints { get; set; } = new();

    /// <summary>
    /// Gets or sets the collection of cross-relation definitions for the entity.
    /// </summary>
    public DbCrossRelationDefinition CrossRelation { get; set; }

    /// <summary>
    /// Indicates whether the entity is a view.
    /// </summary>
    public bool IsView { get; set; }

    /// <summary>
    /// The SQL query used in the view.
    /// </summary>
    public string ViewQuery { get; set; }

    /// <summary>
    /// Gets or sets the collection of types that this entity depends on in views.
    /// </summary>
    public List<Type> ViewDependencies { get; set; } = new();

    /// <summary>
    /// Indicates whether the tables supports history tracking.
    /// </summary>
    public bool EnableAudit { get; set; } = false;

    /// <summary>
    /// Will create a translation table
    /// </summary>
    public bool EnableTranslation { get; set; } = false;
}
