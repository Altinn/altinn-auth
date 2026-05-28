using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Azure.Storage.Blobs.Models;
using Moq;

namespace Altinn.AccessManagement.TestUtils.Data;

public static class TestEntities
{
    #region Persons
    public static ConstantDefinition<Entity> PersonPaula { get; } = new("3f36376b-a013-442d-a84c-3d98c3ffb2a7")
    {
        Entity = new()
        {
            DateOfBirth = new(1990, 1, 1),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Paula Rimstad",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50002203,
            PersonIdentifier = "02056260016",
            RefId = "02056260016",
            TypeId = EntityTypeConstants.Person,
            UserId = 20000095,
            Username = "paula.person",
            VariantId = EntityVariantConstants.Person,
        }
    };

    public static ConstantDefinition<Entity> PersonOrjan { get; } = new("4a1b2c3d-5678-90ab-cdef-1234567890ab")
    {
        Entity = new()
        {
            DateOfBirth = new(1985, 5, 5),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Ørjan Ravnås",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50003899,
            PersonIdentifier = "05058567890",
            RefId = "05058567890",
            TypeId = EntityTypeConstants.Person,
            UserId = 20001337,
            Username = "orjan.olsen",
            VariantId = EntityVariantConstants.Person,
        }
    };

    #endregion

    #region Organizations

    public static ConstantDefinition<Entity> MainUnitNordis { get; } = new("8b8ab597-f0e6-47d8-8f3c-eaefd71c7049")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Main Unit Nordis",
            OrganizationIdentifier = "910448471",
            Parent = null,
            ParentId = null,
            PartyId = 50069468,
            PersonIdentifier = null,
            RefId = "910448471",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.BEDR,
        }
    };

    public static ConstantDefinition<Entity> OrganizationNordisAS { get; } = new("17edfbcd-34e0-4ce4-9f90-eff3747c1234")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Subunit Nordis AS",
            OrganizationIdentifier = "910411578",
            Parent = null,
            ParentId = MainUnitNordis,
            PartyId = 50068510,
            PersonIdentifier = null,
            RefId = "910411578",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
        }
    };

    public static ConstantDefinition<Entity> OrganizationOkernBorettslag { get; } = new("508F126C-958C-41D8-9436-E4CEE94660B1")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Økern Borettslag BRL",
            OrganizationIdentifier = "815493000",
            Parent = null,
            ParentId = null,
            PartyId = 50068511,
            PersonIdentifier = null,
            RefId = "815493000",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.BRL,
        }
    };

    public static ConstantDefinition<Entity> OrganizationVerdiqAS { get; } = new("77362379-847f-412e-8937-de6172188020")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Verdiq AS",
            OrganizationIdentifier = "910397087",
            Parent = null,
            ParentId = null,
            PartyId = 50068535,
            PersonIdentifier = null,
            RefId = "910397087",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
        }
    };

    public static ConstantDefinition<Entity> MainUnitKarlstad { get; } = new("a1b2c3d4-0001-0001-0001-000000000001")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "KARLSTAD OG ULØYBUKT REGNSKAP",
            OrganizationIdentifier = "810418672",
            Parent = null,
            ParentId = null,
            PartyId = 50004222,
            PersonIdentifier = null,
            RefId = "810418672",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
        }
    };

    public static ConstantDefinition<Entity> SubunitKarlstad { get; } = new("a1b2c3d4-0001-0001-0001-000000000002")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "KARLSTAD OG ULØYBUKT REGNSKAP SUBUNIT",
            OrganizationIdentifier = "810418532",
            Parent = null,
            ParentId = MainUnitKarlstad,
            PartyId = 50004221,
            PersonIdentifier = null,
            RefId = "810418532",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.BEDR,
        }
    };

    public static ConstantDefinition<Entity> OrganizationOrsta { get; } = new("a1b2c3d4-0001-0001-0001-000000000003")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "ØRSTA ACCOUNTING",
            OrganizationIdentifier = "910459880",
            Parent = null,
            ParentId = null,
            PartyId = 50005545,
            PersonIdentifier = null,
            RefId = "910459880",
            TypeId = EntityTypeConstants.Organization,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
        }
    };

    public static ConstantDefinition<Entity> PersonKasper { get; } = new("a1b2c3d4-0001-0001-0001-000000000004")
    {
        Entity = new()
        {
            DateOfBirth = new(1949, 12, 7),
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "KASPER BØRSTAD",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = 50002598,
            PersonIdentifier = "07124912037",
            RefId = "07124912037",
            TypeId = EntityTypeConstants.Person,
            UserId = 20000490,
            Username = null,
            VariantId = EntityVariantConstants.Person,
        }
    };

    #endregion

    #region Systemusers
    public static ConstantDefinition<Entity> SystemUserStandard { get; } = new("2cacc11b-6960-413f-9894-c330f99ed7e4")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Standard",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = null,
            PersonIdentifier = null,
            RefId = "2cacc11b-6960-413f-9894-c330f99ed7e4",
            TypeId = EntityTypeConstants.SystemUser,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.StandardSystem,
        }
    };

    public static ConstantDefinition<Entity> SystemUserClient { get; } = new("421667ea-8a95-4242-a5a4-572874a9035c")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Client",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = null,
            PersonIdentifier = null,
            RefId = "421667ea-8a95-4242-a5a4-572874a9035c",
            TypeId = EntityTypeConstants.SystemUser,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AgentSystem,
        }
    };

    #endregion

    #region Self Identified Users

    public static ConstantDefinition<Entity> SIUserMarius { get; } = new("892106fe-337e-4121-a61b-e937552b4280")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Marius",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = null,
            EmailIdentifier = "marius@gmail.com",
            PersonIdentifier = null,
            RefId = "marius@gmail.com",
            TypeId = EntityTypeConstants.SelfIdentified,
            UserId = null,
            Username = "marius@gmail.com",
            VariantId = EntityVariantConstants.SI,
        }
    };

    public static ConstantDefinition<Entity> EmailUserMarius { get; } = new("f43198c9-81ec-4e62-9913-4fef2b69e7d5")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Marius",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = null,
            EmailIdentifier = "marius@gmail.com",
            PersonIdentifier = null,
            RefId = "epost:marius@gmail.com",
            TypeId = EntityTypeConstants.SelfIdentified,
            UserId = null,
            Username = "epost:marius@gmail.com",
            VariantId = EntityVariantConstants.SI_EMAIL,
        }
    };

    public static ConstantDefinition<Entity> EmailUserHarryPotter { get; } = new("3b3578fb-6ba0-4320-8f24-09895a9921ac")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "harry.potter@hogwarts.com",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = null,
            EmailIdentifier = null,
            PersonIdentifier = null,
            RefId = null,
            TypeId = EntityTypeConstants.SelfIdentified,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.SI_EMAIL,
        }
    };

    public static ConstantDefinition<Entity> EduUserHermioneGranger { get; } = new("ac81168a-7b22-4037-acba-5dea31d3a512")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "hermione.granger@hogwarts.com",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = null,
            EmailIdentifier = null,
            PersonIdentifier = null,
            RefId = null,
            TypeId = EntityTypeConstants.SelfIdentified,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.SI_EDU,
        }
    };

    public static ConstantDefinition<Entity> UserRonWeasley { get; } = new("ee421389-a54a-4772-ae8e-784d6d43f599")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "ron.weasley@hogwarts.com",
            OrganizationIdentifier = null,
            Parent = null,
            ParentId = null,
            PartyId = null,
            EmailIdentifier = null,
            PersonIdentifier = null,
            RefId = null,
            TypeId = EntityTypeConstants.SelfIdentified,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.SI,
        }
    };

    #endregion
}
