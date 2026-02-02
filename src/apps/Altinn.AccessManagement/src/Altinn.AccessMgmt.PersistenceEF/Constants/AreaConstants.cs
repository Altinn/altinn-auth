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
    /// Try to get <see cref="Area"/> by any identifier: Name or Guid.
    /// </summary>
    /// <returns></returns>
    public static bool TryGetByAll(string value, [NotNullWhen(true)] out ConstantDefinition<Area>? result)
    {
        if (TryGetByName(value, out result))
        {
            return true;
        }

        if (Guid.TryParse(value, out var areaGuid) && TryGetById(areaGuid, out result))
        {
            return true;
        }

        return false;
    }

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

    /// <summary>
    /// Represents the Working life, school and education area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 26be6035-a1a9-4ac2-a95b-0338c526f932
    /// - <c>Name:</c> "Arbeidsliv, skole og utdanning"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker som gir fullmakter til tjenester og ressurser som omhandler Arbeid, utdanning og arbeidsforhold. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmaktene gir."
    /// - <c>Urn:</c> "accesspackage:area:arbeidsliv_skole_og_utdanning"
    /// - <c>IconUrl:</c> "Aksel_Workplace_Buildings2.svg"
    /// - <c>GroupId:</c> Inhabitant (Innbygger)
    /// </remarks>
    public static ConstantDefinition<Area> WorkingLifeSchoolAndEducation { get; } = new ConstantDefinition<Area>("26be6035-a1a9-4ac2-a95b-0338c526f932")
    {
        Entity = new()
        {
            Name = "Arbeidsliv, skole og utdanning",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker som gir fullmakter til tjenester og ressurser som omhandler Arbeid, utdanning og arbeidsforhold. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmaktene gir.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Workplace_Buildings2.svg").ToString(),
            GroupId = AreaGroupConstants.Inhabitant,
            Urn = "accesspackage:area:arbeidsliv_skole_og_utdanning"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Working life, school and education"),
            KeyValuePair.Create("Description", "This authorization area includes access packages that grant authorizations for services and resources that deal with Work, education and working conditions. When new digital services are introduced, there may be changes in the access that the authorizations provide.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Arbeidsliv, skule og utdanning"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar som gir fullmakter til tenester og ressursar som omhandlar Arbeid, utdanning og arbeidsforhold. Ved innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmaktene gir.")),
    };

    /// <summary>
    /// Represents the Family and leisure area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0b3223ad-f02d-4249-8746-fc8e003292e0
    /// - <c>Name:</c> "Familie og fritid"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker som gir fullmakter til tjenester og ressurser knyttet til familie og fritid. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmaktene gir."
    /// - <c>Urn:</c> "accesspackage:area:familie_og_fritid"
    /// - <c>IconUrl:</c> "PersonTallShort.svg"
    /// - <c>GroupId:</c> Inhabitant (Innbygger)
    /// </remarks>
    public static ConstantDefinition<Area> FamilyAndLeisure { get; } = new ConstantDefinition<Area>("0b3223ad-f02d-4249-8746-fc8e003292e0")
    {
        Entity = new()
        {
            Name = "Familie og fritid",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker som gir fullmakter til tjenester og ressurser knyttet til familie og fritid. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmaktene gir.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "PersonTallShort.svg").ToString(),
            GroupId = AreaGroupConstants.Inhabitant,
            Urn = "accesspackage:area:familie_og_fritid"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Family and leisure"),
            KeyValuePair.Create("Description", "This authorization area includes access packages that grant authorizations for services and resources related to family and leisure. When new digital services are introduced, there may be changes in the access that the authorizations provide.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Familie og fritid"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar som gir fullmakter til tenester og ressursar knytte til familie og fritid. Ved innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmaktene gir.")),
    };

    /// <summary>
    /// Represents the Health and care area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 7547c6d0-42c5-4dfd-bb8e-46453fa011eb
    /// - <c>Name:</c> "Helse og omsorg"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker som gir fullmakter til tjenester og ressurser knyttet til helse og omsorg. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmaktene gir."
    /// - <c>Urn:</c> "accesspackage:area:helse_og_omsorg"
    /// - <c>IconUrl:</c> "Aksel_Wellness_Hospital.svg"
    /// - <c>GroupId:</c> Inhabitant (Innbygger)
    /// </remarks>
    public static ConstantDefinition<Area> HealthAndCare { get; } = new ConstantDefinition<Area>("7547c6d0-42c5-4dfd-bb8e-46453fa011eb")
    {
        Entity = new()
        {
            Name = "Helse og omsorg",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker som gir fullmakter til tjenester og ressurser knyttet til helse og omsorg. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmaktene gir.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Wellness_Hospital.svg").ToString(),
            GroupId = AreaGroupConstants.Inhabitant,
            Urn = "accesspackage:area:helse_og_omsorg"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Health and care"),
            KeyValuePair.Create("Description", "This authorization area includes access packages that grant authorizations for services and resources related to health and care. When new digital services are introduced, there may be changes in the access that the authorizations provide.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Familie og fritid"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar som gir fullmakter til tenester og ressursar knytte til helse og omsorg. Ved innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmaktene gir.")),
    };

    /// <summary>
    /// Represents the Culture, sport and volunteering area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 10a73dfd-1e70-44d4-b651-cd8db81e8b1b
    /// - <c>Name:</c> "Kultur, idrett og frivillighet"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker som gir fullmakter til tjenester innen kultur, idrett og fritidsaktiviteter. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmaktene gir."
    /// - <c>Urn:</c> "accesspackage:area:kultur_idrett_og_frivillighet"
    /// - <c>IconUrl:</c> "Aksel_Wellness_HeadHeart.svg"
    /// - <c>GroupId:</c> Inhabitant (Innbygger)
    /// </remarks>
    public static ConstantDefinition<Area> CultureSportAndVolunteering { get; } = new ConstantDefinition<Area>("10a73dfd-1e70-44d4-b651-cd8db81e8b1b")
    {
        Entity = new()
        {
            Name = "Kultur, idrett og frivillighet",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker som gir fullmakter til tjenester innen kultur, idrett og fritidsaktiviteter. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmaktene gir.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Wellness_HeadHeart.svg").ToString(),
            GroupId = AreaGroupConstants.Inhabitant,
            Urn = "accesspackage:area:kultur_idrett_og_frivillighet"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Culture, sport and volunteering"),
            KeyValuePair.Create("Description", "This authorized area includes access packages that grant authorizations for services within culture, sports and leisure activities. When new digital services are introduced, there may be changes in the access that the authorizations provide.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Kultur, idrett og frivilligheit"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar som gir fullmakter til tenester innan kultur, idrett og fritidsaktivitetar. Ved innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmaktene gir.")),
    };

    /// <summary>
    /// Represents the Patents, certificates, and attestations area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 36cac8e4-56dc-4825-a3ea-03f8f3d1a05b
    /// - <c>Name:</c> "Patenter, sertifikater og attester"
    /// - <c>Description:</c> "Fullmaktsområde for tilgangspakker for tjenester som er relatert til å søke om patent, sertifisering, attester, design og varemerker. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir."
    /// - <c>Urn:</c> "accesspackage:area:patenter_sertifikater_og_attester"
    /// - <c>IconUrl:</c> "Seal.svg"
    /// - <c>GroupId:</c> Inhabitant (Innbygger)
    /// </remarks>
    public static ConstantDefinition<Area> PatentsCertificatesAndAttestations { get; } = new ConstantDefinition<Area>("36cac8e4-56dc-4825-a3ea-03f8f3d1a05b")
    {
        Entity = new()
        {
            Name = "Patenter, sertifikater og attester",
            Description = "Fullmaktsområde for tilgangspakker for tjenester som er relatert til å søke om patent, sertifisering, attester, design og varemerker. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Seal.svg").ToString(),
            GroupId = AreaGroupConstants.Inhabitant,
            Urn = "accesspackage:area:patenter_sertifikater_og_attester"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Patents, certificates, and attestations"),
            KeyValuePair.Create("Description", "Authorization area for access packages for services related to applying for patents, certifications, certificates, designs and trademarks. When new digital services are introduced, there may be changes in the access that the authorization provides.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Patent, sertifikat og attestar"),
            KeyValuePair.Create("Description", "Fullmaktsområde for tilgangspakkar for tenester som er relaterte til å søkja om patent, sertifisering, attestar, design og varemerke. Ved innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")),
    };

    /// <summary>
    /// Represents the Police and judiciary area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0995c7e3-d791-4797-9ceb-1c40eadf9c81
    /// - <c>Name:</c> "Politi og rettsvesen"
    /// - <c>Description:</c> "Fullmaktsområde for tilgangspakker for tjenester som er relatert til politisaker og andre forhold til rettsvesenet. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir."
    /// - <c>Urn:</c> "accesspackage:area:politi_og_rettsvesen"
    /// - <c>IconUrl:</c> "GavelSoundBlock.svg"
    /// - <c>GroupId:</c> Inhabitant (Innbygger)
    /// </remarks>
    public static ConstantDefinition<Area> PoliceAndJudiciary { get; } = new ConstantDefinition<Area>("0995c7e3-d791-4797-9ceb-1c40eadf9c81")
    {
        Entity = new()
        {
            Name = "Politi og rettsvesen",
            Description = "Fullmaktsområde for tilgangspakker for tjenester som er relatert til politisaker og andre forhold til rettsvesenet. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "GavelSoundBlock.svg").ToString(),
            GroupId = AreaGroupConstants.Inhabitant,
            Urn = "accesspackage:area:politi_og_rettsvesen"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Police and judiciary"),
            KeyValuePair.Create("Description", "Authorization area for access packages for services related to police matters and other matters with the judiciary. When new digital services are introduced, there may be changes in the access that the authorization provides.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Politi og rettsvesen"),
            KeyValuePair.Create("Description", "Fullmaktsområde for tilgangspakkar for tenester som er relaterte til politisaker og andre forhold til rettsvesenet. Ved innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")),
    };

    /// <summary>
    /// Represents the Plan, building and property area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 4a9b468d-c008-4ad3-9532-ff4b949f13b7
    /// - <c>Name:</c> "Plan, bygg og eiendom"
    /// - <c>Description:</c> "Fullmaktsområde for tilgangspakker for tjenester som er relatert til søknader og korrespondanse innen plan og eiendom. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir."
    /// - <c>Urn:</c> "accesspackage:area:plan_bygg_og_eiendom"
    /// - <c>IconUrl:</c> "Altinn_Bygg-anlegg-og-eiendom_HandHouse.svg"
    /// - <c>GroupId:</c> Inhabitant (Innbygger)
    /// </remarks>
    public static ConstantDefinition<Area> PlanBuildingAndProperty { get; } = new ConstantDefinition<Area>("4a9b468d-c008-4ad3-9532-ff4b949f13b7")
    {
        Entity = new()
        {
            Name = "Plan, bygg og eiendom",
            Description = "Fullmaktsområde for tilgangspakker for tjenester som er relatert til søknader og korrespondanse innen plan og eiendom. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Altinn_Bygg-anlegg-og-eiendom_HandHouse.svg").ToString(),
            GroupId = AreaGroupConstants.Inhabitant,
            Urn = "accesspackage:area:plan_bygg_og_eiendom"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Plan, building and property"),
            KeyValuePair.Create("Description", "Authorization area for access packages for services related to applications and correspondence within planning and property. When new digital services are introduced, there may be changes in the access that the authorization provides.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Plan, bygg og eigedom"),
            KeyValuePair.Create("Description", "Fullmaktsområde for tilgangspakkar for tenester som er relaterte til søknader og korrespondanse innan plan og eigedom. Ved innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")),
    };

    /// <summary>
    /// Represents the Traffic and transport area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 287836cc-69ee-4220-84f1-35623a85ec7b
    /// - <c>Name:</c> "Trafikk og transport"
    /// - <c>Description:</c> "Fullmaktsområde for tilgangspakker for tjenester som er relatert til søknader og korrespondanse som gjelder trafikk og transportforhold. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir."
    /// - <c>Urn:</c> "accesspackage:area:trafikk_og_transport"
    /// - <c>IconUrl:</c> "Car.svg"
    /// - <c>GroupId:</c> Inhabitant (Innbygger)
    /// </remarks>
    public static ConstantDefinition<Area> TrafficAndTransport { get; } = new ConstantDefinition<Area>("287836cc-69ee-4220-84f1-35623a85ec7b")
    {
        Entity = new()
        {
            Name = "Trafikk og transport",
            Description = "Fullmaktsområde for tilgangspakker for tjenester som er relatert til søknader og korrespondanse som gjelder trafikk og transportforhold. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Car.svg").ToString(),
            GroupId = AreaGroupConstants.Inhabitant,
            Urn = "accesspackage:area:trafikk_og_transport"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Traffic and transport"),
            KeyValuePair.Create("Description", "Authorization area for access packages for services related to applications and correspondence relating to traffic and transport conditions. When new digital services are introduced, there may be changes in the access that the authorization provides.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Plan, bygg og eigedom"),
            KeyValuePair.Create("Description", "Fullmaktsområde for tilgangspakkar for tenester som er relaterte til søknader og korrespondanse som gjeld trafikk og transportforhold. Ved innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")),
    };

    /// <summary>
    /// Represents the Tax, levy, bank and insurance area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> dcc8daab-e658-4251-a94f-a9e1c06b3833
    /// - <c>Name:</c> "Skatt, avgift, bank og forsikring"
    /// - <c>Description:</c> "Fullmaktsområde for tilgangspakker for tjenester som er relatert til søknader og korrespondanse som gjelder skatt, avgifter, bank, forsikring og andre økonomiske forhold. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir."
    /// - <c>Urn:</c> "accesspackage:area:skatt_avgift_bank_og_forsikring"
    /// - <c>IconUrl:</c> "Aksel_Money_SackKroner.svg"
    /// - <c>GroupId:</c> Inhabitant (Innbygger)
    /// </remarks>
    public static ConstantDefinition<Area> TaxLevyBankAndInsurance { get; } = new ConstantDefinition<Area>("dcc8daab-e658-4251-a94f-a9e1c06b3833")
    {
        Entity = new()
        {
            Name = "Skatt, avgift, bank og forsikring",
            Description = "Fullmaktsområde for tilgangspakker for tjenester som er relatert til søknader og korrespondanse som gjelder skatt, avgifter, bank, forsikring og andre økonomiske forhold. Ved innføring av nye digitale tjenester kan det bli endringer i tilganger som fullmakten gir.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Money_SackKroner.svg").ToString(),
            GroupId = AreaGroupConstants.Inhabitant,
            Urn = "accesspackage:area:skatt_avgift_bank_og_forsikring"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Tax, levy, bank and insurance"),
            KeyValuePair.Create("Description", "Authorization area for access packages for services related to applications and correspondence relating to tax, duties, banking, insurance and other financial matters. When new digital services are introduced, there may be changes in the access that the authorization provides.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Skatt, avgift, bank og forsikring"),
            KeyValuePair.Create("Description", "Fullmaktsområde for tilgangspakkar for tenester som er relaterte til søknader og korrespondanse som gjeld skatt, avgifter, bank, forsikring og andre økonomiske forhold. Ved innføring av nye digitale tenester kan det bli endringar i tilgangar som fullmakta gir.")),
    };

    /// <summary>
    /// Represents the Private individual area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 7d77a783-710c-463d-9187-ed1f4bbc23ad
    /// - <c>Name:</c> "Administratorrettigheter"
    /// - <c>Description:</c> "Dette fullmaktsområde skal gi privatperson mulighet til å delegere tilgangsstyring til andre."
    /// - <c>Urn:</c> "accesspackage:area:administratorrettigheter"
    /// - <c>IconUrl:</c> "PersonSuit.svg"
    /// - <c>GroupId:</c> Inhabitant (Innbygger)
    /// </remarks>
    public static ConstantDefinition<Area> AdministratorRights { get; } = new ConstantDefinition<Area>("7d77a783-710c-463d-9187-ed1f4bbc23ad")
    {
        Entity = new()
        {
            Name = "Administratorrettigheter",
            Description = "Dette fullmaktsområde skal gi privatperson mulighet til å delegere tilgangsstyring til andre.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "PersonSuit.svg").ToString(),
            GroupId = AreaGroupConstants.Inhabitant,
            Urn = "accesspackage:area:administratorrettigheter"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Administrator rights"),
            KeyValuePair.Create("Description", "This area of ​​authority shall give the private person the opportunity to delegate access control to others.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Administratorrettar"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet skal gi privatperson høve til å delegera tilgangsstyring til andre.")),
    };

    /// <summary>
    /// Represents the Guardianship area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 2064fcbe-4eb5-42e3-9966-fd8aef27f36a
    /// - <c>Name:</c> "Vergemål"
    /// - <c>Description:</c> "Vergefullmakten legitimerer at vergen kan opptre på vegne av personene som har verge. I vergefullmakten står det hvilke oppgaver vergen kan bistå med. Hva som står i vergefullmakten varierer ut fra hvilket bistandsbehov personen med verge har, og hva personen har samtykket til."
    /// - <c>Urn:</c> "accesspackage:area:vergemal"
    /// - <c>IconUrl:</c> "Aksel_Statistics-and-math_TrendDown.svg" XXX
    /// - <c>GroupId:</c> Inhabitant (Innbygger)
    /// </remarks>
    public static ConstantDefinition<Area> Guardianship { get; } = new ConstantDefinition<Area>("2064fcbe-4eb5-42e3-9966-fd8aef27f36a")
    {
        Entity = new()
        {
            Name = "Vergemål",
            Description = "Vergefullmakten legitimerer at vergen kan opptre på vegne av personene som har verge. I vergefullmakten står det hvilke oppgaver vergen kan bistå med. Hva som står i vergefullmakten varierer ut fra hvilket bistandsbehov personen med verge har, og hva personen har samtykket til.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "Aksel_Statistics-and-math_TrendDown.svg").ToString(),
            GroupId = AreaGroupConstants.Inhabitant,
            Urn = "accesspackage:area:vergemal"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Guardianship"),
            KeyValuePair.Create("Description", "Access directly linked to the role of private person in the National Register. This role can still provide access to services that only exist in the old solution. See the list in the old solution.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Verjemål"),
            KeyValuePair.Create("Description", "Tilgangar knytte direkte til rolla som privatperson i Folkeregisteret. Denne rolla kan framleis gi tilgang til tenester som berre finst i den gamle løysinga. Sjå lista i den gamle løysinga.")),
    };

    /// <summary>
    /// Represents the Vergemål Bank area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 2b855fbf-0104-4c3c-a115-4e0dacfb0bf1
    /// - <c>Name:</c> "Bank"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til banktjenester."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-bank"
    /// - <c>IconUrl:</c> "ShieldLock.svg" 
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalBank { get; } = new ConstantDefinition<Area>("2b855fbf-0104-4c3c-a115-4e0dacfb0bf1")
    {
        Entity = new()
        {
            Name = "Bank",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til banktjenester.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-bank"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Bank"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to Banks.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Bank"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til banktenester.")),
    };

    /// <summary>
    /// Represents the Vergemål Insurance company area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> aa9b2a34-fdfd-4c3d-9894-06795f8a621f
    /// - <c>Name:</c> "Forsikringsselskap"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til forsikring."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-forsikringsselskap"
    /// - <c>IconUrl:</c> "ShieldLock.svg" 
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalInsuranceCompany { get; } = new ConstantDefinition<Area>("aa9b2a34-fdfd-4c3d-9894-06795f8a621f")
    {
        Entity = new()
        {
            Name = "Forsikringsselskap",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til forsikring.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-forsikringsselskap"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Insurance company"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to Insurance companies.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Forsikringsselskap"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til forsikring.")),
    };

    /// <summary>
    /// Represents the Vergemål The house bank area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 07524eb8-7db0-49ac-a34c-4cc56501f26f
    /// - <c>Name:</c> "Husbanken"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til husbanken."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-husbanken"
    /// - <c>IconUrl:</c> "ShieldLock.svg" 
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalTheHouseBank { get; } = new ConstantDefinition<Area>("07524eb8-7db0-49ac-a34c-4cc56501f26f")
    {
        Entity = new()
        {
            Name = "Husbanken",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til husbanken.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-husbanken"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "The house bank"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to The house bank.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Husbanken"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til Husbanken.")),
    };

    /// <summary>
    /// Represents the Vergemål Debt collection company area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 8d4c94dc-bf59-4f3d-bf3f-c4915d582997
    /// - <c>Name:</c> "Inkassoselskap"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til inkassoselskap."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-inkassoselskap"
    /// - <c>IconUrl:</c> "ShieldLock.svg" 
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalDebtCollectionCompany { get; } = new ConstantDefinition<Area>("8d4c94dc-bf59-4f3d-bf3f-c4915d582997")
    {
        Entity = new()
        {
            Name = "Inkassoselskap",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til inkassoselskap.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-inkassoselskap"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Debt collection company"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to debt collection companies.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Inkassoselskap"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til inkassoselskap.")),
    };

    /// <summary>
    /// Represents the Vergemål Debt collection company area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 1096b041-b6cb-4e2e-94d8-55843148c4bf
    /// - <c>Name:</c> "Kartverket"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Kartverket."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-kartverket"
    /// - <c>IconUrl:</c> "ShieldLock.svg" 
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalNorwegianMappingAuthority { get; } = new ConstantDefinition<Area>("1096b041-b6cb-4e2e-94d8-55843148c4bf")
    {
        Entity = new()
        {
            Name = "Kartverket",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Kartverket.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-kartverket"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Norwegian Mapping Authority"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to he Norwegian Mapping Authority.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Kartverket"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til Kartverket.")),
    };

    /// <summary>
    /// Represents the Vergemål Credit rating company area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 217820cc-911d-416b-92f1-fb5dc0cc1f04
    /// - <c>Name:</c> "Kredittvurderingsselskap"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Kredittvurderingsselskap."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-kredittvurderingsselskap"
    /// - <c>IconUrl:</c> "ShieldLock.svg" 
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalCreditRatingCompany { get; } = new ConstantDefinition<Area>("217820cc-911d-416b-92f1-fb5dc0cc1f04")
    {
        Entity = new()
        {
            Name = "Kredittvurderingsselskap",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Kredittvurderingsselskap.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-kredittvurderingsselskap"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Credit rating company"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to credit rating companies.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Kredittvurderingsselskap"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til Kredittvurderingsselskap.")),
    };

    /// <summary>
    /// Represents the Vergemål Bailiff area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 46d7f172-caad-4494-9c6d-3add8d120920
    /// - <c>Name:</c> "Namsmannen"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Namsmannen."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-namsmannen"
    /// - <c>IconUrl:</c> "ShieldLock.svg" 
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalBailiff { get; } = new ConstantDefinition<Area>("46d7f172-caad-4494-9c6d-3add8d120920")
    {
        Entity = new()
        {
            Name = "Namsmannen",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Namsmannen.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-namsmannen"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Bailiff"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to the Bailiff.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Namsmannen"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til Namsmannen.")),
    };

    /// <summary>
    /// Represents the Vergemål Tax authority area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> a0d54306-9af8-48c3-beb4-e37085642c95
    /// - <c>Name:</c> "Skatteetaten"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Skatteetaten."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-skatteetaten"
    /// - <c>IconUrl:</c> "ShieldLock.svg" 
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalTaxAuthority { get; } = new ConstantDefinition<Area>("a0d54306-9af8-48c3-beb4-e37085642c95")
    {
        Entity = new()
        {
            Name = "Skatteetaten",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Skatteetaten.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-skatteetaten"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "The tax authority"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to the Tax Authority.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Skatteetaten"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til Skatteetaten.")),
    };

    /// <summary>
    /// Represents the Vergemål Norwegian Collection Center area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 4a780efa-8609-43be-b059-6c59473c46dd
    /// - <c>Name:</c> "Statens Innkrevingssentral"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Statens Innkrevingssentral."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-statens-innkrevingssentral"
    /// - <c>IconUrl:</c> "ShieldLock.svg" 
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalNorwegianCollectionCenter { get; } = new ConstantDefinition<Area>("4a780efa-8609-43be-b059-6c59473c46dd")
    {
        Entity = new()
        {
            Name = "Statens Innkrevingssentral",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Statens Innkrevingssentral.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-statens-innkrevingssentral"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "The Norwegian Collection Center"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to the Norwegian Collection Center.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Statens innkrevjingssentral"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til Statens Innkrevjingssentral.")),
    };

    /// <summary>
    /// Represents the Vergemål Tax authority area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 28920dfd-e934-4f47-929e-c32359c92896
    /// - <c>Name:</c> "Statsforvalteren"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Statsforvelteren."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-statsforvalteren"
    /// - <c>IconUrl:</c> "ShieldLock.svg" 
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalStateAdministrator { get; } = new ConstantDefinition<Area>("28920dfd-e934-4f47-929e-c32359c92896")
    {
        Entity = new()
        {
            Name = "Statsforvalteren",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Statsforvelteren.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-statsforvalteren"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "The State Administrator"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to the State Administrator.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Statsforvaltaren"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til Statsforvaltaren.")),
    };

    /// <summary>
    /// Represents the Vergemål District Court area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> b34f7595-498f-458c-8a18-06ca2e8d8918
    /// - <c>Name:</c> "Tingretten"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Tingretten."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-tingretten"
    /// - <c>IconUrl:</c> "ShieldLock.svg" 
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalDistrictCourt { get; } = new ConstantDefinition<Area>("b34f7595-498f-458c-8a18-06ca2e8d8918")
    {
        Entity = new()
        {
            Name = "Tingretten",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Tingretten.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-tingretten"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "District Court"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to the District Court.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Tingretten"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til Tingretten.")),
    };

    /// <summary>
    /// Represents the Vergemål Other purchases and conclusion of agreements area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 4cf6b40c-24f2-4006-b280-b73b06d23504
    /// - <c>Name:</c> "Annen kjøp og avtaleinngåelse"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Annen kjøp og avtaleinngåelse."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-annen-kjop-avtaleinngaelse"
    /// - <c>IconUrl:</c> "ShieldLock.svg" 
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalOtherPurchasesAndConclusionsOfAgreemets { get; } = new ConstantDefinition<Area>("4cf6b40c-24f2-4006-b280-b73b06d23504")
    {
        Entity = new()
        {
            Name = "Annen kjøp og avtaleinngåelse",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Annen kjøp og avtaleinngåelse.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-annen-kjop-avtaleinngaelse"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Other purchases and conclusion of agreements"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to other purchases and conclusion of agreements.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Anna kjøp og avtaleinngåing"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til anna kjøp og avtaleinngåing.")),
    };

    /// <summary>
    /// Represents the Vergemål Municipality area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0c1c1143-5e8d-4979-aef8-7de61f05a467
    /// - <c>Name:</c> "Kommune"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Kommune."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-kommune"
    /// - <c>IconUrl:</c> "ShieldLock.svg" 
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalMunicipality { get; } = new ConstantDefinition<Area>("0c1c1143-5e8d-4979-aef8-7de61f05a467")
    {
        Entity = new()
        {
            Name = "Kommune",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Kommune.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-kommune"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Municipality"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to municipalities.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Kommune"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til kommune.")),
    };

    /// <summary>
    /// Represents the Vergemål NAV area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 1dfa599d-1ade-48bd-86b4-ac033027c3d5
    /// - <c>Name:</c> "NAV"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til NAV."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-nav"
    /// - <c>IconUrl:</c> "ShieldLock.svg" 
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalNav { get; } = new ConstantDefinition<Area>("1dfa599d-1ade-48bd-86b4-ac033027c3d5")
    {
        Entity = new()
        {
            Name = "NAV",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til NAV.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-nav"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "NAV"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to NAV.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "NAV"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til NAV.")),
    };

    /// <summary>
    /// Represents the Vergemål Patient travel area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> ad1e864d-5ad0-4063-aa70-f5b7d431da50
    /// - <c>Name:</c> "Pasientreiser"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Pasientreiser."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-pasientreiser"
    /// - <c>IconUrl:</c> "ShieldLock.svg" 
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalPatientTravel { get; } = new ConstantDefinition<Area>("ad1e864d-5ad0-4063-aa70-f5b7d431da50")
    {
        Entity = new()
        {
            Name = "Pasientreiser",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Pasientreiser.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-pasientreiser"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Patient travel"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to patient travel.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Pasientreiser"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til Pasientreiser.")),
    };

    /// <summary>
    /// Represents the Vergemål Helfo area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 0f1095a5-1e2b-4756-b376-bab8c36a89e4
    /// - <c>Name:</c> "Helfo"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Helfo."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-helfo"
    /// - <c>IconUrl:</c> "ShieldLock.svg"
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalHelfo { get; } = new ConstantDefinition<Area>("0f1095a5-1e2b-4756-b376-bab8c36a89e4")
    {
        Entity = new()
        {
            Name = "Helfo",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Helfo.",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-helfo"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Helfo"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to Helfo.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Helfo"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til Helfo.")),
    };

    /// <summary>
    /// Represents the Vergemål Helfo area.
    /// </summary>
    /// <remarks>
    /// - <c>Id:</c> 43e8458d-4b88-49b6-b079-38dfa1ba663a
    /// - <c>Name:</c> "Pasientopplysninger og -tjenester"
    /// - <c>Description:</c> "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Pasientopplysninger og -tjenester."
    /// - <c>Urn:</c> "accesspackage:area:vergemal-pasientopplysninger-tjenester"
    /// - <c>IconUrl:</c> "ShieldLock.svg"
    /// - <c>GroupId:</c> Guardianship (Vergemål)
    /// </remarks>
    public static ConstantDefinition<Area> VergemalPatientInformationAndServices { get; } = new ConstantDefinition<Area>("43e8458d-4b88-49b6-b079-38dfa1ba663a")
    {
        Entity = new()
        {
            Name = "Pasientopplysninger og -tjenester",
            Description = "Dette fullmaktsområdet omfatter tilgangspakker knyttet til Pasientopplysninger og -tjenester",
            IconUrl = new Uri(AltinnCDNPackageIcons, "ShieldLock.svg").ToString(),
            GroupId = AreaGroupConstants.Guardianship,
            Urn = "accesspackage:area:vergemal-pasientopplysninger-tjenester"
        },
        EN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Patient information and services"),
            KeyValuePair.Create("Description", "This area of ​​authority includes access packages linked to patient information and services.")),
        NN = TranslationEntryList.Create(
            KeyValuePair.Create("Name", "Pasientopplysningar og -tenester"),
            KeyValuePair.Create("Description", "Dette fullmaktsområdet omfattar tilgangspakkar knytte til pasientopplysningar og -tenester.")),
    };
}
