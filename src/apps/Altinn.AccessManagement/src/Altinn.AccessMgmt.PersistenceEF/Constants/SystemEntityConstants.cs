using System.Diagnostics.CodeAnalysis;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;

namespace Altinn.AccessMgmt.PersistenceEF.Constants;

/// <summary>
/// Defines constant <see cref="Entity"/> instances used for internal system entities.
/// Each constant represents a specific system entity used for audit and internal operations,
/// with a fixed unique identifier (GUID), name, and reference identifier.
/// </summary>
public static class SystemEntityConstants
{
    /// <summary>
    /// Try to get <see cref="Entity"/> by name.
    /// </summary>
    public static bool TryGetByName(string name, [NotNullWhen(true)] out ConstantDefinition<Entity>? result)
        => ConstantLookup.TryGetByName(typeof(SystemEntityConstants), name, out result);

    /// <summary>
    /// Try to get <see cref="Entity"/> using Guid.
    /// </summary>
    public static bool TryGetById(Guid id, [NotNullWhen(true)] out ConstantDefinition<Entity>? result)
        => ConstantLookup.TryGetById(typeof(SystemEntityConstants), id, out result);

    /// <summary>
    /// Get all constants as a read-only collection.
    /// </summary>
    public static IReadOnlyCollection<ConstantDefinition<Entity>> AllEntities()
        => ConstantLookup.AllEntities<Entity>(typeof(SystemEntityConstants));

    /// <summary>
    /// Get all translations as read-only collection.
    /// </summary>
    public static IReadOnlyCollection<TranslationEntry> AllTranslations()
        => ConstantLookup.AllTranslations<Entity>(typeof(SystemEntityConstants));

    #region Data Ingest Systems

    /// <summary>
    /// Represents the StaticDataIngest system entity.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 3296007f-f9ea-4bd0-b6a6-c8462d54633a
    /// - <c>Name:</c> StaticDataIngest
    /// - <c>RefId:</c> sys-static-data-ingest
    /// - <c>TypeId:</c> Internal entity type
    /// - <c>VariantId:</c> Standard variant
    /// </remarks>
    public static ConstantDefinition<Entity> StaticDataIngest { get; } = new ConstantDefinition<Entity>(AuditDefaults.StaticDataIngest)
    {
        Entity = new()
        {
            Name = nameof(AuditDefaults.StaticDataIngest),
            RefId = "sys-static-data-ingest",
            ParentId = null,
            TypeId = EntityTypeConstants.Internal,
            VariantId = EntityVariantConstants.Standard,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Static Data Ingest")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Statisk datainnlegging")),
    };

    /// <summary>
    /// Represents the RegisterImportSystem system entity.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> efec83fc-deba-4f09-8073-b4dd19d0b16b
    /// - <c>Name:</c> RegisterImportSystem
    /// - <c>RefId:</c> sys-register-import-system
    /// - <c>TypeId:</c> Internal entity type
    /// - <c>VariantId:</c> Standard variant
    /// </remarks>
    public static ConstantDefinition<Entity> RegisterImportSystem { get; } = new ConstantDefinition<Entity>(AuditDefaults.RegisterImportSystem)
    {
        Entity = new()
        {
            Name = nameof(AuditDefaults.RegisterImportSystem),
            RefId = "sys-register-import-system",
            ParentId = null,
            TypeId = EntityTypeConstants.Internal,
            VariantId = EntityVariantConstants.Standard,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Register Import System")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Registerimportsystem")),
    };

    /// <summary>
    /// Represents the ResourceRegistryImportSystem system entity.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 14fd92db-c124-4208-ba62-293cbabff2ad
    /// - <c>Name:</c> ResourceRegistryImportSystem
    /// - <c>RefId:</c> sys-resource-register-import-system
    /// - <c>TypeId:</c> Internal entity type
    /// - <c>VariantId:</c> Standard variant
    /// </remarks>
    public static ConstantDefinition<Entity> ResourceRegistryImportSystem { get; } = new ConstantDefinition<Entity>(AuditDefaults.ResourceRegistryImportSystem)
    {
        Entity = new()
        {
            Name = nameof(AuditDefaults.ResourceRegistryImportSystem),
            RefId = "sys-resource-register-import-system",
            ParentId = null,
            TypeId = EntityTypeConstants.Internal,
            VariantId = EntityVariantConstants.Standard,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Resource Registry Import System")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Ressursregisterimportsystem")),
    };

    /// <summary>
    /// Represents the InternalApiImportSystem system entity.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> b96cda05-c0e0-4c59-b4b8-f15a7dff9590
    /// - <c>Name:</c> InternalApiImportSystem
    /// - <c>RefId:</c> sys-internal-api-import-system
    /// - <c>TypeId:</c> Internal entity type
    /// - <c>VariantId:</c> Standard variant
    /// </remarks>
    public static ConstantDefinition<Entity> InternalApiImportSystem { get; } = new ConstantDefinition<Entity>(AuditDefaults.InternalApiImportSystem)
    {
        Entity = new()
        {
            Name = nameof(AuditDefaults.InternalApiImportSystem),
            RefId = "sys-internal-api-import-system",
            ParentId = null,
            TypeId = EntityTypeConstants.Internal,
            VariantId = EntityVariantConstants.Standard,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Internal API Import System")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Internt API-importsystem")),
    };

    #endregion

    #region API Systems

    /// <summary>
    /// Represents the EnduserApi system entity.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> ed771364-42a8-4934-801e-b482ed20ec3e
    /// - <c>Name:</c> EnduserApi
    /// - <c>RefId:</c> accessmgmt-enduser-api
    /// - <c>TypeId:</c> Internal entity type
    /// - <c>VariantId:</c> Standard variant
    /// </remarks>
    public static ConstantDefinition<Entity> EnduserApi { get; } = new ConstantDefinition<Entity>(AuditDefaults.EnduserApi)
    {
        Entity = new()
        {
            Name = nameof(AuditDefaults.EnduserApi),
            RefId = "accessmgmt-enduser-api",
            ParentId = null,
            TypeId = EntityTypeConstants.Internal,
            VariantId = EntityVariantConstants.Standard,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "End User API")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Sluttbruker-API")),
    };

    /// <summary>
    /// Represents the InternalApi system entity.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> b2b7dd36-8de5-40fb-a6ce-c7a4020f9ddc
    /// - <c>Name:</c> InternalApi
    /// - <c>RefId:</c> accessmgmt-internal-api
    /// - <c>TypeId:</c> Internal entity type
    /// - <c>VariantId:</c> Standard variant
    /// </remarks>
    public static ConstantDefinition<Entity> InternalApi { get; } = new ConstantDefinition<Entity>(AuditDefaults.InternalApi)
    {
        Entity = new()
        {
            Name = nameof(AuditDefaults.InternalApi),
            RefId = "accessmgmt-internal-api",
            ParentId = null,
            TypeId = EntityTypeConstants.Internal,
            VariantId = EntityVariantConstants.Standard,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Internal API")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Internt API")),
    };

    #endregion
}
