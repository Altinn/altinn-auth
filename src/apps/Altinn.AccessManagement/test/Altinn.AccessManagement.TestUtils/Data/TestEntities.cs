using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;

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
    public static ConstantDefinition<Entity> OrganizationNordisAS { get; } = new("17edfbcd-34e0-4ce4-9f90-eff3747c1234")
    {
        Entity = new()
        {
            DateOfBirth = null,
            DateOfDeath = null,
            DeletedAt = null,
            IsDeleted = false,
            Name = "Nordis AS",
            OrganizationIdentifier = "910411578",
            Parent = null,
            ParentId = null,
            PartyId = 50068510,
            PersonIdentifier = null,
            RefId = "910411578",
            TypeId = EntityTypeConstants.Organisation,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
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
            TypeId = EntityTypeConstants.Organisation,
            UserId = null,
            Username = null,
            VariantId = EntityVariantConstants.AS,
        }
    };

    #endregion
}
