using System.Diagnostics.CodeAnalysis;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;

namespace Altinn.AccessMgmt.PersistenceEF.Constants;

/// <summary>
/// Defines constant <see cref="EntityVariant"/> instances used across the system.
/// Each entity variant represents a specific classification of an entity,
/// with a fixed unique identifier (GUID), localized names, and an associated entity type.
/// </summary>
public static class EntityVariantConstants
{
    /// <summary>
    /// Try to get <see cref="EntityVariant"/> name (e.g. "SAM").
    /// </summary>
    public static bool TryGetByName(string name, [NotNullWhen(true)] out ConstantDefinition<EntityVariant>? result)
        => ConstantLookup.TryGetByName(typeof(EntityVariantConstants), name, out result);

    /// <summary>
    /// Try to get <see cref="EntityVariant"/> using Guid.
    /// </summary>
    public static bool TryGetById(Guid id, [NotNullWhen(true)] out ConstantDefinition<EntityVariant>? result)
        => ConstantLookup.TryGetById(typeof(EntityVariantConstants), id, out result);

    /// <summary>
    /// Get all constants as a read-only collection.
    /// </summary>
    public static IReadOnlyCollection<ConstantDefinition<EntityVariant>> AllEntities()
        => ConstantLookup.AllEntities<EntityVariant>(typeof(EntityVariantConstants));

    /// <summary>
    /// Get all translations as read-only collection.
    /// </summary>
    public static IReadOnlyCollection<TranslationEntry> AllTranslations()
        => ConstantLookup.AllTranslations<EntityVariant>(typeof(EntityVariantConstants));

    /// <summary>
    /// Represents the entity variant for "Frivillig registrert utleiebygg." (UTBG).
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 99a54a28-52d3-4608-9298-94081bb3f3d2
    /// - <c>Name:</c> "UTBG"
    /// - <c>Description:</c> "Frivillig registrert utleiebygg."
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "UTBG" / "Voluntarily registered rental building."  
    ///   - NN: "UTBG" / "Frivillig registrert utleigebygg."
    /// </remarks>
    public static ConstantDefinition<EntityVariant> UTBG { get; } = new ConstantDefinition<EntityVariant>("99a54a28-52d3-4608-9298-94081bb3f3d2")
    {
        Entity = new EntityVariant() { TypeId = EntityTypeConstants.Organisation, Name = "UTBG", Description = "Frivillig registrert utleiebygg." },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "UTBG"), KeyValuePair.Create("Description", "Voluntarily registered rental building.")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "UTBG"), KeyValuePair.Create("Description", "Frivillig registrert utleigebygg.")),
    };

    /// <summary>
    /// Represents the entity variant for "Enkeltpersoner som registreres i Revisorregisteret." (REV).
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 2d9371de-576b-42fc-94b9-7e80b2467982
    /// - <c>Name:</c> "REV"  
    /// - <c>Description:</c> "Enkeltpersoner som registreres i Revisorregisteret."
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "REV" / "Individuals who are registered in the Register of Auditors."  
    ///   - NN: "REV" / "Einskildpersonar som blir registrerte i Revisorregisteret."  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> REV { get; } = new ConstantDefinition<EntityVariant>("2d9371de-576b-42fc-94b9-7e80b2467982")
    {
        Entity = new EntityVariant() { TypeId = EntityTypeConstants.Organisation, Name = "REV", Description = "Enkeltpersoner som registreres i Revisorregisteret." },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "REV"), KeyValuePair.Create("Description", "Individuals who are registered in the Register of Auditors.")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "REV"), KeyValuePair.Create("Description", "Enkeltpersonar som blir registrerte i Revisorregisteret.")),
    };

    /// <summary>
    /// Represents the entity variant for "Norskkontrollert utenlandsk selskap" (NOKUS).
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 6cfce90e-1d84-48b6-ba32-cc4c6f81ae04  
    /// - <c>Name:</c> "NOKUS"  
    /// - <c>Description:</c> "Norskkontrollert utenlandsk selskap."
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "NOKUS" / "Norwegian-controlled foreign company."  
    ///   - NN: "NOKUS" / "Norskkontrollert utlendsk selskap."  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> NOKUS { get; } = new ConstantDefinition<EntityVariant>("6cfce90e-1d84-48b6-ba32-cc4c6f81ae04")
    {
        Entity = new EntityVariant() { TypeId = EntityTypeConstants.Organisation, Name = "NOKUS", Description = "Norskkontrollert utenlandsk selskap." },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "NOKUS"), KeyValuePair.Create("Description", "Norwegian-controlled foreign company")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "NOKUS"), KeyValuePair.Create("Description", "Norskkontrollert utanlandsk selskap.")),
    };

    /// <summary>
    /// Represents the entity variant for "Virksomhet drevet i fellesskap" (VIFE).
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 6301535b-0734-4024-8ca1-d0a6a6efadf5  
    /// - <c>Name:</c> "UTLANS"  
    /// - <c>Description:</c> "Utenlandsk ansvarlig selskap."
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "VIFE" / "Foreign general partnership (or foreign unlimited partnership, depending on legal context)."  
    ///   - NN: "VIFE" / "Utlendsk ansvarleg selskap."  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> UTLANS { get; } = new ConstantDefinition<EntityVariant>("6301535b-0734-4024-8ca1-d0a6a6efadf5")
    {
        Entity = new EntityVariant() { TypeId = EntityTypeConstants.Organisation, Name = "UTLANS", Description = "Utenlandsk ansvarlig selskap." },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "UTLANS"), KeyValuePair.Create("Description", "Foreign general partnership (or foreign unlimited partnership, depending on legal context).")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "UTLANS"), KeyValuePair.Create("Description", "Utanlandsk ansvarleg selskap.")),
    };

    /// <summary>
    /// Represents the entity variant for "Virksomhet drevet i fellesskap" (VIFE).
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> e7ed50fc-65c9-442b-973c-89931ff5aeb6  
    /// - <c>Name:</c> "VIFE"  
    /// - <c>Description:</c> "Virksomhet drevet i fellesskap, jf. mval § 12, 4. ledd"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "VIFE" / "Business carried out jointly, cf. VAT Act § 12, fourth paragraph."  
    ///   - NN: "VIFE" / "Verksemd driven i fellesskap, jf. mval § 12, 4. ledd"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> VIFE { get; } = new ConstantDefinition<EntityVariant>("e7ed50fc-65c9-442b-973c-89931ff5aeb6")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "VIFE", Description = "Virksomhet drevet i fellesskap, jf mval § 12, 4.le" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "VIFE"), KeyValuePair.Create("Description", "Business carried out jointly, cf. VAT Act § 12, fourth paragraph.")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "VIFE"), KeyValuePair.Create("Description", "Verksemd driven i fellesskap, jf. mval § 12, 4. ledd")),
    };

    /// <summary>
    /// Represents the entity variant for "Indre selskap" (IS).
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 6dbc447a-3306-4fd0-a7cf-fbd725bbe170  
    /// - <c>Name:</c> "IS"  
    /// - <c>Description:</c> "Et samarbeidsarrangement der en eller flere aktører driver næringsvirksomhet for felles regning og risiko."  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "IS" / "A cooperative arrangement where one or more parties conduct business activities for joint account and risk."  
    ///   - NN: "IS" / "Eit samarbeidsopplegg der ein eller fleire aktørar driv næringsverksemd for felles rekning og risiko."  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> IS { get; } = new ConstantDefinition<EntityVariant>("6dbc447a-3306-4fd0-a7cf-fbd725bbe170")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "IS", Description = "Et samarbeidsarrangement der en eller flere aktører driver næringsvirksomhet for felles regning og risiko." },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "IS"), KeyValuePair.Create("Description", "A cooperative arrangement where one or more parties conduct business activities for joint account and risk.")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "IS"), KeyValuePair.Create("Description", "Eit samarbeidsopplegg der ein eller fleire aktørar driv næringsverksemd for felles rekning og risiko.")),
    };

    /// <summary>
    /// Represents the entity variant for legal co-ownership ("SAM").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> d786bc0e-8e9e-4116-bfc2-0344207c9127  
    /// - <c>Name:</c> "SAM"  
    /// - <c>Description:</c> "Tingsrettslig sameie"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "SAM" / "Legal co-ownership"  
    ///   - NN: "SAM" / "Tingsrettslig sameie"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> SAM { get; } = new ConstantDefinition<EntityVariant>("d786bc0e-8e9e-4116-bfc2-0344207c9127")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "SAM", Description = "Tingsrettslig sameie" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "SAM"), KeyValuePair.Create("Description", "Legal co-ownership")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "SAM"), KeyValuePair.Create("Description", "Tingsrettslig sameie"))
    };

    /// <summary>
    /// Represents the entity variant for securities fund ("VPFO").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> d0a08401-5ae0-4da9-a79a-1113a7746b60  
    /// - <c>Name:</c> "VPFO"  
    /// - <c>Description:</c> "Verdipapirfond"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "VPFO" / "Securities fund"  
    ///   - NN: "VPFO" / "Verdipapirfond"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> VPFO { get; } = new ConstantDefinition<EntityVariant>("d0a08401-5ae0-4da9-a79a-1113a7746b60")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "VPFO", Description = "Verdipapirfond" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "VPFO"), KeyValuePair.Create("Description", "Securities fund")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "VPFO"), KeyValuePair.Create("Description", "Verdipapirfond"))
    };

    /// <summary>
    /// Represents the entity variant for foreign entity ("UTLA").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> c161a605-3c72-40f2-8a5a-15e57e49638c  
    /// - <c>Name:</c> "UTLA"  
    /// - <c>Description:</c> "Utenlandsk enhet"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "UTLA" / "Foreign entity"  
    ///   - NN: "UTLA" / "Utanlandsk eining"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> UTLA { get; } = new ConstantDefinition<EntityVariant>("c161a605-3c72-40f2-8a5a-15e57e49638c")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "UTLA", Description = "Utenlandsk enhet" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "UTLA"), KeyValuePair.Create("Description", "Foreign entity")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "UTLA"), KeyValuePair.Create("Description", "Utanlandsk eining"))
    };

    /// <summary>
    /// Represents the entity variant for other estate ("BO").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 752f87dc-b04f-42cb-becd-173935ec6164  
    /// - <c>Name:</c> "BO"  
    /// - <c>Description:</c> "Andre bo"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "BO" / "Other estate"  
    ///   - NN: "BO" / "Andre bo"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> BO { get; } = new ConstantDefinition<EntityVariant>("752f87dc-b04f-42cb-becd-173935ec6164")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "BO", Description = "Andre bo" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "BO"), KeyValuePair.Create("Description", "Other estate")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "BO"), KeyValuePair.Create("Description", "Andre bo"))
    };

    /// <summary>
    /// Represents the entity variant for limited company ("AS").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 263762ec-54fc-4eae-b7a1-17e92eea9a5c  
    /// - <c>Name:</c> "AS"  
    /// - <c>Description:</c> "Aksjeselskap"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "AS" / "Limited company"  
    ///   - NN: "AS" / "Aksjeselskap"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> AS { get; } = new ConstantDefinition<EntityVariant>("263762ec-54fc-4eae-b7a1-17e92eea9a5c")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "AS", Description = "Aksjeselskap" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "AS"), KeyValuePair.Create("Description", "Limited company")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "AS"), KeyValuePair.Create("Description", "Aksjeselskap"))
    };

    /// <summary>
    /// Represents the entity variant for pension fund ("PK").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 6b2449e7-af5a-4c4e-b475-1b75998ba804  
    /// - <c>Name:</c> "PK"  
    /// - <c>Description:</c> "Pensjonskasse"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "PK" / "Pension fund"  
    ///   - NN: "PK" / "Pensjonskasse"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> PK { get; } = new ConstantDefinition<EntityVariant>("6b2449e7-af5a-4c4e-b475-1b75998ba804")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "PK", Description = "Pensjonskasse" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "PK"), KeyValuePair.Create("Description", "Pension fund")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "PK"), KeyValuePair.Create("Description", "Pensjonskasse"))
    };

    /// <summary>
    /// Represents the entity variant for other individuals registered in the associated register ("PERS").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> ed5d05b6-588c-40fa-8885-2bd36f75ac34  
    /// - <c>Name:</c> "PERS"  
    /// - <c>Description:</c> "Andre enkeltpersoner som registreres i tilknyttet register"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "PERS" / "Other individuals registered in the associated register"  
    ///   - NN: "PERS" / "Andre enkeltpersonar som registrerast i tilknytta register"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> PERS { get; } = new ConstantDefinition<EntityVariant>("ed5d05b6-588c-40fa-8885-2bd36f75ac34")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "PERS", Description = "Andre enkeltpersoner som registreres i tilknyttet register" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "PERS"), KeyValuePair.Create("Description", "Other individuals registered in the associated register")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "PERS"), KeyValuePair.Create("Description", "Andre enkeltpersonar som registrerast i tilknytta register"))
    };

    /// <summary>
    /// Represents the entity variant for European Economic Interest Grouping ("EOFG").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> e0444411-a021-4774-854c-2ed876ffd64e  
    /// - <c>Name:</c> "EOFG"  
    /// - <c>Description:</c> "Europeisk økonomisk foretaksgruppe"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "EOFG" / "European Economic Interest Grouping"  
    ///   - NN: "EOFG" / "Europeisk økonomisk foretaksgruppe"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> EOFG { get; } = new ConstantDefinition<EntityVariant>("e0444411-a021-4774-854c-2ed876ffd64e")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "EOFG", Description = "Europeisk økonomisk foretaksgruppe" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "EOFG"), KeyValuePair.Create("Description", "European Economic Interest Grouping")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "EOFG"), KeyValuePair.Create("Description", "Europeisk økonomisk foretaksgruppe"))
    };

    /// <summary>
    /// Represents the entity variant for European company ("SE").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 441a2876-f15f-4007-9e2e-3d25acbd98ff  
    /// - <c>Name:</c> "SE"  
    /// - <c>Description:</c> "Europeisk selskap"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "SE" / "European company"  
    ///   - NN: "SE" / "Europeisk selskap"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> SE { get; } = new ConstantDefinition<EntityVariant>("441a2876-f15f-4007-9e2e-3d25acbd98ff")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "SE", Description = "Europeisk selskap" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "SE"), KeyValuePair.Create("Description", "European company")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "SE"), KeyValuePair.Create("Description", "Europeisk selskap"))
    };

    /// <summary>
    /// Represents the entity variant for compulsory VAT registration ("TVAM").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 3d5a890a-51aa-4e8a-b53d-3ec4111fe9e9  
    /// - <c>Name:</c> "TVAM"  
    /// - <c>Description:</c> "Tvangsregistrert for MVA"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "TVAM" / "Compulsory VAT registration"  
    ///   - NN: "TVAM" / "Tvangsregistrert for MVA"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> TVAM { get; } = new ConstantDefinition<EntityVariant>("3d5a890a-51aa-4e8a-b53d-3ec4111fe9e9")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "TVAM", Description = "Tvangsregistrert for MVA" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "TVAM"), KeyValuePair.Create("Description", "Compulsory VAT registration")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "TVAM"), KeyValuePair.Create("Description", "Tvangsregistrert for MVA"))
    };

    /// <summary>
    /// Represents the entity variant for mutual insurance company ("GFS").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> e5d4a90e-948a-4c61-965a-43dbbd0efddb  
    /// - <c>Name:</c> "GFS"  
    /// - <c>Description:</c> "Gjensidig forsikringsselskap"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "GFS" / "Mutual insurance company"  
    ///   - NN: "GFS" / "Gjensidig forsikringsselskap"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> GFS { get; } = new ConstantDefinition<EntityVariant>("e5d4a90e-948a-4c61-965a-43dbbd0efddb")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "GFS", Description = "Gjensidig forsikringsselskap" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "GFS"), KeyValuePair.Create("Description", "Mutual insurance company")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "GFS"), KeyValuePair.Create("Description", "Gjensidig forsikringsselskap"))
    };

    /// <summary>
    /// Represents the entity variant for county municipality ("FYLK").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> a581992e-c9dd-4250-8a9e-4e91d9b55424  
    /// - <c>Name:</c> "FYLK"  
    /// - <c>Description:</c> "Fylkeskommune"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "FYLK" / "County municipality"  
    ///   - NN: "FYLK" / "Fylkeskommune"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> FYLK { get; } = new ConstantDefinition<EntityVariant>("a581992e-c9dd-4250-8a9e-4e91d9b55424")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "FYLK", Description = "Fylkeskommune" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "FYLK"), KeyValuePair.Create("Description", "County municipality")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "FYLK"), KeyValuePair.Create("Description", "Fylkeskommune"))
    };

    /// <summary>
    /// Represents the entity variant for other non-legal persons ("IKJP").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> ab5013e9-4210-4ab3-9fc2-554fd78a1b03  
    /// - <c>Name:</c> "IKJP"  
    /// - <c>Description:</c> "Andre ikke-juridiske personer"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "IKJP" / "Other non-legal persons"  
    ///   - NN: "IKJP" / "Andre ikkje-juridiske personar"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> IKJP { get; } = new ConstantDefinition<EntityVariant>("ab5013e9-4210-4ab3-9fc2-554fd78a1b03")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "IKJP", Description = "Andre ikke-juridiske personer" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "IKJP"), KeyValuePair.Create("Description", "Other non-legal persons")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "IKJP"), KeyValuePair.Create("Description", "Andre ikkje-juridiske personar"))
    };

    /// <summary>
    /// Represents the entity variant for Norwegian-registered foreign company ("NUF").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> a90417b4-5fa9-4a01-bfd0-57a1069a000c  
    /// - <c>Name:</c> "NUF"  
    /// - <c>Description:</c> "Norskregistrert utenlandsk foretak"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "NUF" / "Norwegian-registered foreign company"  
    ///   - NN: "NUF" / "Norskregistrert utanlandsk foretak"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> NUF { get; } = new ConstantDefinition<EntityVariant>("a90417b4-5fa9-4a01-bfd0-57a1069a000c")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "NUF", Description = "Norskregistrert utenlandsk foretak" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "NUF"), KeyValuePair.Create("Description", "Norwegian-registered foreign company")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "NUF"), KeyValuePair.Create("Description", "Norskregistrert utanlandsk foretak"))
    };

    /// <summary>
    /// Represents the entity variant for partnership with joint liability ("ANS").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> fca69e4d-453c-4404-b057-5e188c603f4b  
    /// - <c>Name:</c> "ANS"  
    /// - <c>Description:</c> "Ansvarlig selskap med solidarisk ansvar"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "ANS" / "Partnership with joint liability"  
    ///   - NN: "ANS" / "Ansvarleg selskap med solidarisk ansvar"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> ANS { get; } = new ConstantDefinition<EntityVariant>("fca69e4d-453c-4404-b057-5e188c603f4b")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "ANS", Description = "Ansvarlig selskap med solidarisk ansvar" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "ANS"), KeyValuePair.Create("Description", "Partnership with joint liability")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "ANS"), KeyValuePair.Create("Description", "Ansvarleg selskap med solidarisk ansvar"))
    };

    /// <summary>
    /// Represents the entity variant for limited partnership ("KS").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 64b05309-7b6e-40c8-bd29-62d8fa0bd5ec  
    /// - <c>Name:</c> "KS"  
    /// - <c>Description:</c> "Kommandittselskap"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "KS" / "Limited partnership"  
    ///   - NN: "KS" / "Kommandittselskap"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> KS { get; } = new ConstantDefinition<EntityVariant>("64b05309-7b6e-40c8-bd29-62d8fa0bd5ec")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "KS", Description = "Kommandittselskap" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "KS"), KeyValuePair.Create("Description", "Limited partnership")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "KS"), KeyValuePair.Create("Description", "Kommandittselskap"))
    };

    /// <summary>
    /// Represents the entity variant for other company according to special law ("SÆR").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 90b3eb3b-87cb-4ec3-bc44-65630cd02a67  
    /// - <c>Name:</c> "SÆR"  
    /// - <c>Description:</c> "Annet foretak iflg. særskilt lov"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "SÆR" / "Other company according to special law"  
    ///   - NN: "SÆR" / "Annet foretak i følgje særskild lov"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> SAER { get; } = new ConstantDefinition<EntityVariant>("90b3eb3b-87cb-4ec3-bc44-65630cd02a67")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "SÆR", Description = "Annet foretak iflg. særskilt lov" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "SÆR"), KeyValuePair.Create("Description", "Other company according to special law")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "SÆR"), KeyValuePair.Create("Description", "Annet foretak i følgje særskild lov"))
    };

    /// <summary>
    /// Represents the entity variant for inter-municipal company ("IKS").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> d0648c3e-1567-48dc-a7cf-6837653dbc12  
    /// - <c>Name:</c> "IKS"  
    /// - <c>Description:</c> "Interkommunalt selskap"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "IKS" / "Inter-municipal company"  
    ///   - NN: "IKS" / "Interkommunalt selskap"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> IKS { get; } = new ConstantDefinition<EntityVariant>("d0648c3e-1567-48dc-a7cf-6837653dbc12")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "IKS", Description = "Interkommunalt selskap" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "IKS"), KeyValuePair.Create("Description", "Inter-municipal company")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "IKS"), KeyValuePair.Create("Description", "Interkommunalt selskap"))
    };

    /// <summary>
    /// Represents the entity variant for foundation ("STI").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 9d80264a-b968-45f8-b740-6a283cbc06ad  
    /// - <c>Name:</c> "STI"  
    /// - <c>Description:</c> "Stiftelse"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "STI" / "Foundation"  
    ///   - NN: "STI" / "Stiftelse"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> STI { get; } = new ConstantDefinition<EntityVariant>("9d80264a-b968-45f8-b740-6a283cbc06ad")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "STI", Description = "Stiftelse" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "STI"), KeyValuePair.Create("Description", "Foundation")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "STI"), KeyValuePair.Create("Description", "Stiftelse"))
    };

    /// <summary>
    /// Represents the entity variant for housing cooperative ("BBL").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 8aa09ac2-dd61-492f-9613-6fc0558ab6fb  
    /// - <c>Name:</c> "BBL"  
    /// - <c>Description:</c> "Boligbyggelag"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "BBL" / "Housing cooperative"  
    ///   - NN: "BBL" / "Boligbyggelag"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> BBL { get; } = new ConstantDefinition<EntityVariant>("8aa09ac2-dd61-492f-9613-6fc0558ab6fb")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "BBL", Description = "Boligbyggelag" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "BBL"), KeyValuePair.Create("Description", "Housing cooperative")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "BBL"), KeyValuePair.Create("Description", "Boligbyggelag"))
    };

    /// <summary>
    /// Represents the entity variant for shared office ("KTRF").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> e9b021a9-b257-42c0-8460-717a95c883f6  
    /// - <c>Name:</c> "KTRF"  
    /// - <c>Description:</c> "Kontorfellesskap"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "KTRF" / "Shared office"  
    ///   - NN: "KTRF" / "Kontorfellesskap"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> KTRF { get; } = new ConstantDefinition<EntityVariant>("e9b021a9-b257-42c0-8460-717a95c883f6")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "KTRF", Description = "Kontorfellesskap" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "KTRF"), KeyValuePair.Create("Description", "Shared office")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "KTRF"), KeyValuePair.Create("Description", "Kontorfellesskap"))
    };

    /// <summary>
    /// Represents the entity variant for other legal entity ("ANNA").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 2587990f-b036-4a6b-a7d9-815853be1382  
    /// - <c>Name:</c> "ANNA"  
    /// - <c>Description:</c> "Annen juridisk person"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "ANNA" / "Other legal entity"  
    ///   - NN: "ANNA" / "Annan juridisk person"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> ANNA { get; } = new ConstantDefinition<EntityVariant>("2587990f-b036-4a6b-a7d9-815853be1382")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "ANNA", Description = "Annen juridisk person" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "ANNA"), KeyValuePair.Create("Description", "Other legal entity")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "ANNA"), KeyValuePair.Create("Description", "Annan juridisk person"))
    };

    /// <summary>
    /// Represents the entity variant for cooperative enterprise ("SA").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 7d356f18-2f72-49b5-a6f2-83d7c0871991  
    /// - <c>Name:</c> "SA"  
    /// - <c>Description:</c> "Samvirkeforetak"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "SA" / "Cooperative enterprise"  
    ///   - NN: "SA" / "Samvirkeforetak"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> SA { get; } = new ConstantDefinition<EntityVariant>("7d356f18-2f72-49b5-a6f2-83d7c0871991")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "SA", Description = "Samvirkeforetak" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "SA"), KeyValuePair.Create("Description", "Cooperative enterprise")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "SA"), KeyValuePair.Create("Description", "Samvirkeforetak"))
    };

    /// <summary>
    /// Represents the entity variant for administrative unit - public sector ("ADOS").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> e57cac52-e401-4c0f-a1cf-8bb4628fe671  
    /// - <c>Name:</c> "ADOS"  
    /// - <c>Description:</c> "Administrativ enhet - offentlig sektor"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "ADOS" / "Administrative unit - public sector"  
    ///   - NN: "ADOS" / "Administrativ eining - offentleg sektor"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> ADOS { get; } = new ConstantDefinition<EntityVariant>("e57cac52-e401-4c0f-a1cf-8bb4628fe671")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "ADOS", Description = "Administrativ enhet - offentlig sektor" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "ADOS"), KeyValuePair.Create("Description", "Administrative unit - public sector")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "ADOS"), KeyValuePair.Create("Description", "Administrativ eining - offentleg sektor"))
    };

    /// <summary>
    /// Represents the entity variant for municipal enterprise ("KF").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 3ae468d4-ea92-471d-b7b1-924e49b0d619  
    /// - <c>Name:</c> "KF"  
    /// - <c>Description:</c> "Kommunalt foretak"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "KF" / "Municipal enterprise"  
    ///   - NN: "KF" / "Kommunalt foretak"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> KF { get; } = new ConstantDefinition<EntityVariant>("3ae468d4-ea92-471d-b7b1-924e49b0d619")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "KF", Description = "Kommunalt foretak" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "KF"), KeyValuePair.Create("Description", "Municipal enterprise")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "KF"), KeyValuePair.Create("Description", "Kommunalt foretak"))
    };

    /// <summary>
    /// Represents the entity variant for subunit of non-commercial entity ("AAFY").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0c31bb8f-587a-416b-a3cd-980bb73c5612  
    /// - <c>Name:</c> "AAFY"  
    /// - <c>Description:</c> "Underenhet til ikke-næringsdrivende"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "AAFY" / "Subunit of non-commercial entity"  
    ///   - NN: "AAFY" / "Underenhet til ikkje-næringsdrivande"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> AAFY { get; } = new ConstantDefinition<EntityVariant>("0c31bb8f-587a-416b-a3cd-980bb73c5612")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "AAFY", Description = "Underenhet til ikke-næringsdrivende" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "AAFY"), KeyValuePair.Create("Description", "Subunit of non-commercial entity")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "AAFY"), KeyValuePair.Create("Description", "Underenhet til ikkje-næringsdrivande"))
    };

    /// <summary>
    /// Represents the entity variant for partnership with divided liability ("DA").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> b3433097-38b9-4a47-bd50-a4bb794cab3d  
    /// - <c>Name:</c> "DA"  
    /// - <c>Description:</c> "Ansvarlig selskap med delt ansvar"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "DA" / "Partnership with divided liability"  
    ///   - NN: "DA" / "Ansvarleg selskap med delt ansvar"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> DA { get; } = new ConstantDefinition<EntityVariant>("b3433097-38b9-4a47-bd50-a4bb794cab3d")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "DA", Description = "Ansvarlig selskap med delt ansvar" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "DA"), KeyValuePair.Create("Description", "Partnership with divided liability")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "DA"), KeyValuePair.Create("Description", "Ansvarleg selskap med delt ansvar"))
    };

    /// <summary>
    /// Represents the entity variant for specially divided unit, cf. VAT Act § 2-2 ("OPMV").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 4effb14f-8a1f-4272-aefb-b92ee302050f  
    /// - <c>Name:</c> "OPMV"  
    /// - <c>Description:</c> "Særskilt oppdelt enhet, jf. mval. § 2-2"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "OPMV" / "Specially divided unit, cf. VAT Act § 2-2"  
    ///   - NN: "OPMV" / "Særskild oppdelt eining, jf. mval. § 2-2"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> OPMV { get; } = new ConstantDefinition<EntityVariant>("4effb14f-8a1f-4272-aefb-b92ee302050f")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "OPMV", Description = "Særskilt oppdelt enhet, jf. mval. § 2-2" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "OPMV"), KeyValuePair.Create("Description", "Specially divided unit, cf. VAT Act § 2-2")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "OPMV"), KeyValuePair.Create("Description", "Særskild oppdelt eining, jf. mval. § 2-2"))
    };

    /// <summary>
    /// Represents the entity variant for organizational unit ("ORGL").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 4f6c04d2-7223-41cc-8135-bb91d79ed311  
    /// - <c>Name:</c> "ORGL"  
    /// - <c>Description:</c> "Organisasjonsledd"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "ORGL" / "Organizational unit"  
    ///   - NN: "ORGL" / "Organisasjonsledd"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> ORGL { get; } = new ConstantDefinition<EntityVariant>("4f6c04d2-7223-41cc-8135-bb91d79ed311")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "ORGL", Description = "Organisasjonsledd" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "ORGL"), KeyValuePair.Create("Description", "Organizational unit")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "ORGL"), KeyValuePair.Create("Description", "Organisasjonsledd"))
    };

    /// <summary>
    /// Represents the entity variant for the state ("STAT").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 28157281-cc8f-46e0-9e2a-c20cb3b72930  
    /// - <c>Name:</c> "STAT"  
    /// - <c>Description:</c> "Staten"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "STAT" / "The State"  
    ///   - NN: "STAT" / "Staten"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> STAT { get; } = new ConstantDefinition<EntityVariant>("28157281-cc8f-46e0-9e2a-c20cb3b72930")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "STAT", Description = "Staten" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "STAT"), KeyValuePair.Create("Description", "The State")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "STAT"), KeyValuePair.Create("Description", "Staten"))
    };

    /// <summary>
    /// Represents the entity variant for state enterprise ("SF").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> d5b0abc8-22e7-44bb-bb55-c33bd6d7df4d  
    /// - <c>Name:</c> "SF"  
    /// - <c>Description:</c> "Statsforetak"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "SF" / "State enterprise"  
    ///   - NN: "SF" / "Statsforetak"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> SF { get; } = new ConstantDefinition<EntityVariant>("d5b0abc8-22e7-44bb-bb55-c33bd6d7df4d")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "SF", Description = "Statsforetak" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "SF"), KeyValuePair.Create("Description", "State enterprise")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "SF"), KeyValuePair.Create("Description", "Statsforetak"))
    };

    /// <summary>
    /// Represents the entity variant for partnership for ship ownership ("PRE").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 3aded080-d0d4-4893-8d30-c45dff4d7656  
    /// - <c>Name:</c> "PRE"  
    /// - <c>Description:</c> "Partrederi"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "PRE" / "Partnership for ship ownership"  
    ///   - NN: "PRE" / "Partrederi"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> PRE { get; } = new ConstantDefinition<EntityVariant>("3aded080-d0d4-4893-8d30-c45dff4d7656")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "PRE", Description = "Partrederi" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "PRE"), KeyValuePair.Create("Description", "Partnership for ship ownership")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "PRE"), KeyValuePair.Create("Description", "Partrederi"))
    };

    /// <summary>
    /// Represents the entity variant for housing association ("BRL").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 7c0ae1b2-2fa9-4266-8911-c4cb82c1489b  
    /// - <c>Name:</c> "BRL"  
    /// - <c>Description:</c> "Borettslag"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "BRL" / "Housing association"  
    ///   - NN: "BRL" / "Borettslag"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> BRL { get; } = new ConstantDefinition<EntityVariant>("7c0ae1b2-2fa9-4266-8911-c4cb82c1489b")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "BRL", Description = "Borettslag" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "BRL"), KeyValuePair.Create("Description", "Housing association")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "BRL"), KeyValuePair.Create("Description", "Borettslag"))
    };

    /// <summary>
    /// Represents the entity variant for municipality ("KOMM").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> ed82281c-a5a1-4a28-9046-c70d95ce4658  
    /// - <c>Name:</c> "KOMM"  
    /// - <c>Description:</c> "Kommune"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "KOMM" / "Municipality"  
    ///   - NN: "KOMM" / "Kommune"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> KOMM { get; } = new ConstantDefinition<EntityVariant>("ed82281c-a5a1-4a28-9046-c70d95ce4658")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "KOMM", Description = "Kommune" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "KOMM"), KeyValuePair.Create("Description", "Municipality")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "KOMM"), KeyValuePair.Create("Description", "Kommune"))
    };

    /// <summary>
    /// Represents the entity variant for association/club/institution ("FLI").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> ecd6e878-9121-43e6-aec0-c74b562cd3da  
    /// - <c>Name:</c> "FLI"  
    /// - <c>Description:</c> "Forening/lag/innretning"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "FLI" / "Association/club/institution"  
    ///   - NN: "FLI" / "Forening/lag/innretting"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> FLI { get; } = new ConstantDefinition<EntityVariant>("ecd6e878-9121-43e6-aec0-c74b562cd3da")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "FLI", Description = "Forening/lag/innretning" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "FLI"), KeyValuePair.Create("Description", "Association/club/institution")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "FLI"), KeyValuePair.Create("Description", "Forening/lag/innretting"))
    };

    /// <summary>
    /// Represents the entity variant for savings bank ("SPA").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 80acaf52-3bf5-48c7-ab79-cb6f141a5b6f  
    /// - <c>Name:</c> "SPA"  
    /// - <c>Description:</c> "Sparebank"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "SPA" / "Savings bank"  
    ///   - NN: "SPA" / "Sparebank"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> SPA { get; } = new ConstantDefinition<EntityVariant>("80acaf52-3bf5-48c7-ab79-cb6f141a5b6f")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "SPA", Description = "Sparebank" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "SPA"), KeyValuePair.Create("Description", "Savings bank")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "SPA"), KeyValuePair.Create("Description", "Sparebank"))
    };

    /// <summary>
    /// Represents the entity variant for public limited company ("ASA").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> ca45ed3a-41b3-4d2b-add9-db0d41a4e42b  
    /// - <c>Name:</c> "ASA"  
    /// - <c>Description:</c> "Allmennaksjeselskap"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "ASA" / "Public limited company"  
    ///   - NN: "ASA" / "Allmennaksjeselskap"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> ASA { get; } = new ConstantDefinition<EntityVariant>("ca45ed3a-41b3-4d2b-add9-db0d41a4e42b")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "ASA", Description = "Allmennaksjeselskap" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "ASA"), KeyValuePair.Create("Description", "Public limited company")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "ASA"), KeyValuePair.Create("Description", "Allmennaksjeselskap"))
    };

    /// <summary>
    /// Represents the entity variant for condominium ("ESEK").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 1e2e44c0-e5e6-4962-8beb-e0ce16760a04  
    /// - <c>Name:</c> "ESEK"  
    /// - <c>Description:</c> "Eierseksjonssameie"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "ESEK" / "Condominium"  
    ///   - NN: "ESEK" / "Eigarseksjonssameie"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> ESEK { get; } = new ConstantDefinition<EntityVariant>("1e2e44c0-e5e6-4962-8beb-e0ce16760a04")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "ESEK", Description = "Eierseksjonssameie" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "ESEK"), KeyValuePair.Create("Description", "Condominium")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "ESEK"), KeyValuePair.Create("Description", "Eigarseksjonssameie"))
    };

    /// <summary>
    /// Represents the entity variant for sole proprietorship ("ENK").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> d78400f0-27d9-488a-886c-e264cc5c77ba  
    /// - <c>Name:</c> "ENK"  
    /// - <c>Description:</c> "Enkeltpersonforetak"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "ENK" / "Sole proprietorship"  
    ///   - NN: "ENK" / "Enkeltpersonforetak"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> ENK { get; } = new ConstantDefinition<EntityVariant>("d78400f0-27d9-488a-886c-e264cc5c77ba")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "ENK", Description = "Enkeltpersonforetak" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "ENK"), KeyValuePair.Create("Description", "Sole proprietorship")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "ENK"), KeyValuePair.Create("Description", "Enkeltpersonforetak"))
    };

    /// <summary>
    /// Represents the entity variant for county municipal enterprise ("FKF").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 6b798668-a98d-49f2-a6d9-e391bad99fb2  
    /// - <c>Name:</c> "FKF"  
    /// - <c>Description:</c> "Fylkeskommunalt foretak"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "FKF" / "County municipal enterprise"  
    ///   - NN: "FKF" / "Fylkeskommunalt foretak"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> FKF { get; } = new ConstantDefinition<EntityVariant>("6b798668-a98d-49f2-a6d9-e391bad99fb2")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "FKF", Description = "Fylkeskommunalt foretak" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "FKF"), KeyValuePair.Create("Description", "County municipal enterprise")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "FKF"), KeyValuePair.Create("Description", "Fylkeskommunalt foretak"))
    };

    /// <summary>
    /// Represents the entity variant for The Church of Norway ("KIRK").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 7b43c6c2-e8ce-4f63-bb46-e4eb830fa222  
    /// - <c>Name:</c> "KIRK"  
    /// - <c>Description:</c> "Den norske kirke"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "KIRK" / "The Church of Norway"  
    ///   - NN: "KIRK" / "Den norske kyrkja"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> KIRK { get; } = new ConstantDefinition<EntityVariant>("7b43c6c2-e8ce-4f63-bb46-e4eb830fa222")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "KIRK", Description = "Den norske kirke" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "KIRK"), KeyValuePair.Create("Description", "The Church of Norway")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "KIRK"), KeyValuePair.Create("Description", "Den norske kyrkja"))
    };

    /// <summary>
    /// Represents the entity variant for subunit of commercial and public administration ("BEDR").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 1f1e3720-b8a8-490e-8304-e81da21e3d3b  
    /// - <c>Name:</c> "BEDR"  
    /// - <c>Description:</c> "Underenhet til næringsdrivende og offentlig forvaltning"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "BEDR" / "Subunit of commercial and public administration"  
    ///   - NN: "BEDR" / "Underenhet til næringsdrivande og offentleg forvaltning"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> BEDR { get; } = new ConstantDefinition<EntityVariant>("1f1e3720-b8a8-490e-8304-e81da21e3d3b")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "BEDR", Description = "Underenhet til næringsdrivende og offentlig forvaltning" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "BEDR"), KeyValuePair.Create("Description", "Subunit of commercial and public administration")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "BEDR"), KeyValuePair.Create("Description", "Underenhet til næringsdrivande og offentleg forvaltning"))
    };

    /// <summary>
    /// Represents the entity variant for bankruptcy estate ("KBO").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> d7208d54-067d-4b5c-a906-f0da3d3de0f1  
    /// - <c>Name:</c> "KBO"  
    /// - <c>Description:</c> "Konkursbo"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "KBO" / "Bankruptcy estate"  
    ///   - NN: "KBO" / "Konkursbo"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> KBO { get; } = new ConstantDefinition<EntityVariant>("d7208d54-067d-4b5c-a906-f0da3d3de0f1")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "KBO", Description = "Konkursbo" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "KBO"), KeyValuePair.Create("Description", "Bankruptcy estate")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "KBO"), KeyValuePair.Create("Description", "Konkursbo"))
    };

    /// <summary>
    /// Represents the entity variant for limited liability company ("BA").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> ea460099-515f-4e54-88d8-fbe53a807276  
    /// - <c>Name:</c> "BA"  
    /// - <c>Description:</c> "Selskap med begrenset ansvar"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Organisation"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "BA" / "Limited liability company"  
    ///   - NN: "BA" / "Selskap med avgrensa ansvar"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> BA { get; } = new ConstantDefinition<EntityVariant>("ea460099-515f-4e54-88d8-fbe53a807276")
    {
        Entity = new() { TypeId = EntityTypeConstants.Organisation, Name = "BA", Description = "Selskap med begrenset ansvar" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "BA"), KeyValuePair.Create("Description", "Limited liability company")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "BA"), KeyValuePair.Create("Description", "Selskap med avgrensa ansvar"))
    };

    /// <summary>
    /// Represents the entity variant for person ("Person").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> b0690e14-7a75-45a4-8c02-437f6705b5ee  
    /// - <c>Name:</c> "Person"  
    /// - <c>Description:</c> "Person"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Person"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "Person" / "Person"  
    ///   - NN: "PERS" / "Person"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> Person { get; } = new ConstantDefinition<EntityVariant>("b0690e14-7a75-45a4-8c02-437f6705b5ee")
    {
        Entity = new() { TypeId = EntityTypeConstants.Person, Name = "Person", Description = "Person" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Person"), KeyValuePair.Create("Description", "Person")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "PERS"), KeyValuePair.Create("Description", "Person"))
    };

    /// <summary>
    /// Represents the entity variant for agent system ("AgentSystem").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 8ca2ffdb-b4a9-4c64-8a9a-ed0f8dd722a3  
    /// - <c>Name:</c> "AgentSystem"  
    /// - <c>Description:</c> "AgentSystem"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.SystemUser"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "AgentSystem" / "AgentSystem"  
    ///   - NN: "AgentSystem" / "AgentSystem"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> AgentSystem { get; } = new ConstantDefinition<EntityVariant>("8ca2ffdb-b4a9-4c64-8a9a-ed0f8dd722a3")
    {
        Entity = new() { TypeId = EntityTypeConstants.SystemUser, Name = "AgentSystem", Description = "AgentSystem" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "AgentSystem"), KeyValuePair.Create("Description", "AgentSystem")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "AgentSystem"), KeyValuePair.Create("Description", "AgentSystem"))
    };

    /// <summary>
    /// Represents the entity variant for standard system ("StandardSystem").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> f948baa3-8f6b-4790-a35c-85064c1b7f9b  
    /// - <c>Name:</c> "StandardSystem"  
    /// - <c>Description:</c> "StandardSystem"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.SystemUser"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "StandardSystem" / "StandardSystem"  
    ///   - NN: "StandardSystem" / "StandardSystem"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> StandardSystem { get; } = new ConstantDefinition<EntityVariant>("f948baa3-8f6b-4790-a35c-85064c1b7f9b")
    {
        Entity = new() { TypeId = EntityTypeConstants.SystemUser, Name = "StandardSystem", Description = "StandardSystem" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "StandardSystem"), KeyValuePair.Create("Description", "StandardSystem")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "StandardSystem"), KeyValuePair.Create("Description", "StandardSystem"))
    };

    /// <summary>
    /// Represents the entity variant for self-identified user ("SI").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 03d08113-40d0-48bd-85b6-bd4430ccc182  
    /// - <c>Name:</c> "SI"  
    /// - <c>Description:</c> "Selvidentifisert bruker"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Person"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "SI" / "Self-identified user"  
    ///   - NN: "SI" / "Sjølvidentifisert brukar"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> SI { get; } = new ConstantDefinition<EntityVariant>("03d08113-40d0-48bd-85b6-bd4430ccc182")
    {
        Entity = new() { TypeId = EntityTypeConstants.Person, Name = "SI", Description = "Selvidentifisert bruker" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "SI"), KeyValuePair.Create("Description", "Self-identified user")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "SI"), KeyValuePair.Create("Description", "Sjølvidentifisert brukar"))
    };

    /// <summary>
    /// Represents the entity variant for default internal entity ("Standard").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> cbe2834d-3db0-4a14-baa2-d32de004d6d7  
    /// - <c>Name:</c> "Standard"  
    /// - <c>Description:</c> "Standard intern entitet"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Internal"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "Default" / "Default internal entity"  
    ///   - NN: "Standard" / "Standard intern entitet"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> Standard { get; } = new ConstantDefinition<EntityVariant>("cbe2834d-3db0-4a14-baa2-d32de004d6d7")
    {
        Entity = new() { TypeId = EntityTypeConstants.Internal, Name = "Standard", Description = "Standard intern entitet" },
        EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Default"), KeyValuePair.Create("Description", "Default internal entity")),
        NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Standard"), KeyValuePair.Create("Description", "Standard intern entitet")),
    };

    /// <summary>
    /// Represents the entity variant for enterprise user ("Virksomhetsbruker").
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> fa794486-108d-4285-afd1-698763a602dd  
    /// - <c>Name:</c> "Virksomhetsbruker"  
    /// - <c>Description:</c> "Virksomhetsbruker"  
    /// - <c>TypeId:</c> References <see cref="EntityTypeConstants.Internal"/>  
    /// - <c>Translations:</c>  
    ///   - EN: "Enterprise User" / "Enterprise User"  
    ///   - NN: "Verksemdsbrukar" / "Verksemdsbrukar"  
    /// </remarks>
    public static ConstantDefinition<EntityVariant> EnterpriseUser { get; } =
        new ConstantDefinition<EntityVariant>("fa794486-108d-4285-afd1-698763a602dd")
        {
            Entity = new() { TypeId = EntityTypeConstants.Internal, Name = "Virksomhetsbruker", Description = "Virksomhetsbruker" },
            EN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Enterprise User"), KeyValuePair.Create("Description", "Enterprise User")),
            NN = TranslationEntryList.Create(KeyValuePair.Create("Name", "Verksemdsbrukar"), KeyValuePair.Create("Description", "Verksemdsbrukar")),
        };
}
