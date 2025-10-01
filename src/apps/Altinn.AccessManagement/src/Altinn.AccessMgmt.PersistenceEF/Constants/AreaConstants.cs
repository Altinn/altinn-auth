using System.Diagnostics.CodeAnalysis;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;

namespace Altinn.AccessMgmt.PersistenceEF.Constants;

/// <summary>
/// Defines constant <see cref="Area"/> instances used in the system.
/// Each constant represents a specific area for organizing access packages
/// with a fixed unique identifier (GUID), name, description, and associated area group.
/// </summary>
public static class AreaConstants
{
    /// <summary>
    /// Try to get <see cref="Area"/> by name.
    /// </summary>
    public static bool TryGetByName(string name, [NotNullWhen(true)] out ConstantDefinition<Area>? result)
        => ConstantLookup.TryGetByName(typeof(AreaConstants), name, out result);

    /// <summary>
    /// Try to get <see cref="Area"/> using Guid.
    /// </summary>
    public static bool TryGetById(Guid id, [NotNullWhen(true)] out ConstantDefinition<Area>? result)
        => ConstantLookup.TryGetById(typeof(AreaConstants), id, out result);

    /// <summary>
    /// Get all constants as a read-only collection.
    /// </summary>
    public static IReadOnlyCollection<ConstantDefinition<Area>> AllEntities()
        => ConstantLookup.AllEntities<Area>(typeof(AreaConstants));

    /// <summary>
    /// Get all translations as read-only collection.
    /// </summary>
    public static IReadOnlyCollection<TranslationEntry> AllTranslations()
        => ConstantLookup.AllTranslations<Area>(typeof(AreaConstants));

    private static Uri AltinnCDNPackageIcons { get; } = new Uri("https://altinncdn.no/authorization/accesspackageicons/");

    /// <summary>
    /// Represents the Tax, Fees, Accounting and Customs area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 7d32591d-34b7-4afc-8afa-013722f8c05d
    /// - <c>Name:</c> "Skatt, avgift, regnskap og toll"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til skatt, avgift, regnskap og toll."
    /// - <c>Urn:</c> "accesspackage:area:skatt_avgift_regnskap_og_toll"
    /// - <c>IconUrl:</c> "Aksel_Money_SackKroner.svg"
    /// - <c>GroupId:</c> General (Allment)
    /// </remarks>
    public static ConstantDefinition<Area> TaxFeesAccountingAndCustoms { get; } = new ConstantDefinition<Area>("7d32591d-34b7-4afc-8afa-013722f8c05d")
    {
        Entity = new()
        {
            Name = "Skatt, avgift, regnskap og toll",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til skatt, avgift, regnskap og toll.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Money_SackKroner.svg").ToString(),
            GroupId = AreaGroupConstants.General,
            Urn = "accesspackage:area:skatt_avgift_regnskap_og_toll"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Taxes, Fees, Accounting and Customs"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to taxes, fees, accounting, and customs.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Skatt, avgift, rekneskap og toll"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til skatt, avgift, rekneskap og toll.")),
    };

    /// <summary>
    /// Represents the Personnel area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 6f7f3b02-8b5a-4823-9468-0f4646d3a790
    /// - <c>Name:</c> "Personale"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til personale."
    /// - <c>Urn:</c> "accesspackage:area:personale"
    /// - <c>IconUrl:</c> "Aksel_People_PersonGroup.svg"
    /// - <c>GroupId:</c> General (Allment)
    /// </remarks>
    public static ConstantDefinition<Area> Personnel { get; } = new ConstantDefinition<Area>("6f7f3b02-8b5a-4823-9468-0f4646d3a790")
    {
        Entity = new()
        {
            Name = "Personale",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til personale.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_People_PersonGroup.svg").ToString(),
            GroupId = AreaGroupConstants.General,
            Urn = "accesspackage:area:personale"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Personnel"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to personnel.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Personale"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til personalet.")),
    };

    /// <summary>
    /// Represents the Environment, Accident and Safety area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> a8834a7c-ed89-4c73-b5d5-19a2347f3b13
    /// - <c>Name:</c> "Miljø, ulykke og sikkerhet"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til miljø, ulykke og sikkerhet."
    /// - <c>Urn:</c> "accesspackage:area:miljo_ulykke_og_sikkerhet"
    /// - <c>IconUrl:</c> "Aksel_People_HandHeart.svg"
    /// - <c>GroupId:</c> General (Allment)
    /// </remarks>
    public static ConstantDefinition<Area> EnvironmentAccidentAndSafety { get; } = new ConstantDefinition<Area>("a8834a7c-ed89-4c73-b5d5-19a2347f3b13")
    {
        Entity = new()
        {
            Name = "Miljø, ulykke og sikkerhet",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til miljø, ulykke og sikkerhet.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_People_HandHeart.svg").ToString(),
            GroupId = AreaGroupConstants.General,
            Urn = "accesspackage:area:miljo_ulykke_og_sikkerhet"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Environment, Accident and Safety"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to environment, accident, and safety.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Miljø, ulykke og tryggleik"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til miljø, ulykke og tryggleik.")),
    };

    /// <summary>
    /// Represents the Mail and Archive area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 6f938de8-34f2-4bab-a0c6-3a3eb64aad3b
    /// - <c>Name:</c> "Post og arkiv"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til post og arkiv."
    /// - <c>Urn:</c> "accesspackage:area:post_og_arkiv"
    /// - <c>IconUrl:</c> "Aksel_Interface_EnvelopeClosed.svg"
    /// - <c>GroupId:</c> General (Allment)
    /// </remarks>
    public static ConstantDefinition<Area> MailAndArchive { get; } = new ConstantDefinition<Area>("6f938de8-34f2-4bab-a0c6-3a3eb64aad3b")
    {
        Entity = new()
        {
            Name = "Post og arkiv",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til post og arkiv.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Interface_EnvelopeClosed.svg").ToString(),
            GroupId = AreaGroupConstants.General,
            Urn = "accesspackage:area:post_og_arkiv"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Mail and Archive"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to mail and archive.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Post og arkiv"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til post og arkiv.")),
    };

    /// <summary>
    /// Represents the Business Affairs area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 3f5df819-7aca-49e1-bf6f-3e8f120f20d1
    /// - <c>Name:</c> "Forhold ved virksomheten"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til forhold ved virksomheten."
    /// - <c>Urn:</c> "accesspackage:area:forhold_ved_virksomheten"
    /// - <c>IconUrl:</c> "Aksel_Workplace_Buildings3.svg"
    /// - <c>GroupId:</c> General (Allment)
    /// </remarks>
    public static ConstantDefinition<Area> BusinessAffairs { get; } = new ConstantDefinition<Area>("3f5df819-7aca-49e1-bf6f-3e8f120f20d1")
    {
        Entity = new()
        {
            Name = "Forhold ved virksomheten",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til forhold ved virksomheten.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Workplace_Buildings3.svg").ToString(),
            GroupId = AreaGroupConstants.General,
            Urn = "accesspackage:area:forhold_ved_virksomheten"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Business Affairs"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to business affairs.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Forhold ved verksemda"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til forhold ved verksemda.")),
    };

    /// <summary>
    /// Represents the Integrations area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 892e98c6-1696-46e7-9bb1-59c08761ec64
    /// - <c>Name:</c> "Integrasjoner"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til integrasjoner."
    /// - <c>Urn:</c> "accesspackage:area:integrasjoner"
    /// - <c>IconUrl:</c> "Aksel_Interface_RotateLeft.svg"
    /// - <c>GroupId:</c> General (Allment)
    /// </remarks>
    public static ConstantDefinition<Area> Integrations { get; } = new ConstantDefinition<Area>("892e98c6-1696-46e7-9bb1-59c08761ec64")
    {
        Entity = new()
        {
            Name = "Integrasjoner",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til integrasjoner.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Interface_RotateLeft.svg").ToString(),
            GroupId = AreaGroupConstants.General,
            Urn = "accesspackage:area:integrasjoner"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Integrations"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to integrations.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Integrasjonar"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til integrasjonar.")),
    };

    /// <summary>
    /// Represents the Manage Access area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> e4ae823f-41db-46ed-873f-8a5d1378fff8
    /// - <c>Name:</c> "Administrere tilganger"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til administrere tilganger."
    /// - <c>Urn:</c> "accesspackage:area:administrere_tilganger"
    /// - <c>IconUrl:</c> "Altinn_Administrere-tilganger_PersonLock.svg"
    /// - <c>GroupId:</c> General (Allment)
    /// </remarks>
    public static ConstantDefinition<Area> ManageAccess { get; } = new ConstantDefinition<Area>("e4ae823f-41db-46ed-873f-8a5d1378fff8")
    {
        Entity = new()
        {
            Name = "Administrere tilganger",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til administrere tilganger.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Altinn_Administrere-tilganger_PersonLock.svg").ToString(),
            GroupId = AreaGroupConstants.General,
            Urn = "accesspackage:area:administrere_tilganger"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Manage Access"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to managing access.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Administrere tilgongar"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til administrering av tilgongar.")),
    };

    /// <summary>
    /// Represents the Agriculture, Forestry, Hunting, Fishing and Aquaculture area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> fc93d25e-80bc-469a-aa43-a6cee80eb3e2
    /// - <c>Name:</c> "Jordbruk, skogbruk, jakt, fiske og akvakultur"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til jordbruk, skogbruk, jakt, fiske og akvakultur."
    /// - <c>Urn:</c> "accesspackage:area:jordbruk_skogbruk_jakt_fiske_og_akvakultur"
    /// - <c>IconUrl:</c> "Aksel_Nature-and-animals-Plant.svg"
    /// - <c>GroupId:</c> Industry (Bransje)
    /// </remarks>
    public static ConstantDefinition<Area> AgricultureForestryHuntingFishingAndAquaculture { get; } = new ConstantDefinition<Area>("fc93d25e-80bc-469a-aa43-a6cee80eb3e2")
    {
        Entity = new()
        {
            Name = "Jordbruk, skogbruk, jakt, fiske og akvakultur",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til jordbruk, skogbruk, jakt, fiske og akvakultur.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Nature-and-animals-Plant.svg").ToString(),
            GroupId = AreaGroupConstants.Industry,
            Urn = "accesspackage:area:jordbruk_skogbruk_jakt_fiske_og_akvakultur"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Agriculture, Forestry, Hunting, Fishing and Aquaculture"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to agriculture, forestry, hunting, fishing and aquaculture.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Jordbruk, skogbruk, jakt, fiske og akvakultur"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til jordbruk, skogbruk, jakt, fiske og akvakultur.")),
    };

    /// <summary>
    /// Represents the Construction, Infrastructure and Real Estate area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 536b317c-ef85-45d4-9b48-6511578e1952
    /// - <c>Name:</c> "Bygg, anlegg og eiendom"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til bygg, anlegg og eiendom."
    /// - <c>Urn:</c> "accesspackage:area:bygg_anlegg_og_eiendom"
    /// - <c>IconUrl:</c> "Altinn_Bygg-anlegg-og-eiendom_HandHouse.svg"
    /// - <c>GroupId:</c> Industry (Bransje)
    /// </remarks>
    public static ConstantDefinition<Area> ConstructionInfrastructureAndRealEstate { get; } = new ConstantDefinition<Area>("536b317c-ef85-45d4-9b48-6511578e1952")
    {
        Entity = new()
        {
            Name = "Bygg, anlegg og eiendom",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til bygg, anlegg og eiendom.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Altinn_Bygg-anlegg-og-eiendom_HandHouse.svg").ToString(),
            GroupId = AreaGroupConstants.Industry,
            Urn = "accesspackage:area:bygg_anlegg_og_eiendom"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Construction, Infrastructure and Real Estate"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to construction, infrastructure and real estate.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Bygg, anlegg og eigedom"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til bygg, anlegg og eigedom.")),
    };

    /// <summary>
    /// Represents the Transport and Storage area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 6ff90072-566b-4acd-baac-ec477534e712
    /// - <c>Name:</c> "Transport og lagring"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til transport og lagring."
    /// - <c>Urn:</c> "accesspackage:area:transport_og_lagring"
    /// - <c>IconUrl:</c> "Aksel_Transportation_Truck.svg"
    /// - <c>GroupId:</c> Industry (Bransje)
    /// </remarks>
    public static ConstantDefinition<Area> TransportAndStorage { get; } = new ConstantDefinition<Area>("6ff90072-566b-4acd-baac-ec477534e712")
    {
        Entity = new()
        {
            Name = "Transport og lagring",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til transport og lagring.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Transportation_Truck.svg").ToString(),
            GroupId = AreaGroupConstants.Industry,
            Urn = "accesspackage:area:transport_og_lagring"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Transport and Storage"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to transport and storage.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Transport og lagring"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til transport og lagring.")),
    };

    /// <summary>
    /// Represents the Health, Care and Protection area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> eab59b26-833f-40ca-9e27-72107e8f1908
    /// - <c>Name:</c> "Helse, pleie, omsorg og vern"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til helse, pleie, omsorg og vern."
    /// - <c>Urn:</c> "accesspackage:area:helse_pleie_omsorg_og_vern"
    /// - <c>IconUrl:</c> "Aksel_Wellness_Hospital.svg"
    /// - <c>GroupId:</c> Industry (Bransje)
    /// </remarks>
    public static ConstantDefinition<Area> HealthCareAndProtection { get; } = new ConstantDefinition<Area>("eab59b26-833f-40ca-9e27-72107e8f1908")
    {
        Entity = new()
        {
            Name = "Helse, pleie, omsorg og vern",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til helse, pleie, omsorg og vern.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Wellness_Hospital.svg").ToString(),
            GroupId = AreaGroupConstants.Industry,
            Urn = "accesspackage:area:helse_pleie_omsorg_og_vern"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Health, Care and Protection"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to health, care and protection.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Helse, pleie, omsorg og vern"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til helse, pleie, omsorg og vern.")),
    };

    /// <summary>
    /// Represents the Childhood and Education area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 7326614f-cf7c-492e-8e7f-d74e6e4a8970
    /// - <c>Name:</c> "Oppvekst og utdanning"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til oppvekst og utdanning."
    /// - <c>Urn:</c> "accesspackage:area:oppvekst_og_utdanning"
    /// - <c>IconUrl:</c> "Aksel_Workplace_Buildings2.svg"
    /// - <c>GroupId:</c> Industry (Bransje)
    /// </remarks>
    public static ConstantDefinition<Area> ChildhoodAndEducation { get; } = new ConstantDefinition<Area>("7326614f-cf7c-492e-8e7f-d74e6e4a8970")
    {
        Entity = new()
        {
            Name = "Oppvekst og utdanning",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til oppvekst og utdanning.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Workplace_Buildings2.svg").ToString(),
            GroupId = AreaGroupConstants.Industry,
            Urn = "accesspackage:area:oppvekst_og_utdanning"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Childhood and Education"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to childhood and education.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Oppvekst og utdanning"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til oppvekst og utdanning.")),
    };

    /// <summary>
    /// Represents the Energy, Water, Sewage and Waste area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 6e152c10-0f63-4060-9b14-66808e7ac320
    /// - <c>Name:</c> "Energi, vann, avløp og avfall"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til energi, vann, avløp og avfall."
    /// - <c>Urn:</c> "accesspackage:area:energi_vann_avlop_og_avfall"
    /// - <c>IconUrl:</c> "Aksel_Workplace_TapWater.svg"
    /// - <c>GroupId:</c> Industry (Bransje)
    /// </remarks>
    public static ConstantDefinition<Area> EnergyWaterSewageAndWaste { get; } = new ConstantDefinition<Area>("6e152c10-0f63-4060-9b14-66808e7ac320")
    {
        Entity = new()
        {
            Name = "Energi, vann, avløp og avfall",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til energi, vann, avløp og avfall.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Workplace_TapWater.svg").ToString(),
            GroupId = AreaGroupConstants.Industry,
            Urn = "accesspackage:area:energi_vann_avlop_og_avfall"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Energy, Water, Sewage and Waste"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to energy, water, sewage and waste.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Energi, vann, avløp og avfall"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til energi, vann, avløp og avfall.")),
    };

    /// <summary>
    /// Represents the Industries area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 10c2dd29-5ab3-4a26-900e-8e2326150353
    /// - <c>Name:</c> "Industrier"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til industrier."
    /// - <c>Urn:</c> "accesspackage:area:industrier"
    /// - <c>IconUrl:</c> "Altinn_Industrier_Factory.svg"
    /// - <c>GroupId:</c> Industry (Bransje)
    /// </remarks>
    public static ConstantDefinition<Area> Industries { get; } = new ConstantDefinition<Area>("10c2dd29-5ab3-4a26-900e-8e2326150353")
    {
        Entity = new()
        {
            Name = "Industrier",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til industrier.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Altinn_Industrier_Factory.svg").ToString(),
            GroupId = AreaGroupConstants.Industry,
            Urn = "accesspackage:area:industrier"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Industries"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to industries.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Industriar"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til industriar.")),
    };

    /// <summary>
    /// Represents the Culture and Volunteering area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 5996ba37-6db0-4391-8918-b1b0bd4b394b
    /// - <c>Name:</c> "Kultur og frivillighet"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til kultur og frivillighet."
    /// - <c>Urn:</c> "accesspackage:area:kultur_og_frivillighet"
    /// - <c>IconUrl:</c> "Aksel_Wellness_HeadHeart.svg"
    /// - <c>GroupId:</c> Industry (Bransje)
    /// </remarks>
    public static ConstantDefinition<Area> CultureAndVolunteering { get; } = new ConstantDefinition<Area>("5996ba37-6db0-4391-8918-b1b0bd4b394b")
    {
        Entity = new()
        {
            Name = "Kultur og frivillighet",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til kultur og frivillighet.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Wellness_HeadHeart.svg").ToString(),
            GroupId = AreaGroupConstants.Industry,
            Urn = "accesspackage:area:kultur_og_frivillighet"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Culture and Volunteering"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to culture and volunteering.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Kultur og frivillighet"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til kultur og frivillighet.")),
    };

    /// <summary>
    /// Represents the Commerce, Accommodation and Catering area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 3797e9f0-dd83-404c-9897-e356c32ef600
    /// - <c>Name:</c> "Handel, overnatting og servering"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til handel, overnatting og servering."
    /// - <c>Urn:</c> "accesspackage:area:handel_overnatting_og_servering"
    /// - <c>IconUrl:</c> "Aksel_Wellness_TrayFood.svg"
    /// - <c>GroupId:</c> Industry (Bransje)
    /// </remarks>
    public static ConstantDefinition<Area> CommerceAccommodationAndCatering { get; } = new ConstantDefinition<Area>("3797e9f0-dd83-404c-9897-e356c32ef600")
    {
        Entity = new()
        {
            Name = "Handel, overnatting og servering",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til handel, overnatting og servering.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Wellness_TrayFood.svg").ToString(),
            GroupId = AreaGroupConstants.Industry,
            Urn = "accesspackage:area:handel_overnatting_og_servering"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Commerce, Accommodation and Catering"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to commerce, accommodation and catering.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Handel, overnatting og servering"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til handel, overnatting og servering.")),
    };

    /// <summary>
    /// Represents the Other Service Industries area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> e31169f6-d4c7-4e45-93c7-f90bc285b639
    /// - <c>Name:</c> "Andre tjenesteytende næringer"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til andre tjenesteytende næringer."
    /// - <c>Urn:</c> "accesspackage:area:andre_tjenesteytende_naeringer"
    /// - <c>IconUrl:</c> "Aksel_Workplace_Reception.svg"
    /// - <c>GroupId:</c> Industry (Bransje)
    /// </remarks>
    public static ConstantDefinition<Area> OtherServiceIndustries { get; } = new ConstantDefinition<Area>("e31169f6-d4c7-4e45-93c7-f90bc285b639")
    {
        Entity = new()
        {
            Name = "Andre tjenesteytende næringer",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til andre tjenesteytende næringer.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Workplace_Reception.svg").ToString(),
            GroupId = AreaGroupConstants.Industry,
            Urn = "accesspackage:area:andre_tjenesteytende_naeringer"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Other Service Industries"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to other service industries.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Andre tenesteytande næringar"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til andre tenesteytande næringar.")),
    };

    /// <summary>
    /// Represents the Authorizations for Accountants area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 64cbcdc8-01c9-448c-b3d2-eb9582beb3c2
    /// - <c>Name:</c> "Fullmakter for regnskapsfører"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til fullmakter for regnskapsfører."
    /// - <c>Urn:</c> "accesspackage:area:fullmakter_for_regnskapsforer"
    /// - <c>IconUrl:</c> "Aksel_Home_Calculator.svg"
    /// - <c>GroupId:</c> Special (Særskilt)
    /// </remarks>
    public static ConstantDefinition<Area> AuthorizationsForAccountants { get; } = new ConstantDefinition<Area>("64cbcdc8-01c9-448c-b3d2-eb9582beb3c2")
    {
        Entity = new()
        {
            Name = "Fullmakter for regnskapsfører",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til fullmakter for regnskapsfører.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Home_Calculator.svg").ToString(),
            GroupId = AreaGroupConstants.Special,
            Urn = "accesspackage:area:fullmakter_for_regnskapsforer"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Authorizations for Accountants"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to authorizations for accountants.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Fullmakter for rekneskapsførar"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til fullmakter for rekneskapsførar.")),
    };

    /// <summary>
    /// Represents the Authorizations for Auditors area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 7df15290-f43c-4831-a1b4-3edfa43e526d
    /// - <c>Name:</c> "Fullmakter for revisor"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til fullmakter for revisor."
    /// - <c>Urn:</c> "accesspackage:area:fullmakter_for_revisor"
    /// - <c>IconUrl:</c> "Aksel_Files-and-application_FileSearch.svg"
    /// - <c>GroupId:</c> Special (Særskilt)
    /// </remarks>
    public static ConstantDefinition<Area> AuthorizationsForAuditors { get; } = new ConstantDefinition<Area>("7df15290-f43c-4831-a1b4-3edfa43e526d")
    {
        Entity = new()
        {
            Name = "Fullmakter for revisor",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til fullmakter for revisor.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Files-and-application_FileSearch.svg").ToString(),
            GroupId = AreaGroupConstants.Special,
            Urn = "accesspackage:area:fullmakter_for_revisor"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Authorizations for Auditors"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to authorizations for auditors.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Fullmakter for revisor"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til fullmakter for revisor.")),
    };

    /// <summary>
    /// Represents the Authorizations for Bankruptcy Estates area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> f3daddb7-6e21-455e-b6d2-65a281375b6b
    /// - <c>Name:</c> "Fullmakter for konkursbo"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til fullmakter for konkursbo."
    /// - <c>Urn:</c> "accesspackage:area:fullmakter_for_konkursbo"
    /// - <c>IconUrl:</c> "Aksel_Statistics-and-math_TrendDown.svg"
    /// - <c>GroupId:</c> Special (Særskilt)
    /// </remarks>
    public static ConstantDefinition<Area> AuthorizationsForBankruptcyEstates { get; } = new ConstantDefinition<Area>("f3daddb7-6e21-455e-b6d2-65a281375b6b")
    {
        Entity = new()
        {
            Name = "Fullmakter for konkursbo",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til fullmakter for konkursbo.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Statistics-and-math_TrendDown.svg").ToString(),
            GroupId = AreaGroupConstants.Special,
            Urn = "accesspackage:area:fullmakter_for_konkursbo"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Authorizations for Bankruptcy Estates"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to authorizations for bankruptcy estates.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Fullmakter for konkursbo"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til fullmakter for konkursbo.")),
    };

    /// <summary>
    /// Represents the Authorizations for Businesses area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0195efb8-7c80-76b3-bb86-ae9dfd74bca2
    /// - <c>Name:</c> "Fullmakter for forretningsfører"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til fullmakter for forretningsfører."
    /// - <c>Urn:</c> "accesspackage:area:fullmakter_for_forretningsforer"
    /// - <c>IconUrl:</c> "Aksel_Statistics-and-math_TrendDown.svg"
    /// - <c>GroupId:</c> Special (Særskilt)
    /// </remarks>
    public static ConstantDefinition<Area> AuthorizationsForBusinesses { get; } = new ConstantDefinition<Area>("0195efb8-7c80-76b3-bb86-ae9dfd74bca2")
    {
        Entity = new()
        {
            Name = "Fullmakter for forretningsfører",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til fullmakter for forretningsfører.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Statistics-and-math_TrendDown.svg").ToString(),
            GroupId = AreaGroupConstants.Special,
            Urn = "accesspackage:area:fullmakter_for_forretningsforer"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Authorizations for Bussineses"),
            KeyValuePair.Create("Description", "This authorization area includes access packages related to authorizations for bussineses.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Fullmakter for forretningsfører"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytt til fullmakter for forretningsfører.")),
    };
}
