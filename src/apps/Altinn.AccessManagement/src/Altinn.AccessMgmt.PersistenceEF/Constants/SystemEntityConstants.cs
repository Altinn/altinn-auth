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
    /// Try to get <see cref="Entity"/> by any identifier: Name or Guid.
    /// </summary>
    public static bool TryGetByAll(string value, [NotNullWhen(true)] out ConstantDefinition<Entity>? result)
    {
        if (TryGetByName(value, out result))
        {
            return true;
        }

        if (Guid.TryParse(value, out var entityGuid) && TryGetById(entityGuid, out result))
        {
            return true;
        }

        return false;
    }

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

    /// <summary>
    /// Represents the InternalApiImportSystem system entity.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> f1be3999-68f6-4757-92b4-d3f3d33345e1
    /// - <c>Name:</c> InternalApiImportSystem
    /// - <c>RefId:</c> sys-internal-api-import-system
    /// - <c>TypeId:</c> Internal entity type
    /// - <c>VariantId:</c> Standard variant
    /// </remarks>
    public static ConstantDefinition<Entity> SingleRightImportSystem { get; } = new ConstantDefinition<Entity>(AuditDefaults.SingleRightImportSystem)
    {
        Entity = new()
        {
            Name = nameof(AuditDefaults.SingleRightImportSystem),
            RefId = "sys-single-right-import-system",
            ParentId = null,
            TypeId = EntityTypeConstants.Internal,
            VariantId = EntityVariantConstants.Standard,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Internal API Import System")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Internt API-importsystem")),
    };

    /// <summary>
    /// Represents the Internal Instance Delegation ImportSystem system entity.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 07ab1b35-0299-4667-9dde-4c3f75d9bbeb
    /// - <c>Name:</c> Internal Instance Delegation Import System
    /// - <c>RefId:</c> sys-instance-right-import-system
    /// - <c>TypeId:</c> Internal entity type
    /// - <c>VariantId:</c> Standard variant
    /// </remarks>
    public static ConstantDefinition<Entity> InstanceRightImportSystem { get; } = new ConstantDefinition<Entity>(AuditDefaults.InstanceRightImportSystem)
    {
        Entity = new()
        {
            Name = nameof(AuditDefaults.InstanceRightImportSystem),
            RefId = "sys-instance-right-import-system",
            ParentId = null,
            TypeId = EntityTypeConstants.Internal,
            VariantId = EntityVariantConstants.Standard,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Internal Instance Delegation Import System")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Internt Instans Delegering-importsystem")),
    };

    /// <summary>
    /// Represents the Altinn2RoleImportSystem system entity.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 46cfa478-971f-446e-9bc1-af57469361d0
    /// - <c>Name:</c> Altinn2RoleImportSystem
    /// - <c>RefId:</c> sys-altinn2-role-import-system
    /// - <c>TypeId:</c> Internal entity type
    /// - <c>VariantId:</c> Standard variant
    /// </remarks>
    public static ConstantDefinition<Entity> Altinn2RoleImportSystem { get; } = new ConstantDefinition<Entity>(AuditDefaults.Altinn2RoleImportSystem)
    {
        Entity = new()
        {
            Name = nameof(AuditDefaults.Altinn2RoleImportSystem),
            RefId = "sys-altinn2-role-import-system",
            ParentId = null,
            TypeId = EntityTypeConstants.Internal,
            VariantId = EntityVariantConstants.Standard,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Altinn2 Role Import System")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Altinn2 Role-importsystem")),
    };

    /// <summary>
    /// Represents the Altinn2AddRulesApi system entity.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0d8c172d-64e4-4aa2-8233-91ad32aa0f9d
    /// - <c>Name:</c> Altinn2AddRulesApi
    /// - <c>RefId:</c> sys-altinn2-addrules-api-system
    /// - <c>TypeId:</c> Internal entity type
    /// - <c>VariantId:</c> Standard variant
    /// </remarks>
    public static ConstantDefinition<Entity> Altinn2AddRulesApi { get; } = new ConstantDefinition<Entity>(AuditDefaults.Altinn2AddRulesApi)
    {
        Entity = new()
        {
            Name = nameof(AuditDefaults.Altinn2AddRulesApi),
            RefId = "sys-altinn2-addrules-api-system",
            ParentId = null,
            TypeId = EntityTypeConstants.Internal,
            VariantId = EntityVariantConstants.Standard,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Altinn2 AddRules API System")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Altinn2 AddRules API System")),
    };

    /// <summary>
    /// Represents the LegacySingleRightsApi system entity.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> b1b52707-bd24-4f01-9bb9-3b0b6d6bd8d6
    /// - <c>Name:</c> LegacySingleRightsApi
    /// - <c>RefId:</c> sys-legacy-singlerights-api-system
    /// - <c>TypeId:</c> Internal entity type
    /// - <c>VariantId:</c> Standard variant
    /// </remarks>
    public static ConstantDefinition<Entity> LegacySingleRightsApi { get; } = new ConstantDefinition<Entity>(AuditDefaults.LegacySingleRightsApi)
    {
        Entity = new()
        {
            Name = nameof(AuditDefaults.LegacySingleRightsApi),
            RefId = "sys-legacy-singlerights-api-system",
            ParentId = null,
            TypeId = EntityTypeConstants.Internal,
            VariantId = EntityVariantConstants.Standard,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Legacy Single Rights API System")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Legacy Single Rights API System")),
    };

    /// <summary>
    /// Represents the LegacyMaskinportenSchemaApi system entity.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> cfd2991c-bbdf-43b5-8b10-1ba1dfaed167
    /// - <c>Name:</c> LegacyMaskinportenSchemaApi
    /// - <c>RefId:</c> sys-legacy-maskinportenschema-api-system
    /// - <c>TypeId:</c> Internal entity type
    /// - <c>VariantId:</c> Standard variant
    /// </remarks>
    public static ConstantDefinition<Entity> LegacyMaskinportenSchemaApi { get; } = new ConstantDefinition<Entity>(AuditDefaults.LegacyMaskinportenSchemaApi)
    {
        Entity = new()
        {
            Name = nameof(AuditDefaults.LegacyMaskinportenSchemaApi),
            RefId = "sys-legacy-maskinportenschema-api-system",
            ParentId = null,
            TypeId = EntityTypeConstants.Internal,
            VariantId = EntityVariantConstants.Standard,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Legacy MaskinportenSchema API System")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Legacy MaskinportenSchema API System")),
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

    /// <summary>
    /// Represents the ServiceOwnerApi system entity.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> ab418674-710b-4ca0-8ff1-b7daac8dc3d0
    /// - <c>Name:</c> Service Owner API
    /// - <c>RefId:</c> accessmgmt-service-owner-api
    /// - <c>TypeId:</c> Internal entity type
    /// - <c>VariantId:</c> Standard variant
    /// </remarks>
    public static ConstantDefinition<Entity> ServiceOwnerApi { get; } = new ConstantDefinition<Entity>(AuditDefaults.ServiceOwnerApi)
    {
        Entity = new()
        {
            Name = nameof(AuditDefaults.ServiceOwnerApi),
            RefId = "accessmgmt-service-owner-api",
            ParentId = null,
            TypeId = EntityTypeConstants.Internal,
            VariantId = EntityVariantConstants.Standard,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Service owner API")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Tjenesteeier API")),
    };

    /// <summary>
    /// Represents the AppInstanceDelegationApi system entity.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 81435d97-8527-4246-a475-ce281a898fa0
    /// - <c>Name:</c> App Instance Delegation API
    /// - <c>RefId:</c> accessmgmt-app-instance-delegation-api
    /// - <c>TypeId:</c> Internal entity type
    /// - <c>VariantId:</c> Standard variant
    /// </remarks>
    public static ConstantDefinition<Entity> AppInstanceDelegationApi { get; } = new ConstantDefinition<Entity>(AuditDefaults.AppInstanceDelegationApi)
    {
        Entity = new()
        {
            Name = nameof(AuditDefaults.AppInstanceDelegationApi),
            RefId = "accessmgmt-app-instance-delegation-api",
            ParentId = null,
            TypeId = EntityTypeConstants.Internal,
            VariantId = EntityVariantConstants.Standard,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "App instance delegation API")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "App Instans delegerings API")),
    };

    #endregion

    #region Internal

    /// <summary>
    /// Represents the DBA system entity.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0195efb8-7c80-7262-b616-7d9eb843bcaa
    /// - <c>Name:</c> DBA
    /// - <c>RefId:</c> sys-dba
    /// - <c>TypeId:</c> Internal entity type
    /// - <c>VariantId:</c> Standard variant
    /// </remarks>
    public static ConstantDefinition<Entity> DBA { get; } = new ConstantDefinition<Entity>(AuditDefaults.DBA)
    {
        Entity = new()
        {
            Name = nameof(AuditDefaults.DBA),
            RefId = "sys-dba",
            ParentId = null,
            TypeId = EntityTypeConstants.Internal,
            VariantId = EntityVariantConstants.Standard,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "DBA")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "DBA")),
    };

    /// <summary>
    /// Represents the Outbox system entity.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 2b0e4b45-b5ac-41a9-b063-bba1dc9410e6
    /// - <c>Name:</c> Outbox
    /// - <c>RefId:</c> sys-outbox
    /// - <c>TypeId:</c> Internal entity type
    /// - <c>VariantId:</c> Standard variant
    /// </remarks>
    public static ConstantDefinition<Entity> Outbox { get; } = new ConstantDefinition<Entity>(AuditDefaults.Outbox)
    {
        Entity = new()
        {
            Name = nameof(AuditDefaults.Outbox),
            RefId = "sys-outbox",
            ParentId = null,
            TypeId = EntityTypeConstants.Internal,
            VariantId = EntityVariantConstants.Standard,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Outbox")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Outbox")),
    };

    #endregion
}
