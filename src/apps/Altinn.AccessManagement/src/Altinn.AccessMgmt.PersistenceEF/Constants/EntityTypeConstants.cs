using System.Diagnostics.CodeAnalysis;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;

namespace Altinn.AccessMgmt.PersistenceEF.Constants;

/// <summary>
/// Defines constant <see cref="EntityType"/> instances used across the system.
/// Each entity type represents a category of actors in the access management domain,
/// with a fixed unique identifier (GUID), localized names, and an associated provider.
/// </summary>
public static class EntityTypeConstants
{
    /// <summary>
    /// Try to get <see cref="EntityType"/> by name.
    /// </summary>
    public static bool TryGetByName(string name, [NotNullWhen(true)] out ConstantDefinition<EntityType>? result)
        => ConstantLookup.TryGetByName(typeof(EntityTypeConstants), name, out result);

    /// <summary>
    /// Try to get <see cref="EntityType"/> using Guid.
    /// </summary>
    public static bool TryGetById(Guid id, [NotNullWhen(true)] out ConstantDefinition<EntityType>? result)
        => ConstantLookup.TryGetById(typeof(EntityTypeConstants), id, out result);

    /// <summary>
    /// Get all constants as a read-only collection.
    /// </summary>
    public static IReadOnlyCollection<ConstantDefinition<EntityType>> AllEntities() 
        => ConstantLookup.AllEntities<EntityType>(typeof(EntityTypeConstants));

    /// <summary>
    /// Get all translations as read-only collection.
    /// </summary>
    public static IReadOnlyCollection<TranslationEntry> AllTranslations()
        => ConstantLookup.AllTranslations<EntityType>(typeof(EntityTypeConstants));

    /// <summary>
    /// Represents the entity type for enterprise users ("Virksomhetbruker").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> Unique identifier for the enterprise user entity type.  
    /// - <c>Name:</c> "Virksomhetbruker"  
    /// - <c>ProviderId:</c> References <see cref="ProviderConstants.Altinn3"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "Enterprise user"  
    ///   - NN: "Verksemdbrukar"  
    /// </remarks>
    public static ConstantDefinition<EntityType> EnterpriseUser { get; } = new ConstantDefinition<EntityType>("870be6c4-b68f-4918-b897-12cdeae246de")
    {
        Entity = new()
        {
            Name = "Virksomhetbruker",
            ProviderId = ProviderConstants.Altinn3,
        },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Enterprise user")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Verksemdbrukar")),
    };

    /// <summary>
    /// Represents the entity type for self-identified entities ("Selvidentifisert").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> Unique identifier for the self-identified entity type.  
    /// - <c>Name:</c> "Selvidentifisert"  
    /// - <c>ProviderId:</c> References <see cref="ProviderConstants.Altinn3"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "Self-identified"  
    ///   - NN: "Sjølvidentifisert"  
    /// </remarks>
    public static ConstantDefinition<EntityType> SelfIdentified { get; } = new ConstantDefinition<EntityType>("fef80481-a798-4407-9e28-8fd167824434")
    {
        Entity = new()
        {
            Name = "Selvidentifisert",
            ProviderId = ProviderConstants.Altinn3,
        },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Self-identified")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Sjølvidentifisert")),
    };

    /// <summary>
    /// Represents the entity type for organizations ("Organisasjon").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> Unique identifier for the organization entity type.  
    /// - <c>Name:</c> "Organisasjon"  
    /// - <c>ProviderId:</c> References <see cref="ProviderConstants.Altinn3"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "Organization"  
    ///   - NN: "Organisasjon"  
    /// </remarks>
    public static ConstantDefinition<EntityType> Organisation { get; } = new ConstantDefinition<EntityType>("8c216e2f-afdd-4234-9ba2-691c727bb33d")
    {
        Entity = new()
        {
            Name = "Organisasjon",
            ProviderId = ProviderConstants.Altinn3,
        },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Organization")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Organisasjon")),
    };

    /// <summary>
    /// Represents the entity type for persons ("Person").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> Unique identifier for the person entity type.  
    /// - <c>Name:</c> "Person"  
    /// - <c>ProviderId:</c> References <see cref="ProviderConstants.Altinn3"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "Person"  
    ///   - NN: "Person"  
    /// </remarks>
    public static ConstantDefinition<EntityType> Person { get; } = new ConstantDefinition<EntityType>("bfe09e70-e868-44b3-8d81-dfe0e13e058a")
    {
        Entity = new()
        {
            Name = "Person",
            ProviderId = ProviderConstants.Altinn3,
        },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Person")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Person")),
    };

    /// <summary>
    /// Represents the entity type for system users ("Systembruker").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> Unique identifier for the system user entity type.  
    /// - <c>Name:</c> "Systembruker"  
    /// - <c>ProviderId:</c> References <see cref="ProviderConstants.Altinn3"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "SystemUser"  
    ///   - NN: "Systembrukar"  
    /// </remarks>
    public static ConstantDefinition<EntityType> SystemUser { get; } = new ConstantDefinition<EntityType>("fe643898-2f47-4080-85e3-86bf6fe39630")
    {
        Entity = new()
        {
            Name = "Systembruker",
            ProviderId = ProviderConstants.Altinn3,
        },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "SystemUser")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Systembrukar")),
    };

    /// <summary>
    /// Represents the entity type for internal actors ("Intern").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> Unique identifier for the internal entity type.  
    /// - <c>Name:</c> "Intern"  
    /// - <c>ProviderId:</c> References <see cref="ProviderConstants.Altinn3"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "Internal"  
    ///   - NN: "Intern"  
    /// </remarks>
    public static ConstantDefinition<EntityType> Internal { get; } = new ConstantDefinition<EntityType>("4557cc81-c10d-40b4-8134-f8825060016e")
    {
        Entity = new()
        {
            Name = "Intern",
            ProviderId = ProviderConstants.Altinn3,
        },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Internal")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Intern")),
    };
}
