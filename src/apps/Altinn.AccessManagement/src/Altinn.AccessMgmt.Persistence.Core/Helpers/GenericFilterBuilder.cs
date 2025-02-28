using System.Linq.Expressions;
using System.Reflection;

namespace Altinn.AccessMgmt.Persistence.Core.Helpers;

/// <summary>
/// Provides a fluent API for constructing a collection of <see cref="GenericFilter"/> objects based on expressions for the entity type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The entity type for which the filters are being built.</typeparam>
public class GenericFilterBuilder<T> : IEnumerable<GenericFilter>
{
    private readonly List<GenericFilter> _filters = new();

    /// <summary>
    /// Adds a filter condition to the builder using the specified property expression, value, and comparer.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="property">An expression selecting the property to filter on.</param>
    /// <param name="value">The value to compare against.</param>
    /// <param name="comparer">The type of comparison to perform. Defaults to <see cref="FilterComparer.Equals"/>.</param>
    /// <returns>The current instance of <see cref="GenericFilterBuilder{T}"/>, enabling a fluent API.</returns>
    public GenericFilterBuilder<T> Add<TProperty>(Expression<Func<T, TProperty>> property, TProperty value, FilterComparer comparer = FilterComparer.Equals)
    {
        var propertyInfo = ExtractPropertyInfo(property);
        _filters.Add(new GenericFilter(propertyInfo.Name, value!, comparer));
        return this;
    }

    /// <summary>
    /// Adds an equality filter condition for the specified property.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="property">An expression selecting the property to filter on.</param>
    /// <param name="value">The value the property should equal.</param>
    /// <returns>The current instance of <see cref="GenericFilterBuilder{T}"/>, enabling a fluent API.</returns>
    public GenericFilterBuilder<T> Equal<TProperty>(Expression<Func<T, TProperty>> property, TProperty value)
    {
        return Add(property, value, FilterComparer.Equals);
    }

    /// <summary>
    /// Extracts the <see cref="PropertyInfo"/> from the specified property expression.
    /// </summary>
    /// <typeparam name="TProperty">The type of the property.</typeparam>
    /// <param name="expression">An expression selecting a property of <typeparamref name="T"/>.</param>
    /// <returns>The <see cref="PropertyInfo"/> representing the property in the expression.</returns>
    /// <exception cref="ArgumentException">Thrown if the expression does not refer to a valid property.</exception>
    private PropertyInfo ExtractPropertyInfo<TProperty>(Expression<Func<T, TProperty>> expression)
    {
        MemberExpression? memberExpression = expression.Body switch
        {
            MemberExpression member => member,
            UnaryExpression { Operand: MemberExpression member } => member,
            _ => null
        };

        return memberExpression?.Member as PropertyInfo ?? throw new ArgumentException($"Expression '{expression}' does not refer to a valid property.");
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection of <see cref="GenericFilter"/> objects.
    /// </summary>
    /// <returns>An enumerator for the collection of filters.</returns>
    public IEnumerator<GenericFilter> GetEnumerator()
    {
        return _filters.GetEnumerator();
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection of <see cref="GenericFilter"/> objects.
    /// </summary>
    /// <returns>An enumerator for the collection of filters.</returns>
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
