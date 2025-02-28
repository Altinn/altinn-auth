using System.Reflection;
using NpgsqlTypes;

namespace Altinn.AccessMgmt.Persistence.Core.Helpers;

/// <summary>
/// Helper class
/// </summary>
public static class DbHelperMethods
{
    /// <summary>
    /// Get the corresponding PostgreSQL type for a given property
    /// </summary>
    /// <param name="property">Property</param>
    /// <returns></returns>
    public static NpgsqlDbType GetPostgresType(PropertyInfo property)
    {
        return GetPostgresType(property.PropertyType);
    }

    /// <summary>
    /// Get the corresponding PostgreSQL type for a given .NET type
    /// </summary>
    /// <param name="type">Type</param>
    /// <returns></returns>
    public static NpgsqlDbType GetPostgresType(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is Type underlyingType)
        {
            type = underlyingType;
        }

        if (type == typeof(string))
        {
            return NpgsqlDbType.Text;
        }

        if (type == typeof(int))
        {
            return NpgsqlDbType.Integer;
        }

        if (type == typeof(long))
        {
            return NpgsqlDbType.Bigint;
        }

        if (type == typeof(short))
        {
            return NpgsqlDbType.Smallint;
        }

        if (type == typeof(Guid))
        {
            return NpgsqlDbType.Uuid;
        }

        if (type == typeof(bool))
        {
            return NpgsqlDbType.Boolean;
        }

        if (type == typeof(DateTime))
        {
            return NpgsqlDbType.Timestamp;
        }

        if (type == typeof(DateTimeOffset))
        {
            return NpgsqlDbType.TimestampTz;
        }

        if (type == typeof(float))
        {
            return NpgsqlDbType.Real;
        }

        if (type == typeof(double))
        {
            return NpgsqlDbType.Double;
        }

        if (type == typeof(decimal))
        {
            return NpgsqlDbType.Numeric;
        }

        //// Add more when needed

        throw new NotSupportedException($"Type '{type.Name}' is not supported for PostgreSQL mapping.");
    }
}
