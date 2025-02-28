using System.Collections.Concurrent;
using System.Data;
using System.Text.Json;
using FastMember;

namespace Altinn.AccessMgmt.Persistence.Core.Utilities;

/// <summary>
/// DbConverter converts data from an IDataReader into objects of type T.
/// It leverages FastMember for fast member access and caches metadata for improved performance.
/// This version uses the list-handling logic from OldDbConverter while preserving the optimizations from DbConverter.
/// </summary>
public sealed class DbConverter : IDbConverter
{
    private static readonly Lazy<DbConverter> _instance = new Lazy<DbConverter>(() => new DbConverter());

    /// <summary>
    /// Gets the singleton instance of DbConverter.
    /// </summary>
    public static DbConverter Instance => _instance.Value;

    // Cache for TypeAccessor instances to avoid repeated reflection overhead.
    private static readonly ConcurrentDictionary<Type, TypeAccessor> AccessorCache = new ConcurrentDictionary<Type, TypeAccessor>();

    // Cache for property metadata per type. Keys are lower-cased property names.
    private static readonly ConcurrentDictionary<Type, Dictionary<string, (Member Member, Type? ElementType)>> PropertyCache = new ConcurrentDictionary<Type, Dictionary<string, (Member, Type?)>>();

    #region Caching and Reflection Helpers

    /// <summary>
    /// Retrieves or creates a TypeAccessor for the specified type.
    /// </summary>
    private static TypeAccessor GetAccessor(Type type) =>
        AccessorCache.GetOrAdd(type, t => TypeAccessor.Create(t));

    /// <summary>
    /// Retrieves or creates property metadata for the specified type.
    /// Each key is the lower-cased property name, and the value contains the Member info and, if applicable, the element type for lists.
    /// </summary>
    private static Dictionary<string, (Member Member, Type? ElementType)> GetProperties(Type type) =>
        PropertyCache.GetOrAdd(type, t =>
        {
            var accessor = GetAccessor(t);
            return accessor.GetMembers().ToDictionary(
                m => m.Name.ToLower(),
                m => (m, GetListOrEnumerableElementType(m.Type))
            );
        });

    /// <summary>
    /// Returns the element type if the provided type is a generic List or IEnumerable; otherwise, returns null.
    /// </summary>
    private static Type? GetListOrEnumerableElementType(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            return type.GetGenericArguments()[0];
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
        {
            return type.GetGenericArguments()[0];
        }

        return null;
    }

    #endregion

    #region Conversion Logic

    /// <summary>
    /// Converts the data from the provided IDataReader into a list of objects of type T.
    /// </summary>
    public List<T> ConvertToObjects<T>(IDataReader reader) 
        where T : new()
    {
        var results = new List<T>();

        while (reader.Read())
        {
            T instance = ProcessRow<T>(reader);
            results.Add(instance);
        }

        return results;
    }

    /// <summary>
    /// Processes a single row from the reader and maps it to an object of type T.
    /// </summary>
    private static T ProcessRow<T>(IDataReader reader) 
        where T : new()
    {
        T instance = new T();
        var accessor = GetAccessor(typeof(T));
        var properties = GetProperties(typeof(T));

        // Cache for already created sub-objects (for nested properties)
        var subObjectCache = new Dictionary<string, object>();

        for (int i = 0; i < reader.FieldCount; i++)
        {
            string columnName = reader.GetName(i).ToLower();

            // Skip if the column does not map to any property.
            if (!properties.TryGetValue(columnName, out var prop))
            {
                continue;
            }

            object? value = reader.IsDBNull(i) ? null : reader.GetValue(i);
            if (value == null)
            {
                continue;
            }

            if (prop.ElementType != null)
            {
                // Handle collection mapping using JSON deserialization.
                MapListProperty(instance, accessor, prop, value);
            }
            else if (prop.Member.Type.IsClass && prop.Member.Type != typeof(string))
            {
                // Handle nested sub-objects.
                MapSubObjectProperty(reader, instance, accessor, prop, subObjectCache, columnName);
            }
            else
            {
                // Handle simple property mapping.
                MapSimpleProperty(instance, accessor, prop, value);
            }
        }

        return instance;
    }

    /// <summary>
    /// Maps a simple (non-collection, non-complex) property using Convert.ChangeType.
    /// </summary>
    private static void MapSimpleProperty<T>(
        T instance,
        TypeAccessor accessor,
        (Member Member, Type? ElementType) prop,
        object value)
    {
        if (prop.Member.Type == typeof(DateTimeOffset) && value is DateTime dt)
        {
            var dateTimeOffset = new DateTimeOffset(DateTime.SpecifyKind(dt, DateTimeKind.Utc));
            accessor[instance, prop.Member.Name] = dateTimeOffset;
            return;
        }

        accessor[instance, prop.Member.Name] = Convert.ChangeType(value, prop.Member.Type);
    }

    /// <summary>
    /// Maps a property representing a list.
    /// Uses JSON deserialization with the property type from metadata.
    /// This logic is adapted from OldDbConverter.
    /// </summary>
    private static void MapListProperty<T>(
        T instance,
        TypeAccessor accessor,
        (Member Member, Type? ElementType) prop,
        object value)
    {
        // Use the actual property type (e.g., List<SomeType> or IEnumerable<SomeType>).
        var propertyType = prop.Member.Type;

        // Deserialize the JSON string into the list, using Web defaults.
        object? listValue = JsonSerializer.Deserialize(
            value?.ToString() ?? "[]",
            propertyType,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        );

        // Assign the deserialized list to the property.
        accessor[instance, prop.Member.Name] = listValue;
    }

    /// <summary>
    /// Maps a property representing a nested sub-object.
    /// It attempts to create or reuse the sub-object and map its properties from columns with a specific prefix.
    /// </summary>
    private static void MapSubObjectProperty<T>(
        IDataReader reader,
        T instance,
        TypeAccessor accessor,
        (Member Member, Type? ElementType) prop,
        Dictionary<string, object> subObjectCache,
        string columnName)
    {
        // Define a prefix for sub-object columns.
        string prefix = columnName + "_";

        // Retrieve or create the sub-object.
        if (!subObjectCache.TryGetValue(prefix, out object? subObject))
        {
            subObject = Activator.CreateInstance(prop.Member.Type)
                        ?? throw new InvalidOperationException($"Cannot create instance of type {prop.Member.Type}");
            subObjectCache[prefix] = subObject;
        }

        // Get metadata for the sub-object's properties.
        var subProperties = GetProperties(prop.Member.Type);
        foreach (var subProp in subProperties)
        {
            string subColumn = prefix + subProp.Key;
            int ordinal;
            try
            {
                ordinal = reader.GetOrdinal(subColumn);
            }
            catch (IndexOutOfRangeException)
            {
                continue; // Skip if the sub-column is not present.
            }

            if (!reader.IsDBNull(ordinal))
            {
                object? subValue = reader.GetValue(ordinal);
                if (subValue != null)
                {
                    GetAccessor(prop.Member.Type)[subObject, subProp.Value.Member.Name] =
                        Convert.ChangeType(subValue, subProp.Value.Member.Type);
                }
            }
        }

        // Assign the fully mapped sub-object.
        accessor[instance, prop.Member.Name] = subObject;
    }

    #endregion
}
