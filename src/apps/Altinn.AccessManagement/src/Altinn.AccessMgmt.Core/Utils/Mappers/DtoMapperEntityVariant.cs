using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Utils;

/// <inheritdoc/>
public partial class DtoMapperEntityVariant : IDtoMapper
{
    public static EntityVariantDto Convert(EntityVariant obj)
    {
        EntityTypeConstants.TryGetById(obj.TypeId, out var type);
        return new EntityVariantDto()
        {
            Id = obj.Id,
            Name = obj.Name,
            Description = obj.Description,
            TypeId = obj.TypeId,
            Type = Convert(obj.Type == null ? type.Entity : obj.Type),
        };
    }

    public static EntityTypeDto Convert(EntityType obj)
    {
        return new EntityTypeDto()
        {
            Id = obj.Id,
            Name = obj.Name,
            ProviderId = obj.ProviderId
        };
    }
}
