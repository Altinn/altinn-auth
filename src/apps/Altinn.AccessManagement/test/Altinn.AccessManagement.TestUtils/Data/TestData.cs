using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessManagement.TestUtils.Data;

public static class TestData
{
    #region Firmaer

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
            OrganizationIdentifier = "310000004",
            Parent = null,
            ParentId = null,
            PartyId = 50100004,
            PersonIdentifier = null,
            RefId = "310000004",
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
            OrganizationIdentifier = "310000005",
            Parent = null,
            ParentId = null,
            PartyId = 50100005,
            PersonIdentifier = null,
            RefId = "310000005",
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
            OrganizationIdentifier = "310000006",
            Parent = null,
            ParentId = null,
            PartyId = 50100006,
            PersonIdentifier = null,
            RefId = "310000006",
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
            PersonIdentifier = "12037500001",
            RefId = "12037500001",
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
            PersonIdentifier = "22076800002",
            RefId = "22076800002",
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
            PersonIdentifier = "05118200003",
            RefId = "05118200003",
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
            PersonIdentifier = "18027900004",
            RefId = "18027900004",
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
            PersonIdentifier = "30097000005",
            RefId = "30097000005",
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
            PersonIdentifier = "14048500006",
            RefId = "14048500006",
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
            PersonIdentifier = "15036000023",
            RefId = "15036000023",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100023,
            Username = "bodil.farmor",
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
            PersonIdentifier = "21068800007",
            RefId = "21068800007",
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
            PersonIdentifier = "08016500008",
            RefId = "08016500008",
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
            PersonIdentifier = "03129100009",
            RefId = "03129100009",
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
            PersonIdentifier = "15087200010",
            RefId = "15087200010",
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
            PersonIdentifier = "28056700011",
            RefId = "28056700011",
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
            PartyId = 50200012,
            PersonIdentifier = "07108300012",
            RefId = "07108300012",
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
            PartyId = 50200013,
            PersonIdentifier = "19038000013",
            RefId = "19038000013",
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
            PersonIdentifier = "25117600014",
            RefId = "25117600014",
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
            PersonIdentifier = "02046900015",
            RefId = "02046900015",
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
            PersonIdentifier = "16078700016",
            RefId = "16078700016",
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
            PersonIdentifier = "11097400017",
            RefId = "11097400017",
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
            PersonIdentifier = "09026600018",
            RefId = "09026600018",
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
            PersonIdentifier = "20127300019",
            RefId = "20127300019",
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
            PersonIdentifier = "04068100020",
            RefId = "04068100020",
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
            PersonIdentifier = "27087700021",
            RefId = "27087700021",
            TypeId = EntityTypeConstants.Person,
            UserId = 20100021,
            Username = "helge.nilsen",
            VariantId = EntityVariantConstants.Person,
        }
    };

    #endregion

    #region Assignments

    // UUIDv7 format: 0196a0b1-xxxx-7xxx-8xxx-xxxxxxxxxxxx

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
