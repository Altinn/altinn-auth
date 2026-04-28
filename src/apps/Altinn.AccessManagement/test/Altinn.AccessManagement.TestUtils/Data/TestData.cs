using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessManagement.TestUtils.Data;

public static class TestData
{
    #region Resource Types

    /// <summary>
    /// A generic test resource type used across integration tests.
    /// </summary>
    public static ResourceType TestResourceType { get; } = new()
    {
        Id = Guid.Parse("0195efb8-7c80-7f26-817a-50893176320d"),
        Name = "Test",
    };

    /// <summary>
    /// A generic test resource type used across integration tests.
    /// </summary>
    public static ResourceType CorrespondenceResourceType { get; } = new()
    {
        Id = Guid.Parse("019d95d1-95c7-7887-91cc-30fc9af97191"),
        Name = "CorrespondenceService",
    };

    #endregion

    #region Resources

    public static readonly Resource TestdirektoratetCorrespondenceService = new()
    {
        Id = Guid.Parse("019d95d3-92d5-750f-a594-a8b08e756a4a"),
        Name = "Testdirektoratet Correspondence Service",
        Description = "Messaging service for testing instance delegation from Altinn II",
        RefId = "ttd-migratedcorrespondence-4471-1",
        TypeId = TestData.CorrespondenceResourceType.Id,
        ProviderId = ProviderConstants.Altinn2.Id,
    };

    public static readonly Resource MattilsynetBakeryService = new()
    {
        Id = Guid.Parse("0195efb8-7c80-7f26-817a-50893176320c"),
        Name = "Mattilsynet Bakery Service",
        Description = "Service for bakery inspections and food safety reporting to the Norwegian Food Safety Authority",
        RefId = "app_mat_mattilsynet-baker-konditorvare",
        TypeId = TestData.TestResourceType.Id,
        ProviderId = ProviderConstants.Altinn3.Id,
    };

    public static readonly Resource SiriusSkattemelding = new()
    {
        Id = Guid.Parse("0195efb8-7c80-7f26-817a-50893176320e"),
        Name = "Skattemelding med næringsspesifikasjon 2020",
        Description = "Skattemelding med næringsspesifikasjon 2020",
        RefId = "app_skd_sirius-skattemelding-v1",
        TypeId = TestData.TestResourceType.Id,
        ProviderId = ProviderConstants.Altinn3.Id,
    };

    public static readonly Resource NavSykepengerDialog = new()
    {
        Id = Guid.Parse("0195efb8-7c80-7f26-817a-50893176320f"),
        Name = "Dialogs for sickness benefits",
        Description = "The service is used to send and receive dialogues in Dialogporten about new sick leaves, submitted applications, requests for income reports, and receipts for submitted income reports.",
        RefId = "nav_sykepenger_dialog",
        TypeId = TestData.TestResourceType.Id,
        ProviderId = ProviderConstants.Altinn3.Id,
    };

    public static readonly Resource DiheOmsetningsoppgaveAlkohol = new()
    {
        Id = Guid.Parse("0195efb8-7c80-7f26-817a-508931763210"),
        Name = "Omsetningsoppgave for alkohol",
        Description = "Omsetningsoppgave for alkohol",
        RefId = "app_dihe_omsetningsoppgave-for-alkohol",
        TypeId = TestData.TestResourceType.Id,
        ProviderId = ProviderConstants.Altinn3.Id,
    };

    public static readonly Resource NavSykepengerSykmelding = new()
    {
        Id = Guid.Parse("0195efb8-7c80-7f26-817a-508931763211"),
        Name = "Sykmelding til arbeidsgiver",
        Description = "Sykmelding fra arbeidstaker gjør at arbeidsgiver kan motta sykmeldingen som den sykmeldte sender fra nav.no.",
        RefId = "nav_sykepenger_sykmelding",
        TypeId = TestData.TestResourceType.Id,
        ProviderId = ProviderConstants.Altinn3.Id,
    };

    public static readonly Resource SkattResource = new()
    {
        Name = "Skattemelding",
        Description = "Innlevering av skattemelding for næringsdrivende",
        RefId = "app_skd_skattemelding",
        TypeId = TestData.TestResourceType.Id,
        ProviderId = ProviderConstants.Altinn3.Id,
    };

    public static readonly Resource MvaResource = new()
    {
        Name = "MVA-melding",
        Description = "Innlevering av merverdiavgiftsmelding",
        RefId = "app_skd_mva-melding",
        TypeId = TestData.TestResourceType.Id,
        ProviderId = ProviderConstants.Altinn3.Id,
    };

    #endregion

    #region Firmaer

    public static ConstantDefinition<Entity> HanSoloEnterprise { get; } = new("c3af1102-9ea6-4335-800d-fb9a80b322ae")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Han Solo Enterprise",
            OrganizationIdentifier = "305000043",
            Parent = null,
            ParentId = null,
            PartyId = 50300004,
            PersonIdentifier = null,
            RefId = "305000043",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
        }
    };

    public static ConstantDefinition<Entity> BakerJohnsen { get; } = new("a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Baker Johnsen",
            OrganizationIdentifier = "913456785",
            Parent = null,
            ParentId = null,
            PartyId = 50100001,
            PersonIdentifier = null,
            RefId = "913456785",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
        }
    };

    public static ConstantDefinition<Entity> SvendsenAutomobil { get; } = new("b2c3d4e5-f6a7-4b8c-9d0e-1f2a3b4c5d6e")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Svendsen Automobil",
            OrganizationIdentifier = "876543214",
            Parent = null,
            ParentId = null,
            PartyId = 50100002,
            PersonIdentifier = null,
            RefId = "876543214",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
        }
    };

    public static ConstantDefinition<Entity> FredriksonsFabrikk { get; } = new("c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Fredriksons Fabrikk",
            OrganizationIdentifier = "246813574",
            Parent = null,
            ParentId = null,
            PartyId = 50100003,
            PersonIdentifier = null,
            RefId = "246813574",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
        }
    };

    public static ConstantDefinition<Entity> DumboAdventures { get; } = new("063b5a5e-e4e3-4ac0-bfe6-ef0818c5445d")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Dumbo Adventures AS",
            OrganizationIdentifier = "313783510",
            Parent = null,
            ParentId = null,
            PartyId = 50083510,
            PersonIdentifier = null,
            RefId = "313783510",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
        }
    };

    public static ConstantDefinition<Entity> MilleHundefrisor { get; } = new("019d1b09-cb7a-747c-ab62-fa35a9c66ba9")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Mille Hundefrisør",
            OrganizationIdentifier = "314255461",
            Parent = null,
            ParentId = null,
            PartyId = 50155461,
            PersonIdentifier = null,
            RefId = "314255461",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.ENK,
        }
    };

    public static ConstantDefinition<Entity> KaosMagicDesignAndArts { get; } = new("7a1b2c3d-4e5f-4678-9abc-def012345670")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Kaos Magic Design and Arts",
            OrganizationIdentifier = "315678900",
            Parent = null,
            ParentId = null,
            PartyId = 50315678,
            PersonIdentifier = null,
            RefId = "315678900",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
        }
    };

    public static ConstantDefinition<Entity> NAV { get; } = new("019d1a5e-68b6-754c-8ee2-e79f4ac137cc")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Arbeids- og velferdsetaten (NAV)",
            OrganizationIdentifier = "889640782",
            Parent = null,
            ParentId = null,
            PartyId = null,
            PersonIdentifier = null,
            RefId = "889640782",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
        }
    };

    public static ConstantDefinition<Provider> ServiceOwnerNAV { get; } = new("e95e2ed7-67b3-3983-56ed-9f88a960c686")
    {
        Entity = new()
        {
            Name = "Arbeids- og velferdsetaten (NAV)",
            RefId = "889640782",
            TypeId = ProviderTypeConstants.ServiceOwner,
            Code = "nav",
        }
    };

    #endregion

    #region Regnskapsselskaper

    public static ConstantDefinition<Entity> RegnskapNorge { get; } = new("d4e5f6a7-b8c9-4d0e-1f2a-3b4c5d6e7f80")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "RegnskapNorge",
            OrganizationIdentifier = "310000000",
            Parent = null,
            ParentId = null,
            PartyId = 50100004,
            PersonIdentifier = null,
            RefId = "310000000",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
        }
    };

    public static ConstantDefinition<Entity> MittRegnskap { get; } = new("e5f6a7b8-c9d0-4e1f-2a3b-4c5d6e7f8091")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "MittRegnskap",
            OrganizationIdentifier = "314265971",
            Parent = null,
            ParentId = null,
            PartyId = 50265971,
            PersonIdentifier = null,
            RefId = "314265971",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
        }
    };

    #endregion

    #region Revisorselskap

    public static ConstantDefinition<Entity> RpcAS { get; } = new("f6a7b8c9-d0e1-4f2a-3b4c-5d6e7f8091a2")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "RPC AS",
            OrganizationIdentifier = "312889285",
            Parent = null,
            ParentId = null,
            PartyId = 50189285,
            PersonIdentifier = null,
            RefId = "312889285",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
        }
    };

    #endregion

    #region Tjenesteeier

    public static ConstantDefinition<Entity> StorMektigTenesteeier { get; } = new("5f6ed073-0aba-4562-a6d1-e31539e2f938")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Stor og Mektig Tjenesteeier",
            OrganizationIdentifier = "974761076",
            Parent = null,
            ParentId = null,
            PartyId = 501161076,
            PersonIdentifier = null,
            RefId = "974761076",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
        }
    };

    #endregion

    #region Personer - Baker Johnsen

    public static ConstantDefinition<Entity> LarsBakke { get; } = new("10000001-aaaa-4bbb-8ccc-ddddeeee0001")
    {
        Entity = new()
        {
            DateOfBirth = new(1975, 3, 12),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Lars Bakke",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200001,
            PersonIdentifier = "29814997306",
            RefId = "29814997306",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100001,
            Username = "lars.bakke",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> HildeStrand { get; } = new("10000002-aaaa-4bbb-8ccc-ddddeeee0002")
    {
        Entity = new()
        {
            DateOfBirth = new(1968, 7, 22),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Hilde Strand",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200002,
            PersonIdentifier = "22076800077",
            RefId = "22076800077",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100002,
            Username = "hilde.strand",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> KnutVik { get; } = new("10000003-aaaa-4bbb-8ccc-ddddeeee0003")
    {
        Entity = new()
        {
            DateOfBirth = new(1982, 11, 5),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Knut Vik",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200003,
            PersonIdentifier = "05118200081",
            RefId = "05118200081",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100003,
            Username = "knut.vik",
            VariantId = EntityVariantConstants.Person,
        }
    };

    #endregion

    #region Personer - Svendsen Automobil

    public static ConstantDefinition<Entity> MortenDahl { get; } = new("10000004-aaaa-4bbb-8ccc-ddddeeee0004")
    {
        Entity = new()
        {
            DateOfBirth = new(1979, 2, 18),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Morten Dahl",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200004,
            PersonIdentifier = "18027900190",
            RefId = "18027900190",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100004,
            Username = "morten.dahl",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> GreteHolm { get; } = new("10000005-aaaa-4bbb-8ccc-ddddeeee0005")
    {
        Entity = new()
        {
            DateOfBirth = new(1970, 9, 30),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Grete Holm",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200005,
            PersonIdentifier = "30097000030",
            RefId = "30097000030",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100005,
            Username = "grete.holm",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> ArneLund { get; } = new("10000006-aaaa-4bbb-8ccc-ddddeeee0006")
    {
        Entity = new()
        {
            DateOfBirth = new(1985, 4, 14),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Arne Lund",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200006,
            PersonIdentifier = "14048500189",
            RefId = "14048500189",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100006,
            Username = "arne.lund",
            VariantId = EntityVariantConstants.Person,
        }
    };

    #endregion

    #region Personer - Dumbo Adventures

    public static ConstantDefinition<Entity> MalinEmilie { get; } = new("3a53efdd-c152-436d-800b-bc3d2bada0f9")
    {
        Entity = new()
        {
            DateOfBirth = new(2002, 4, 15),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Malin Emilie",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50096763,
            PersonIdentifier = "05887196763",
            RefId = "05887196763",
            TypeId = EntityTypeConstants.Person,
            UserId = 20096763,
            Username = "malin.emilie",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> Thea { get; } = new("8f52236a-7fbb-4681-ae45-429de0c747e3")
    {
        Entity = new()
        {
            DateOfBirth = new(2002, 4, 15),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Thea BFF",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50049134,
            PersonIdentifier = "11813349134",
            RefId = "11813349134",
            TypeId = EntityTypeConstants.Person,
            UserId = 20049134,
            Username = "thea.bff",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> JosephineYvonnesdottir { get; } = new("eeec4506-51d7-40d2-a0ef-38b95c95dff6")
    {
        Entity = new()
        {
            DateOfBirth = new(1995, 9, 15),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Josephine Yvonnesdottir",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 5049963,
            PersonIdentifier = "02883749963",
            RefId = "02883749963",
            TypeId = EntityTypeConstants.Person,
            UserId = 20049963,
            Username = "josephine.yvonnesdottir",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> BodilFarmor { get; } = new("10000023-aaaa-4bbb-8ccc-ddddeeee0023")
    {
        Entity = new()
        {
            DateOfBirth = new(1960, 3, 15),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Bodil Farmor",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200023,
            PersonIdentifier = "15036000187",
            RefId = "15036000187",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100023,
            Username = "bodil.farmor",
            VariantId = EntityVariantConstants.Person,
        }
    };

    #endregion

    #region Personer - Mille Hundefrisør

    public static ConstantDefinition<Entity> Milena { get; } = new("10000024-aaaa-4bbb-8ccc-ddddeeee0024")
    {
        Entity = new()
        {
            DateOfBirth = new(1990, 5, 10),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Milena Solstad",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50298981,
            PersonIdentifier = "28849198981",
            RefId = "28849198981",
            TypeId = EntityTypeConstants.Person,
            UserId = 20198981,
            Username = "milena.solstad",
            VariantId = EntityVariantConstants.Person,
        }
    };

    #endregion

    #region Personer - Kaos Magic Design and Arts

    public static ConstantDefinition<Entity> JinxArcane { get; } = new("7a1b2c3d-4e5f-4678-9abc-def012345671")
    {
        Entity = new()
        {
            DateOfBirth = new(1993, 7, 22),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Jinx Arcane",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50401001,
            PersonIdentifier = "22079340306",
            RefId = "22079340306",
            TypeId = EntityTypeConstants.Person,
            UserId = 20401001,
            Username = "jinx.arcane",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> AlexTheArtist { get; } = new("7a1b2c3d-4e5f-4678-9abc-def012345672")
    {
        Entity = new()
        {
            DateOfBirth = new(1988, 3, 14),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Alex The Artist",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50401002,
            PersonIdentifier = "14038840283",
            RefId = "14038840283",
            TypeId = EntityTypeConstants.Person,
            UserId = 20401002,
            Username = "alex.theartist",
            VariantId = EntityVariantConstants.Person,
        }
    };

    #endregion

    #region Personer - Fredriksons Fabrikk

    public static ConstantDefinition<Entity> SiljeHaugen { get; } = new("10000007-aaaa-4bbb-8ccc-ddddeeee0007")
    {
        Entity = new()
        {
            DateOfBirth = new(1988, 6, 21),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Silje Haugen",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200007,
            PersonIdentifier = "21068800185",
            RefId = "21068800185",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100007,
            Username = "silje.haugen",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> EinarBerg { get; } = new("10000008-aaaa-4bbb-8ccc-ddddeeee0008")
    {
        Entity = new()
        {
            DateOfBirth = new(1965, 1, 8),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Einar Berg",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200008,
            PersonIdentifier = "08016500047",
            RefId = "08016500047",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100008,
            Username = "einar.berg",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> ToneKvam { get; } = new("10000009-aaaa-4bbb-8ccc-ddddeeee0009")
    {
        Entity = new()
        {
            DateOfBirth = new(1991, 12, 3),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Tone Kvam",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200009,
            PersonIdentifier = "03129100000",
            RefId = "03129100000",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100009,
            Username = "tone.kvam",
            VariantId = EntityVariantConstants.Person,
        }
    };

    #endregion

    #region Personer - RegnskapNorge

    public static ConstantDefinition<Entity> BjornMoe { get; } = new("10000010-aaaa-4bbb-8ccc-ddddeeee0010")
    {
        Entity = new()
        {
            DateOfBirth = new(1972, 8, 15),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Bjorn Moe",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200010,
            PersonIdentifier = "22847596388",
            RefId = "22847596388",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100010,
            Username = "bjorn.moe",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> RandiLie { get; } = new("10000011-aaaa-4bbb-8ccc-ddddeeee0011")
    {
        Entity = new()
        {
            DateOfBirth = new(1967, 5, 28),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Randi Lie",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200011,
            PersonIdentifier = "28056700090",
            RefId = "28056700090",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100011,
            Username = "randi.lie",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> VegardSolberg { get; } = new("10000012-aaaa-4bbb-8ccc-ddddeeee0012")
    {
        Entity = new()
        {
            DateOfBirth = new(1983, 10, 7),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Vegard Solberg",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 502099111,
            PersonIdentifier = "07905999111",
            RefId = "07905999111",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100012,
            Username = "vegard.solberg",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> IngerNygard { get; } = new("10000013-aaaa-4bbb-8ccc-ddddeeee0013")
    {
        Entity = new()
        {
            DateOfBirth = new(1980, 3, 19),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Inger Nygard",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50299121,
            PersonIdentifier = "03834199121",
            RefId = "03834199121",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100013,
            Username = "inger.nygard",
            VariantId = EntityVariantConstants.Person,
        }
    };

    #endregion

    #region Personer - MittRegnskap

    public static ConstantDefinition<Entity> AstridJohansen { get; } = new("10000014-aaaa-4bbb-8ccc-ddddeeee0014")
    {
        Entity = new()
        {
            DateOfBirth = new(1976, 11, 25),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Astrid Johansen",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200014,
            PersonIdentifier = "25117600079",
            RefId = "25117600079",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100014,
            Username = "astrid.johansen",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> TrondLarsen { get; } = new("10000015-aaaa-4bbb-8ccc-ddddeeee0015")
    {
        Entity = new()
        {
            DateOfBirth = new(1969, 4, 2),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Trond Larsen",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200015,
            PersonIdentifier = "02046900076",
            RefId = "02046900076",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100015,
            Username = "trond.larsen",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> MaritEriksen { get; } = new("10000016-aaaa-4bbb-8ccc-ddddeeee0016")
    {
        Entity = new()
        {
            DateOfBirth = new(1987, 7, 16),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Marit Eriksen",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200016,
            PersonIdentifier = "16078700088",
            RefId = "16078700088",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100016,
            Username = "marit.eriksen",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> GeirPedersen { get; } = new("10000017-aaaa-4bbb-8ccc-ddddeeee0017")
    {
        Entity = new()
        {
            DateOfBirth = new(1974, 9, 11),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Geir Pedersen",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200017,
            PersonIdentifier = "11097400182",
            RefId = "11097400182",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100017,
            Username = "geir.pedersen",
            VariantId = EntityVariantConstants.Person,
        }
    };

    #endregion

    #region Personer - RPC AS

    public static ConstantDefinition<Entity> OddHalvorsen { get; } = new("10000018-aaaa-4bbb-8ccc-ddddeeee0018")
    {
        Entity = new()
        {
            DateOfBirth = new(1966, 2, 9),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Odd Halvorsen",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200018,
            PersonIdentifier = "09026600097",
            RefId = "09026600097",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100018,
            Username = "odd.halvorsen",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> LivKristiansen { get; } = new("10000019-aaaa-4bbb-8ccc-ddddeeee0019")
    {
        Entity = new()
        {
            DateOfBirth = new(1973, 12, 20),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Liv Kristiansen",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200019,
            PersonIdentifier = "20127300020",
            RefId = "20127300020",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100019,
            Username = "liv.kristiansen",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> SteinarAndreassen { get; } = new("10000020-aaaa-4bbb-8ccc-ddddeeee0020")
    {
        Entity = new()
        {
            DateOfBirth = new(1981, 6, 4),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Steinar Andreassen",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200020,
            PersonIdentifier = "04068100033",
            RefId = "04068100033",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100020,
            Username = "steinar.andreassen",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> HelgeNilsen { get; } = new("10000021-aaaa-4bbb-8ccc-ddddeeee0021")
    {
        Entity = new()
        {
            DateOfBirth = new(1977, 8, 27),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Helge Nilsen",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50200021,
            PersonIdentifier = "27087700130",
            RefId = "27087700130",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100021,
            Username = "helge.nilsen",
            VariantId = EntityVariantConstants.Person,
        }
    };

    #endregion

    #region Personer - Han Solo Enterprise
    public static ConstantDefinition<Entity> HanSolo { get; } = new("ffe29638-80d9-4f34-a8d8-a172a6f360c8")
    {
        Entity = new()
        {
            DateOfBirth = new(1976, 3, 10),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Han Solo",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50300001,
            PersonIdentifier = "10837612174",
            RefId = "10837612174",
            TypeId = EntityTypeConstants.Person,
            UserId = 20300001,
            Username = null,
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> BenSolo { get; } = new("1e984aef-7edc-4435-8dd6-cdf1ff2899f1")
    {
        Entity = new()
        {
            DateOfBirth = new(2010, 9, 28),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Ben Solo",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50300002,
            PersonIdentifier = "28091080186",
            RefId = "28091080186",
            TypeId = EntityTypeConstants.Person,
            UserId = 20300002,
            Username = null,
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> LeiaOrgana { get; } = new("60c29841-a339-43d6-8244-a5957a961000")
    {
        Entity = new()
        {
            DateOfBirth = new(1986, 10, 7),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Leia Organa",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50300003,
            PersonIdentifier = "07108640094",
            RefId = "07108640094",
            TypeId = EntityTypeConstants.Person,
            UserId = 20300003,
            Username = null,
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> LukeSkyWalker { get; } = new("758de848-89cd-4d92-b4e7-3697c22594e7")
    {
        Entity = new()
        {
            DateOfBirth = new(1989, 7, 21),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Luke Skywalker",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50300005,
            PersonIdentifier = "21078942743",
            RefId = "07108640094",
            TypeId = EntityTypeConstants.Person,
            UserId = 20300005,
            Username = null,
            VariantId = EntityVariantConstants.Person,
        }
    };
    #endregion

    #region Assignments

    // UUIDv7 format: 0196a0b1-xxxx-7xxx-8xxx-xxxxxxxxxxxx

    // Han Solo Enterprise - personroller
    private static readonly Guid AssignHanSoloEnterpriseHanSoloMD = Guid.Parse("019d9613-c654-7a01-8998-a46f9c983067");
    private static readonly Guid AssignHanSoloEnterpriseBenSoloRS = Guid.Parse("019dba1e-4b53-765b-b671-248cb31e3094");
    private static readonly Guid AssignHanSoloEnterpriseLukeSkywalkerRS = Guid.Parse("019dba4a-6a75-7852-b595-d1d026169101");
    private static readonly Guid AssignHanSoloEnterpriseLeiaOrganaCB = Guid.Parse("019dba3a-484e-70bd-b79a-231025c33c19");

    // Baker Johnsen - personroller
    private static readonly Guid AssignBakerJohnsenLarsBakkeMD = Guid.Parse("0196a0b1-0001-7001-8001-000000000001");
    private static readonly Guid AssignBakerJohnsenHildeStrandCB = Guid.Parse("0196a0b1-0001-7001-8001-000000000002");
    private static readonly Guid AssignBakerJohnsenKnutVikBM = Guid.Parse("0196a0b1-0001-7001-8001-000000000003");

    // Svendsen Automobil - personroller
    private static readonly Guid AssignSvendsenMortenDahlMD = Guid.Parse("0196a0b1-0001-7001-8001-000000000004");
    private static readonly Guid AssignSvendsenGreteHolmCB = Guid.Parse("0196a0b1-0001-7001-8001-000000000005");
    private static readonly Guid AssignSvendsenArneLundBM = Guid.Parse("0196a0b1-0001-7001-8001-000000000006");

    // Fredriksons Fabrikk - personroller
    private static readonly Guid AssignFredriksonSiljeHaugenMD = Guid.Parse("0196a0b1-0001-7001-8001-000000000007");
    private static readonly Guid AssignFredriksonEinarBergCB = Guid.Parse("0196a0b1-0001-7001-8001-000000000008");
    private static readonly Guid AssignFredriksonToneKvamBM = Guid.Parse("0196a0b1-0001-7001-8001-000000000009");

    // RegnskapNorge - personroller
    private static readonly Guid AssignRegnskapNorgeBjornMoeMD = Guid.Parse("0196a0b1-0001-7001-8001-00000000000a");
    private static readonly Guid AssignRegnskapNorgeRandiLieCB = Guid.Parse("0196a0b1-0001-7001-8001-00000000000b");
    private static readonly Guid AssignRegnskapNorgeVegardSolbergBM = Guid.Parse("0196a0b1-0001-7001-8001-00000000000c");
    private static readonly Guid AssignRegnskapNorgeIngerNygardAcc = Guid.Parse("0196a0b1-0001-7001-8001-00000000000d");

    // MittRegnskap - personroller
    private static readonly Guid AssignMittRegnskapAstridJohansenMD = Guid.Parse("0196a0b1-0001-7001-8001-00000000000e");
    private static readonly Guid AssignMittRegnskapTrondLarsenCB = Guid.Parse("0196a0b1-0001-7001-8001-00000000000f");
    private static readonly Guid AssignMittRegnskapMaritEriksenBM = Guid.Parse("0196a0b1-0001-7001-8001-000000000010");
    private static readonly Guid AssignMittRegnskapGeirPedersenAcc = Guid.Parse("0196a0b1-0001-7001-8001-000000000011");

    // RPC AS - personroller
    private static readonly Guid AssignRpcOddHalvorsenMD = Guid.Parse("0196a0b1-0001-7001-8001-000000000012");
    private static readonly Guid AssignRpcLivKristiansenCB = Guid.Parse("0196a0b1-0001-7001-8001-000000000013");
    private static readonly Guid AssignRpcSteinarAndreassenBM = Guid.Parse("0196a0b1-0001-7001-8001-000000000014");
    private static readonly Guid AssignRpcHelgeNilsenAud = Guid.Parse("0196a0b1-0001-7001-8001-000000000015");

    // Dumbo Adventures - personroller
    private static readonly Guid AssignDumboAdventuresMalinEmilieMD = Guid.Parse("0196a0b1-0001-7001-8001-000000000020");
    private static readonly Guid AssignDumboAdventuresThea = Guid.Parse("0196a0b1-0001-7001-8001-000000000021");

    // Mille Hundefrisør - personroller
    private static readonly Guid AssignMilleHundefrisorTheaMD = Guid.Parse("0196a0b1-0001-7001-8001-000000000030");
    private static readonly Guid AssignMilleHundefrisorMilenaCB = Guid.Parse("0196a0b1-0001-7001-8001-000000000031");

    // Kaos Magic Design and Arts - personroller
    private static readonly Guid AssignKaosJinxArcaneMD = Guid.Parse("0196a0b1-0001-7001-8001-000000000050");
    private static readonly Guid AssignKaosAlexTheArtistCB = Guid.Parse("0196a0b1-0001-7001-8001-000000000051");

    // Kaos Magic Design and Arts - rettighetshavere
    public static readonly Guid AssignKaosJosephineRightholder = Guid.Parse("0196a0b1-0001-7001-8001-000000000053");

    // Dumbo Adventures - org-til-org
    private static readonly Guid AssignDumboAdventuresKaosAuditor = Guid.Parse("0196a0b1-0001-7001-8001-000000000052");

    // Privatperson (self-to-self) assignments
    private static readonly Guid AssignMalinEmiliePriv = Guid.Parse("0196a0b1-0001-7001-8001-000000000040");
    private static readonly Guid AssignTheaPriv = Guid.Parse("0196a0b1-0001-7001-8001-000000000041");
    private static readonly Guid AssignJosephinePriv = Guid.Parse("0196a0b1-0001-7001-8001-000000000042");
    private static readonly Guid AssignMilenaPriv = Guid.Parse("0196a0b1-0001-7001-8001-000000000043");
    private static readonly Guid AssignJinxArcanePriv = Guid.Parse("0196a0b1-0001-7001-8001-000000000044");
    private static readonly Guid AssignAlexTheArtistPriv = Guid.Parse("0196a0b1-0001-7001-8001-000000000045");    
    private static readonly Guid AssignHanSoloPriv = Guid.Parse("019dba34-78cc-7b38-b91b-393fc2488c4a");
    private static readonly Guid AssignBenSoloPriv = Guid.Parse("019dba34-a7ce-7ec3-9cc7-beb6e3c7dbce");
    private static readonly Guid AssignLeiaOrganaPriv = Guid.Parse("019dba34-bcf3-7285-8ad8-7709b37cd47a");
    private static readonly Guid AssignLukeSkywalkerPriv = Guid.Parse("019dba4a-fa99-738e-aedd-5b324de37fc7");

    // Org-til-org assignments
    private static readonly Guid AssignBakerJohnsenRegnskapNorgeAcc = Guid.Parse("0196a0b1-0001-7001-8001-000000000016");
    private static readonly Guid AssignSvendsenMittRegnskapAcc = Guid.Parse("0196a0b1-0001-7001-8001-000000000017");
    private static readonly Guid AssignFredriksonRegnskapNorgeAcc = Guid.Parse("0196a0b1-0001-7001-8001-000000000018");
    private static readonly Guid AssignBakerJohnsenRpcAud = Guid.Parse("0196a0b1-0001-7001-8001-000000000019");
    private static readonly Guid AssignSvendsenRpcAud = Guid.Parse("0196a0b1-0001-7001-8001-00000000001a");
    private static readonly Guid AssignFredriksonRpcAud = Guid.Parse("0196a0b1-0001-7001-8001-00000000001b");
    private static readonly Guid AssignRegnskapNorgeRpcAud = Guid.Parse("0196a0b1-0001-7001-8001-00000000001c");
    private static readonly Guid AssignMittRegnskapRpcAud = Guid.Parse("0196a0b1-0001-7001-8001-00000000001d");

#pragma warning disable SA1401 // Fields should be private
    public static List<Assignment> Assignments = new()
#pragma warning restore SA1401 // Fields should be private
    {
        // Han Solo Enterprise - personroller
        new Assignment() { Id = AssignHanSoloEnterpriseHanSoloMD, FromId = HanSoloEnterprise.Id, ToId = HanSolo.Id, RoleId = RoleConstants.ManagingDirector.Id },
        new Assignment() { Id = AssignHanSoloEnterpriseBenSoloRS, FromId = HanSoloEnterprise.Id, ToId = BenSolo.Id, RoleId = RoleConstants.ReporterSender.Id },
        new Assignment() { Id = AssignHanSoloEnterpriseLukeSkywalkerRS, FromId = HanSoloEnterprise.Id, ToId = LukeSkyWalker.Id, RoleId = RoleConstants.ReporterSender.Id },
        new Assignment() { Id = AssignHanSoloEnterpriseLeiaOrganaCB, FromId = HanSoloEnterprise.Id, ToId = LeiaOrgana.Id, RoleId = RoleConstants.ChairOfTheBoard.Id },
            
        // Baker Johnsen - personroller
        new Assignment() { Id = AssignBakerJohnsenLarsBakkeMD, FromId = BakerJohnsen, ToId = LarsBakke, RoleId = RoleConstants.ManagingDirector },
        new Assignment() { Id = AssignBakerJohnsenHildeStrandCB, FromId = BakerJohnsen, ToId = HildeStrand, RoleId = RoleConstants.ChairOfTheBoard },
        new Assignment() { Id = AssignBakerJohnsenKnutVikBM, FromId = BakerJohnsen, ToId = KnutVik, RoleId = RoleConstants.BoardMember },

        // Svendsen Automobil - personroller
        new Assignment() { Id = AssignSvendsenMortenDahlMD, FromId = SvendsenAutomobil, ToId = MortenDahl, RoleId = RoleConstants.ManagingDirector },
        new Assignment() { Id = AssignSvendsenGreteHolmCB, FromId = SvendsenAutomobil, ToId = GreteHolm, RoleId = RoleConstants.ChairOfTheBoard },
        new Assignment() { Id = AssignSvendsenArneLundBM, FromId = SvendsenAutomobil, ToId = ArneLund, RoleId = RoleConstants.BoardMember },

        // Fredriksons Fabrikk - personroller
        new Assignment() { Id = AssignFredriksonSiljeHaugenMD, FromId = FredriksonsFabrikk, ToId = SiljeHaugen, RoleId = RoleConstants.ManagingDirector },
        new Assignment() { Id = AssignFredriksonEinarBergCB, FromId = FredriksonsFabrikk, ToId = EinarBerg, RoleId = RoleConstants.ChairOfTheBoard },
        new Assignment() { Id = AssignFredriksonToneKvamBM, FromId = FredriksonsFabrikk, ToId = ToneKvam, RoleId = RoleConstants.BoardMember },

        // RegnskapNorge - personroller
        new Assignment() { Id = AssignRegnskapNorgeBjornMoeMD, FromId = RegnskapNorge, ToId = BjornMoe, RoleId = RoleConstants.ManagingDirector },
        new Assignment() { Id = AssignRegnskapNorgeRandiLieCB, FromId = RegnskapNorge, ToId = RandiLie, RoleId = RoleConstants.ChairOfTheBoard },
        new Assignment() { Id = AssignRegnskapNorgeVegardSolbergBM, FromId = RegnskapNorge, ToId = VegardSolberg, RoleId = RoleConstants.BoardMember },
        new Assignment() { Id = AssignRegnskapNorgeIngerNygardAcc, FromId = RegnskapNorge, ToId = IngerNygard, RoleId = RoleConstants.Accountant },

        // MittRegnskap - personroller
        new Assignment() { Id = AssignMittRegnskapAstridJohansenMD, FromId = MittRegnskap, ToId = AstridJohansen, RoleId = RoleConstants.ManagingDirector },
        new Assignment() { Id = AssignMittRegnskapTrondLarsenCB, FromId = MittRegnskap, ToId = TrondLarsen, RoleId = RoleConstants.ChairOfTheBoard },
        new Assignment() { Id = AssignMittRegnskapMaritEriksenBM, FromId = MittRegnskap, ToId = MaritEriksen, RoleId = RoleConstants.BoardMember },
        new Assignment() { Id = AssignMittRegnskapGeirPedersenAcc, FromId = MittRegnskap, ToId = GeirPedersen, RoleId = RoleConstants.Accountant },

        // Dumbo Adventures - personroller
        new Assignment() { Id = AssignDumboAdventuresMalinEmilieMD, FromId = DumboAdventures, ToId = MalinEmilie, RoleId = RoleConstants.ManagingDirector },
        new Assignment() { Id = AssignDumboAdventuresThea, FromId = DumboAdventures, ToId = Thea, RoleId = RoleConstants.Rightholder },

        // Mille Hundefrisør - personroller
        new Assignment() { Id = AssignMilleHundefrisorTheaMD, FromId = MilleHundefrisor, ToId = Thea, RoleId = RoleConstants.ManagingDirector },
        new Assignment() { Id = AssignMilleHundefrisorMilenaCB, FromId = MilleHundefrisor, ToId = Milena, RoleId = RoleConstants.ChairOfTheBoard },

        // Kaos Magic Design and Arts - personroller
        new Assignment() { Id = AssignKaosJinxArcaneMD, FromId = KaosMagicDesignAndArts, ToId = JinxArcane, RoleId = RoleConstants.ManagingDirector },
        new Assignment() { Id = AssignKaosAlexTheArtistCB, FromId = KaosMagicDesignAndArts, ToId = AlexTheArtist, RoleId = RoleConstants.ChairOfTheBoard },

        // Kaos Magic Design and Arts - rettighetshavere
        new Assignment() { Id = AssignKaosJosephineRightholder, FromId = KaosMagicDesignAndArts, ToId = JosephineYvonnesdottir, RoleId = RoleConstants.Rightholder },

        // Dumbo Adventures - org-til-org (Kaos is auditor for Dumbo)
        new Assignment() { Id = AssignDumboAdventuresKaosAuditor, FromId = DumboAdventures, ToId = KaosMagicDesignAndArts, RoleId = RoleConstants.Auditor },

        // Privatperson (self-to-self)
        new Assignment() { Id = AssignMalinEmiliePriv, FromId = MalinEmilie, ToId = MalinEmilie, RoleId = RoleConstants.PrivatePerson },
        new Assignment() { Id = AssignTheaPriv, FromId = Thea, ToId = Thea, RoleId = RoleConstants.PrivatePerson },
        new Assignment() { Id = AssignJosephinePriv, FromId = JosephineYvonnesdottir, ToId = JosephineYvonnesdottir, RoleId = RoleConstants.PrivatePerson },
        new Assignment() { Id = AssignMilenaPriv, FromId = Milena, ToId = Milena, RoleId = RoleConstants.PrivatePerson },
        new Assignment() { Id = AssignJinxArcanePriv, FromId = JinxArcane, ToId = JinxArcane, RoleId = RoleConstants.PrivatePerson },
        new Assignment() { Id = AssignAlexTheArtistPriv, FromId = AlexTheArtist, ToId = AlexTheArtist, RoleId = RoleConstants.PrivatePerson },
        new Assignment() { Id = AssignHanSoloPriv, FromId = HanSolo, ToId = HanSolo, RoleId = RoleConstants.PrivatePerson },
        new Assignment() { Id = AssignBenSoloPriv, FromId = BenSolo, ToId = BenSolo, RoleId = RoleConstants.PrivatePerson },
        new Assignment() { Id = AssignLeiaOrganaPriv, FromId = LeiaOrgana, ToId = LeiaOrgana, RoleId = RoleConstants.PrivatePerson },
        new Assignment() { Id = AssignLukeSkywalkerPriv, FromId = LukeSkyWalker, ToId = LukeSkyWalker, RoleId = RoleConstants.PrivatePerson },
        
        // RPC AS - personroller
        new Assignment() { Id = AssignRpcOddHalvorsenMD, FromId = RpcAS, ToId = OddHalvorsen, RoleId = RoleConstants.ManagingDirector },
        new Assignment() { Id = AssignRpcLivKristiansenCB, FromId = RpcAS, ToId = LivKristiansen, RoleId = RoleConstants.ChairOfTheBoard },
        new Assignment() { Id = AssignRpcSteinarAndreassenBM, FromId = RpcAS, ToId = SteinarAndreassen, RoleId = RoleConstants.BoardMember },
        new Assignment() { Id = AssignRpcHelgeNilsenAud, FromId = RpcAS, ToId = HelgeNilsen, RoleId = RoleConstants.Auditor },

        // Org-til-org assignments (regnskapsfører og revisor)
        new Assignment() { Id = AssignBakerJohnsenRegnskapNorgeAcc, FromId = BakerJohnsen, ToId = RegnskapNorge, RoleId = RoleConstants.Accountant },
        new Assignment() { Id = AssignSvendsenMittRegnskapAcc, FromId = SvendsenAutomobil, ToId = MittRegnskap, RoleId = RoleConstants.Accountant },
        new Assignment() { Id = AssignFredriksonRegnskapNorgeAcc, FromId = FredriksonsFabrikk, ToId = RegnskapNorge, RoleId = RoleConstants.Accountant },
        new Assignment() { Id = AssignBakerJohnsenRpcAud, FromId = BakerJohnsen, ToId = RpcAS, RoleId = RoleConstants.Auditor },
        new Assignment() { Id = AssignSvendsenRpcAud, FromId = SvendsenAutomobil, ToId = RpcAS, RoleId = RoleConstants.Auditor },
        new Assignment() { Id = AssignFredriksonRpcAud, FromId = FredriksonsFabrikk, ToId = RpcAS, RoleId = RoleConstants.Auditor },
        new Assignment() { Id = AssignRegnskapNorgeRpcAud, FromId = RegnskapNorge, ToId = RpcAS, RoleId = RoleConstants.Auditor },
        new Assignment() { Id = AssignMittRegnskapRpcAud, FromId = MittRegnskap, ToId = RpcAS, RoleId = RoleConstants.Auditor },

    };

    #endregion

    #region Assignment Packages

#pragma warning disable SA1401 // Fields should be private
    public static List<AssignmentPackage> AssignmentPackages = new()
#pragma warning restore SA1401 // Fields should be private
    {
        // Dumbo Adventures - Thea has Rightholder with a package
        new AssignmentPackage()
        {
            AssignmentId = AssignDumboAdventuresThea,
            PackageId = PackageConstants.SalarySpecialCategory.Id
        },
    };

    #endregion

    #region Assignment Resources

#pragma warning disable SA1401 // Fields should be private
    public static List<AssignmentResource> AssignmentResources = new()
#pragma warning restore SA1401 // Fields should be private
    {
        // Josephine has instance rights for Kaos on SiriusSkattemelding
        new AssignmentResource()
        {
            AssignmentId = AssignKaosJosephineRightholder,
            ResourceId = SiriusSkattemelding.Id,
            PolicyPath = "sirius-skattemelding-v1/50315678/p5049963/delegationpolicy.xml",
            PolicyVersion = "1.0",
        },

        // Josephine has instance rights for Kaos on MattilsynetBakeryService
        new AssignmentResource()
        {
            AssignmentId = AssignKaosJosephineRightholder,
            ResourceId = MattilsynetBakeryService.Id,
            PolicyPath = "mattilsynet-baker-konditorvare/50315678/p5049963/delegationpolicy.xml",
            PolicyVersion = "1.0",
        },
    };

    #endregion

    #region Assignment Instances

#pragma warning disable SA1401 // Fields should be private
    public static List<AssignmentInstance> AssignmentInstances = new()
#pragma warning restore SA1401 // Fields should be private
    {
        // Josephine has instance rights for Kaos on SiriusSkattemelding
        new AssignmentInstance()
        {
            AssignmentId = AssignKaosJosephineRightholder,
            ResourceId = SiriusSkattemelding.Id,
            InstanceId = "urn:altinn:instance-id:50315678/b1a2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d",
            PolicyPath = "sirius-skattemelding-v1/50315678/p5049963/delegationpolicy.xml",
            PolicyVersion = "1.0",
        },

        // Josephine has instance rights for Kaos on MattilsynetBakeryService
        new AssignmentInstance()
        {
            AssignmentId = AssignKaosJosephineRightholder,
            ResourceId = MattilsynetBakeryService.Id,
            InstanceId = "urn:altinn:instance-id:50315678/a2b3c4d5-f6a7-4b8c-9d0e-1f2a3b4c5d6e",
            PolicyPath = "mattilsynet-baker-konditorvare/50315678/p5049963/delegationpolicy.xml",
            PolicyVersion = "1.0",
        },
    };

    #endregion

    #region Delegations

#pragma warning disable SA1401 // Fields should be private
    public static List<Delegation> Delegations = new()
#pragma warning restore SA1401 // Fields should be private
    {
        // RegnskapNorge delegerer sine klient-assignments til Inger Nygard
        new Delegation() { Id = Guid.Parse("0196a0b1-0002-7001-8001-000000000001"), FromId = AssignBakerJohnsenRegnskapNorgeAcc, ToId = AssignRegnskapNorgeIngerNygardAcc, FacilitatorId = RegnskapNorge },
        new Delegation() { Id = Guid.Parse("0196a0b1-0002-7001-8001-000000000002"), FromId = AssignFredriksonRegnskapNorgeAcc, ToId = AssignRegnskapNorgeIngerNygardAcc, FacilitatorId = RegnskapNorge },

        // MittRegnskap delegerer til Geir Pedersen
        new Delegation() { Id = Guid.Parse("0196a0b1-0002-7001-8001-000000000003"), FromId = AssignSvendsenMittRegnskapAcc, ToId = AssignMittRegnskapGeirPedersenAcc, FacilitatorId = MittRegnskap },

        // RPC AS delegerer revisor-assignments til Helge Nilsen
        new Delegation() { Id = Guid.Parse("0196a0b1-0002-7001-8001-000000000004"), FromId = AssignBakerJohnsenRpcAud, ToId = AssignRpcHelgeNilsenAud, FacilitatorId = RpcAS },
        new Delegation() { Id = Guid.Parse("0196a0b1-0002-7001-8001-000000000005"), FromId = AssignSvendsenRpcAud, ToId = AssignRpcHelgeNilsenAud, FacilitatorId = RpcAS },
        new Delegation() { Id = Guid.Parse("0196a0b1-0002-7001-8001-000000000006"), FromId = AssignFredriksonRpcAud, ToId = AssignRpcHelgeNilsenAud, FacilitatorId = RpcAS },
    };

    #endregion

    #region Helpers

    public static Assignment GetAssignment(Guid fromId, Guid toId, Guid roleId)
        => Assignments.First(a => a.FromId == fromId && a.ToId == toId && a.RoleId == roleId);

    #endregion
}
