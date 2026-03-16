using System.Diagnostics.CodeAnalysis;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;

namespace Altinn.AccessMgmt.PersistenceEF.Constants;

/// <summary>
/// Defines constant <see cref="EntityType"/> instances used across the system.
/// Each entity type represents a category of actors in the access management domain,
/// with a fixed unique identifier (GUID), localized names, and an associated provider.
/// </summary>
public static class InstanceSourceTypeConstants
{
    /// <summary>
    /// Try to get <see cref="EntityType"/> by any identifier: Name or Guid.
    /// </summary>
    public static bool TryGetByAll(string value, [NotNullWhen(true)] out ConstantDefinition<InstanceSourceType>? result, bool includeTranslations = false)
    {
        if (TryGetByName(value, includeTranslations, out result))
        {
            return true;
        }

        if (Guid.TryParse(value, out var entityTypeGuid) && TryGetById(entityTypeGuid, out result))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Try to get <see cref="EntityType"/> by name.
    /// </summary>
    public static bool TryGetByName(string name, [NotNullWhen(true)] out ConstantDefinition<InstanceSourceType>? result)
        => ConstantLookup.TryGetByName(typeof(InstanceSourceTypeConstants), name, out result);

    /// <summary>
    /// Try to get <see cref="EntityType"/> by name.
    /// </summary>
    public static bool TryGetByName(string name, bool includeTranslations, [NotNullWhen(true)] out ConstantDefinition<InstanceSourceType>? result)
        => ConstantLookup.TryGetByName(typeof(InstanceSourceTypeConstants), name, includeTranslations, out result);

    /// <summary>
    /// Try to get <see cref="EntityType"/> using Guid.
    /// </summary>
    public static bool TryGetById(Guid id, [NotNullWhen(true)] out ConstantDefinition<InstanceSourceType>? result)
        => ConstantLookup.TryGetById(typeof(InstanceSourceTypeConstants), id, out result);

    /// <summary>
    /// Get all constants as a read-only collection.
    /// </summary>
    public static IReadOnlyCollection<ConstantDefinition<InstanceSourceType>> AllEntities()
        => ConstantLookup.AllEntities<InstanceSourceType>(typeof(InstanceSourceTypeConstants));

    /// <summary>
    /// Get all translations as read-only collection.
    /// </summary>
    public static IReadOnlyCollection<TranslationEntry> AllTranslations()
        => ConstantLookup.AllTranslations<InstanceSourceType>(typeof(InstanceSourceTypeConstants));

    /// <summary>
    /// Represents the instance delegation source type (Altinn App).
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> Unique identifier for the Altinn App instance delegation source type.  
    /// - <c>Name:</c> "Altinn App"  
    /// - <c>ProviderId:</c> References <see cref="ProviderConstants.Altinn3"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "Altinn App"  
    ///   - NN: "Altinn App"  
    /// </remarks>
    public static ConstantDefinition<InstanceSourceType> AltinnApp { get; } = new ConstantDefinition<InstanceSourceType>("019cd6c4-a340-776e-a63a-2370a05db6c7")
    {
        Entity = new()
        {
            Name = "Altinn App",
        },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Altinn App")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Altinn App")),
    };

    /// <summary>
    /// Represents the instance delegation source type (Sluttbruker).
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> Unique identifier for the End user instance delegation source type.  
    /// - <c>Name:</c> "Sluttbruker"  
    /// - <c>ProviderId:</c> References <see cref="ProviderConstants.Altinn3"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "End user"  
    ///   - NN: "Sluttbrukar"  
    /// </remarks>
    public static ConstantDefinition<InstanceSourceType> EndUser { get; } = new ConstantDefinition<InstanceSourceType>("019cd6c4-a340-7f7a-94af-f1181ec4a132")
    {
        Entity = new()
        {
            Name = "Sluttbruker",
        },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "End User")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Sluttbrukar")),
    };    
}
