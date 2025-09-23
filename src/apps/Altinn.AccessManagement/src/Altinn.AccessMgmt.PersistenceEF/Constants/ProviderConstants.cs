using System.Diagnostics.CodeAnalysis;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;

namespace Altinn.AccessMgmt.PersistenceEF.Constants;

/// <summary>
/// Defines a set of constant <see cref="Provider"/> instances used across the system.
/// Each constant represents a specific provider, such as system providers or service owners,
/// with a fixed unique identifier (GUID), code, name, and associated provider type.
/// </summary>
public static class ProviderConstants
{
    /// <summary>
    /// Try to get <see cref="Provider"/> by name.
    /// </summary>
    public static bool TryGetByName(string name, [NotNullWhen(true)] out ConstantDefinition<Provider>? result)
        => ConstantLookup.TryGetByName(typeof(ProviderConstants), name, out result);

    /// <summary>
    /// Try to get <see cref="Provider"/> using Guid.
    /// </summary>
    public static bool TryGetById(Guid id, [NotNullWhen(true)] out ConstantDefinition<Provider>? result)
        => ConstantLookup.TryGetById(typeof(ProviderConstants), id, out result);

    /// <summary>
    /// Get all constants as a read-only collection.
    /// </summary>
    public static IReadOnlyCollection<ConstantDefinition<Provider>> AllEntities() 
        => ConstantLookup.AllEntities<Provider>(typeof(ProviderConstants));

    /// <summary>
    /// Get all translations as read-only collection.
    /// </summary>
    public static IReadOnlyCollection<TranslationEntry> AllTranslations() 
        => ConstantLookup.AllTranslations<Provider>(typeof(ProviderConstants));

    /// <summary>
    /// Represents the Altinn 2 system provider.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> Unique identifier for Altinn 2.
    /// - <c>Code:</c> "sys-altinn2"
    /// - <c>Name:</c> "Altinn 2"
    /// - <c>TypeId:</c> Set to <see cref="ProviderTypeConstants.System"/>
    /// - <c>RefId:</c> Empty string since this is a system provider.
    /// </remarks>
    public static ConstantDefinition<Provider> Altinn2 { get; } = new ConstantDefinition<Provider>("0195ea92-2080-777d-8626-69c91ea2a05d")
    {
        Entity = new()
        {
            Name = "Altinn 2",
            Code = "sys-altinn2",
            TypeId = ProviderTypeConstants.System,
            RefId = string.Empty
        },
    };

    /// <summary>
    /// Represents the Altinn 3 service owner provider.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> Unique identifier for Altinn 3.
    /// - <c>Code:</c> "sys-altinn3"
    /// - <c>Name:</c> "Altinn 3"
    /// - <c>TypeId:</c> Set to <see cref="ProviderTypeConstants.System"/>
    /// - <c>RefId:</c> Empty string since this is a service owner.
    /// </remarks>
    public static ConstantDefinition<Provider> Altinn3 { get; } = new ConstantDefinition<Provider>("0195ea92-2080-7e7c-bbe3-bb0521c1e51a")
    {
        Entity = new()
        {
            Name = "Altinn 3",
            Code = "sys-altinn3",
            TypeId = ProviderTypeConstants.System,
            RefId = string.Empty
        },
    };

    /// <summary>
    /// Represents the Resource Registry system provider ("Ressursregisteret").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> Unique identifier for Resource Registry.
    /// - <c>Code:</c> "sys-resreg"
    /// - <c>Name:</c> "Ressursregisteret"
    /// - <c>TypeId:</c> Set to <see cref="ProviderTypeConstants.System"/>
    /// - <c>RefId:</c> Empty string since this is a system provider.
    /// </remarks>
    public static ConstantDefinition<Provider> ResourceRegistry { get; } = new ConstantDefinition<Provider>("0195ea92-2080-777d-8626-69c91ea2a05e")
    {
        Entity = new()
        {
            Name = "Ressursregisteret",
            Code = "sys-resreg",
            TypeId = ProviderTypeConstants.System,
            RefId = string.Empty
        },
    };

    /// <summary>
    /// Represents the Central Coordinating Register service owner provider ("Enhetsregisteret").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> Unique identifier for Central Coordinating Register (CCR).
    /// - <c>Code:</c> "sys-ccr"
    /// - <c>Name:</c> "Enhetsregisteret"
    /// - <c>TypeId:</c> Set to <see cref="ProviderTypeConstants.System"/>
    /// - <c>RefId:</c> Empty string since this is a service owner.
    /// </remarks>
    public static ConstantDefinition<Provider> CentralCoordinatingRegister { get; } = new ConstantDefinition<Provider>("0195ea92-2080-7e7c-bbe3-bb0521c1e52a")
    {
        Entity = new()
        {
            Name = "Enhetsregisteret",
            Code = "sys-ccr",
            TypeId = ProviderTypeConstants.System,
            RefId = string.Empty
        },
    };
}
