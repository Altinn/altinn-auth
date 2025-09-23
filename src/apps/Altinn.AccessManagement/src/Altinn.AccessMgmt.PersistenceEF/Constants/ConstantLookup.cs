using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Altinn.AccessMgmt.PersistenceEF.Models.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Utils;

namespace Altinn.AccessMgmt.PersistenceEF.Constants;

/// <summary>
/// Provides lookup methods for constant definitions.
/// Supports lookups by ID, name, and retrieving all entities and translations.
/// </summary>
/// <remarks>
/// Uses reflection to find constants in a class and caches results.
/// ID lookups require <see cref="IEntityId"/>.
/// Name lookups require both <see cref="IEntityId"/> and <see cref="IEntityName"/>.
/// </remarks>
public static class ConstantLookup
{
    private static readonly ConcurrentDictionary<(Type ConstantsClass, Type EntityType), Dictionary<Guid, object>> _byId = new();
    private static readonly ConcurrentDictionary<(Type ConstantsClass, Type EntityType), Dictionary<string, object>> _byName = new();
    private static readonly ConcurrentDictionary<(Type ConstantsClass, Type EntityType), List<object>> _constants = new();

    private static List<ConstantDefinition<TType>> GetConstants<TType>(Type constantsClass)
        where TType : IEntityId
    {
        var key = (constantsClass, typeof(TType));
        var cachedConstants = _constants.GetOrAdd(key, _ =>
        {
            var constants = constantsClass
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(ConstantDefinition<TType>))
                .Select(p => (ConstantDefinition<TType>)p.GetValue(null)!)
                .ToList();

            return [.. constants.Cast<object>()];
        });

        return [.. cachedConstants.Cast<ConstantDefinition<TType>>()];
    }

    private static Dictionary<Guid, object> GetById<TType>(Type constantsClass)
        where TType : IEntityId
    {
        var key = (constantsClass, typeof(TType));
        return _byId.GetOrAdd(key, _ =>
        {
            var constants = GetConstants<TType>(constantsClass);
            return constants.ToDictionary<ConstantDefinition<TType>, Guid, object>(
                cd => cd.Entity.Id,
                cd => cd);
        });
    }

    private static Dictionary<string, object> GetByName<TType>(Type constantsClass)
        where TType : IEntityId, IEntityName
    {
        var key = (constantsClass, typeof(TType));
        return _byName.GetOrAdd(key, _ =>
        {
            var constants = GetConstants<TType>(constantsClass);
            return constants.ToDictionary<ConstantDefinition<TType>, string, object>(
                cd => cd.Entity.Name,
                cd => cd,
                StringComparer.OrdinalIgnoreCase);
        });
    }

    private static List<object> GetAllEntities<TType>(Type constantsClass)
        where TType : IEntityId
    {
        var constants = GetConstants<TType>(constantsClass);
        return [.. constants.Cast<object>()];
    }

    /// <summary>
    /// Try to get entity by ID for types that implement IEntityId.
    /// </summary>
    public static bool TryGetById<TType>(Type constantsClass, Guid id, [NotNullWhen(true)] out ConstantDefinition<TType>? result)
        where TType : IEntityId
    {
        var byId = GetById<TType>(constantsClass);
        if (byId.TryGetValue(id, out var value))
        {
            result = (ConstantDefinition<TType>)value;
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Try to get entity by name for types that implement both IEntityId and IEntityName.
    /// </summary>
    public static bool TryGetByName<TType>(Type constantsClass, string name, [NotNullWhen(true)] out ConstantDefinition<TType>? result)
        where TType : IEntityId, IEntityName
    {
        var byName = GetByName<TType>(constantsClass);
        if (byName.TryGetValue(name, out var value))
        {
            result = (ConstantDefinition<TType>)value;
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Get all constants as a read-only collection for types that implement IEntityId.
    /// </summary>
    public static IReadOnlyCollection<ConstantDefinition<TType>> AllEntities<TType>(Type constantsClass)
        where TType : IEntityId
    {
        var all = GetAllEntities<TType>(constantsClass);
        return [.. all.Cast<ConstantDefinition<TType>>()];
    }

    /// <summary>
    /// Get all translations as read-only collection for types that implement IEntityId.
    /// </summary>
    public static IReadOnlyCollection<TranslationEntry> AllTranslations<TType>(Type constantsClass)
        where TType : IEntityId
    {
        return [.. AllEntities<TType>(constantsClass).SelectMany(t => (List<TranslationEntry>)t)];
    }
}
