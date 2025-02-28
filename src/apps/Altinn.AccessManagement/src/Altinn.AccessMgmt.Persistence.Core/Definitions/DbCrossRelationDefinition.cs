namespace Altinn.AccessMgmt.Persistence.Core.Definitions;

/// <summary>
/// Defines a cross-reference relationship between two entities through a junction table.
/// This allows filtering in both directions without duplication.
/// </summary>
public class DbCrossRelationDefinition
{
    /// <summary>
    /// The junction table that connects the two entities.
    /// </summary>
    public Type CrossType { get; }

    /// <summary>
    /// The junction table that connects the two entities.
    /// </summary>
    public Type CrossExtendedType { get; }

    /// <summary>
    /// The first type in the relationship.
    /// </summary>
    public Type AType { get; }

    /// <summary>
    /// The identity (primary key) property of A.
    /// </summary>
    public string AIdentityProperty { get; }

    /// <summary>
    /// The property in the junction table that references A.
    /// </summary>
    public string AReferenceProperty { get; }

    /// <summary>
    /// The second type in the relationship.
    /// </summary>
    public Type BType { get; }

    /// <summary>
    /// The identity (primary key) property of B.
    /// </summary>
    public string BIdentityProperty { get; }

    /// <summary>
    /// The property in the junction table that references B.
    /// </summary>
    public string BReferenceProperty { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DbCrossRelationDefinition"/> class.
    /// </summary>
    public DbCrossRelationDefinition(
        Type crossType,
        Type crossExtendedType,
        Type AType, 
        string AIdentityProperty, 
        string AReferenceProperty,
        Type BType, 
        string BIdentityProperty, 
        string BReferenceProperty)
    {
        this.CrossType = crossType;
        this.CrossExtendedType = crossExtendedType;
        this.AType = AType;
        this.AIdentityProperty = AIdentityProperty;
        this.AReferenceProperty = AReferenceProperty;
        this.BType = BType;
        this.BIdentityProperty = BIdentityProperty;
        this.BReferenceProperty = BReferenceProperty;
    }

    /// <summary>
    /// Checks if the given type is A.
    /// </summary>
    public bool IsA(Type type) => type == AType;

    /// <summary>
    /// Checks if the given type is B.
    /// </summary>
    public bool IsB(Type type) => type == BType;

    /// <summary>
    /// Gets the identity property (primary key) based on the given type.
    /// </summary>
    public string GetIdentityProperty(Type type) => IsA(type) ? AIdentityProperty : BIdentityProperty;

    /// <summary>
    /// Gets the corresponding reference property in the junction table for the given type.
    /// </summary>
    public string GetReferenceProperty(Type type) => IsA(type) ? AReferenceProperty : BReferenceProperty;

    /// <summary>
    /// Gets the reference property used to filter the related entity.
    /// </summary>
    public string GetFilterProperty(Type type) => IsA(type) ? BReferenceProperty : AReferenceProperty;
}
