using System.Diagnostics.CodeAnalysis;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;

namespace Altinn.AccessMgmt.PersistenceEF.Constants;

/// <summary>
/// Defines constant <see cref="AreaGroup"/> instances used in the system.
/// Each constant represents a specific area group for organizing areas
/// with a fixed unique identifier (GUID), name, description, and associated entity type.
/// </summary>
public static class AreaGroupConstants
{
    /// <summary>
    /// Try to get <see cref="AreaGroup"/> by name.
    /// </summary>
    public static bool TryGetByName(string name, [NotNullWhen(true)] out ConstantDefinition<AreaGroup>? result)
        => ConstantLookup.TryGetByName(typeof(AreaGroupConstants), name, out result);

    /// <summary>
    /// Try to get <see cref="AreaGroup"/> using Guid.
    /// </summary>
    public static bool TryGetById(Guid id, [NotNullWhen(true)] out ConstantDefinition<AreaGroup>? result)
        => ConstantLookup.TryGetById(typeof(AreaGroupConstants), id, out result);

    /// <summary>
    /// Get all constants as a read-only collection.
    /// </summary>
    public static IReadOnlyCollection<ConstantDefinition<AreaGroup>> AllEntities()
        => ConstantLookup.AllEntities<AreaGroup>(typeof(AreaGroupConstants));

    /// <summary>
    /// Get all translations as read-only collection.
    /// </summary>
    public static IReadOnlyCollection<TranslationEntry> AllTranslations()
        => ConstantLookup.AllTranslations<AreaGroup>(typeof(AreaGroupConstants));

    /// <summary>
    /// Represents the General area group.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 7e2a3af8-08cb-43a9-bdd7-7d5c7e377145
    /// - <c>Name:</c> "Allment"
    /// - <c>Description:</c> "Standard gruppe"
    /// - <c>EntityTypeId:</c> Organisation entity type
    /// </remarks>
    public static ConstantDefinition<AreaGroup> General { get; } = new ConstantDefinition<AreaGroup>("7e2a3af8-08cb-43a9-bdd7-7d5c7e377145")
    {
        Entity = new()
        {
            Name = "Allment",
            Description = "Standard gruppe",
            EntityTypeId = EntityTypeConstants.Organisation,
            Urn = null,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "General")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Allment")),
    };

    /// <summary>
    /// Represents the Industry area group.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 3757643a-316d-4d0e-a52b-4dc7cdebc0b4
    /// - <c>Name:</c> "Bransje"
    /// - <c>Description:</c> "For bransje grupper"
    /// - <c>EntityTypeId:</c> Organisation entity type
    /// </remarks>
    public static ConstantDefinition<AreaGroup> Industry { get; } = new ConstantDefinition<AreaGroup>("3757643a-316d-4d0e-a52b-4dc7cdebc0b4")
    {
        Entity = new()
        {
            Name = "Bransje",
            Description = "For bransje grupper",
            EntityTypeId = EntityTypeConstants.Organisation,
            Urn = null,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Industry")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Bransje")),
    };

    /// <summary>
    /// Represents the Special area group.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 554f0321-53b8-4d97-be12-6a585c507159
    /// - <c>Name:</c> "Særskilt"
    /// - <c>Description:</c> "For de sære tingene"
    /// - <c>EntityTypeId:</c> Organisation entity type
    /// </remarks>
    public static ConstantDefinition<AreaGroup> Special { get; } = new ConstantDefinition<AreaGroup>("554f0321-53b8-4d97-be12-6a585c507159")
    {
        Entity = new()
        {
            Name = "Særskilt",
            Description = "For de sære tingene",
            EntityTypeId = EntityTypeConstants.Organisation,
            Urn = null,
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Special")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Særskilt")),
    };
}
