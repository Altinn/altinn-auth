using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.Core.Tests.Utils.Mappers;

/// <summary>
/// Pure-logic unit tests for <see cref="DtoMapperEntityVariant"/>.
/// No containers required.
/// </summary>
public class DtoMapperEntityVariantTest
{
    // Organization TypeId from EntityTypeConstants
    private static readonly Guid OrganizationTypeId = new("8c216e2f-afdd-4234-9ba2-691c727bb33d");

    // ── Convert(EntityVariant) — explicit Type ────────────────────────────────

    [Fact]
    public void Convert_EntityVariant_WithExplicitType_MapsAllFields()
    {
        var typeId = OrganizationTypeId;
        var variant = new EntityVariant
        {
            Id = Guid.NewGuid(),
            TypeId = typeId,
            Name = "Enkeltpersonforetak",
            Description = "Sole proprietorship",
            Type = new EntityType { Id = typeId, Name = "Organisasjon", ProviderId = Guid.NewGuid() },
        };

        var dto = DtoMapperEntityVariant.Convert(variant);

        dto.Id.Should().Be(variant.Id);
        dto.Name.Should().Be("Enkeltpersonforetak");
        dto.Description.Should().Be("Sole proprietorship");
        dto.TypeId.Should().Be(typeId);
        dto.Type.Should().NotBeNull();
        dto.Type.Name.Should().Be("Organisasjon");
    }

    [Fact]
    public void Convert_EntityVariant_WithoutExplicitType_FallsBackToEntityTypeConstants()
    {
        // TypeId matches EntityTypeConstants.Organization — TryGetById will succeed
        var variant = new EntityVariant
        {
            Id = Guid.NewGuid(),
            TypeId = OrganizationTypeId,
            Name = "AS",
            Description = "Aksjeselskap",
            Type = null,
        };

        var dto = DtoMapperEntityVariant.Convert(variant);

        dto.Id.Should().Be(variant.Id);
        dto.TypeId.Should().Be(OrganizationTypeId);
        // Type must be populated via EntityTypeConstants fallback
        dto.Type.Should().NotBeNull();
        dto.Type.Name.Should().Be("Organisasjon");
    }

    // ── Convert(EntityType) ───────────────────────────────────────────────────

    [Fact]
    public void Convert_EntityType_MapsIdNameAndProviderId()
    {
        var providerId = Guid.NewGuid();
        var entityType = new EntityType
        {
            Id = OrganizationTypeId,
            Name = "Organisasjon",
            ProviderId = providerId,
        };

        var dto = DtoMapperEntityVariant.Convert(entityType);

        dto.Id.Should().Be(OrganizationTypeId);
        dto.Name.Should().Be("Organisasjon");
        dto.ProviderId.Should().Be(providerId);
    }
}
