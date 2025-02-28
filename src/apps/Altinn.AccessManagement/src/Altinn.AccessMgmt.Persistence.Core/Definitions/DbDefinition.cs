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
    /// Indicates whether the entity supports translations.
    /// </summary>
    public bool HasTranslation { get; set; } = false;

    /// <summary>
    /// Indicates whether the entity supports history tracking.
    /// </summary>
    public bool HasHistory { get; set; } = false;

    ///// <summary>
    ///// Gets or sets the default language code for translations (e.g., "nob").
    ///// </summary>
    //public string DefaultLanguage { get; set; } = "nob"; // no-NB?

    ///// <summary>
    ///// Gets or sets the name of the base schema where the entity's table is located.
    ///// </summary>
    //public string BaseSchema { get; set; } = "dbo";

    ///// <summary>
    ///// Gets or sets the name of the schema used for storing translations of the entity.
    ///// </summary>
    //public string TranslationSchema { get; set; } = "translation";

    ///// <summary>
    ///// Gets or sets the alias prefix used for translation views in queries.
    ///// </summary>
    //public string TranslationAliasPrefix { get; set; } = "t_"; // translation view name?

    ///// <summary>
    ///// Gets or sets the name of the schema used for storing historical records of the entity.
    ///// </summary>
    //public string BaseHistorySchema { get; set; } = "dbo_history";

    ///// <summary>
    ///// Gets or sets the alias prefix used for history views in queries.
    ///// </summary>
    //public string HistoryAliasPrefix { get; set; } = "h_";

    ///// <summary>
    ///// Gets or sets the name of the schema used for storing historical translations of the entity.
    ///// </summary>
    //public string TranslationHistorySchema { get; set; } = "translation_history";

    ///// <summary>
    ///// Gets or sets the name of the schema used for storing historical translations of the entity.
    ///// </summary>
    //public string DatabaseReadUser { get; set; } = "platform_authorization";
}
