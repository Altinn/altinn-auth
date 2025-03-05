using System.Linq.Expressions;

namespace Altinn.AccessMgmt.Persistence.Core.Utilities.Search;

/// <summary>
/// Builder for configuring property-based search criteria with fuzzy matching.
/// Allows defining weighted properties and handling collections dynamically.
/// </summary>
/// <typeparam name="T">The type of objects being searched.</typeparam>
public class SearchPropertyBuilder<T>
{
    private readonly Dictionary<string, (Func<T, object> Selector, double Weight, FuzzynessLevel fuzzyness)> _properties = new();

    /// <summary>
    /// Adds a property to the search configuration.
    /// </summary>
    /// <param name="expression">An expression selecting the property from the object.</param>
    /// <param name="weight">The weight assigned to this property in search scoring.</param>
    /// <param name="fuzzyness">Defines the level of fuzziness applied in search matching</param>
    /// <returns>The current instance of <see cref="SearchPropertyBuilder{T}"/> for chaining.</returns>
    public SearchPropertyBuilder<T> Add(Expression<Func<T, object>> expression, double weight, FuzzynessLevel fuzzyness)
    {
        string propertyName = GetPropertyName(expression);
        _properties[propertyName] = (expression.Compile(), weight, fuzzyness);
        return this;
    }

    /// <summary>
    /// Adds a collection-based property to the search configuration.
    /// Supports both combined and detailed search modes.
    /// </summary>
    /// <typeparam name="TCollection">The type of elements in the collection.</typeparam>
    /// <param name="collectionSelector">An expression selecting the collection property from the object.</param>
    /// <param name="itemSelector">A function to extract the searchable string from each item in the collection.</param>
    /// <param name="weight">The weight assigned to this collection in search scoring.</param>
    /// <param name="fuzzyness">Defines the level of fuzziness applied in search matching</param>
    /// <param name="detailed">
    /// If <c>true</c>, treats each item in the collection as a separate searchable entity.
    /// If <c>false</c>, combines all items into a single searchable string.
    /// </param>
    /// <returns>The current instance of <see cref="SearchPropertyBuilder{T}"/> for chaining.</returns>
    public SearchPropertyBuilder<T> AddCollection<TCollection>(
        Expression<Func<T, IEnumerable<TCollection>>> collectionSelector,
        Func<TCollection, string> itemSelector,
        double weight, 
        FuzzynessLevel fuzzyness,
        bool detailed = false)
    {
        string propertyName = GetPropertyName(collectionSelector);

        if (detailed)
        {
            // Each item is treated as a separate entity
            _properties[$"{propertyName} (Detailed)"] = (pkg =>
                string.Join(" | ", collectionSelector.Compile()(pkg).Select(itemSelector)), weight, fuzzyness);
        }
        else
        {
            // All items are combined into a single searchable string
            _properties[$"{propertyName} (Combined)"] = (pkg =>
                string.Join(", ", collectionSelector.Compile()(pkg).Select(itemSelector)), weight, fuzzyness);
        }

        return this;
    }

    /// <summary>
    /// Builds and returns the configured property dictionary for search operations.
    /// </summary>
    /// <returns>A dictionary mapping property names to their search configurations.</returns>
    public Dictionary<string, (Func<T, object>, double, FuzzynessLevel)> Build()
    {
        return _properties;
    }

    /// <summary>
    /// Extracts the full property name from an expression, preserving nested properties.
    /// </summary>
    /// <typeparam name="T">The type of the object containing the property.</typeparam>
    /// <typeparam name="TProperty">The type of the property being extracted.</typeparam>
    /// <param name="expression">An expression selecting a property from the object.</param>
    /// <returns>
    /// A string representing the full property path, with nested properties joined by an underscore.
    /// Example: "Area_Group_Name" for `pkg.Area.Group.Name`.
    /// Returns "UnknownProperty" if the expression type is not recognized.
    /// </returns>
    private static string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> expression)
    {
        if (expression.Body is MemberExpression member)
        {
            return GetFullPropertyName(member);
        }

        if (expression.Body is UnaryExpression unary && unary.Operand is MemberExpression unaryMember)
        {
            return GetFullPropertyName(unaryMember);
        }

        if (expression.Body is MethodCallExpression methodCall)
        {
            return methodCall.Method.Name;
        }

        return "UnknownProperty";
    }

    /// <summary>
    /// Recursively constructs the full property path from a nested MemberExpression.
    /// </summary>
    /// <param name="member">The member expression representing the property.</param>
    /// <returns>
    /// A string containing the full property path, with each level separated by an underscore.
    /// Example: "Area_Group_Name" for a nested property structure.
    /// </returns>
    private static string GetFullPropertyName(MemberExpression member)
    {
        List<string> parts = new();
        while (member != null)
        {
            parts.Add(member.Member.Name);
            member = member.Expression as MemberExpression;
        }

        parts.Reverse();
        return string.Join("_", parts);
    }
}
