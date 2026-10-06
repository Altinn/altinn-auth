using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Enums;
using Altinn.Authorization.Api.Contracts.Authorization;

namespace Altinn.AccessManagement.Api.Internal.Extensions
{
    /// <summary>
    /// Provides extension methods for mapping delegation change models between core and DTO.
    /// </summary>
    public static class DelegationChangeExtensions
    {
        /// <summary>
        /// Converts a <see cref="DelegationChange"/> object to a <see cref="DelegationChangeDto"/> object.
        /// </summary>
        /// <param name="core">The <see cref="DelegationChange"/> object to convert.</param>
        /// <returns>A <see cref="DelegationChangeDto"/> object representing the converted data.</returns>
        public static DelegationChangeDto ToDelegationChangeDto(this DelegationChange core)
        {
            return new DelegationChangeDto
            {
                DelegationChangeId = core.DelegationChangeId,
                ResourceRegistryDelegationChangeId = core.ResourceRegistryDelegationChangeId,
                DelegationChangeType = ToDto(core.DelegationChangeType),
                ResourceId = core.ResourceId,
                ResourceType = core.ResourceType,
                InstanceId = core.InstanceId,
                OfferedByPartyId = core.OfferedByPartyId,
                FromUuid = core.FromUuid,
                FromUuidType = ToDto(core.FromUuidType),
                CoveredByPartyId = core.CoveredByPartyId,
                CoveredByUserId = core.CoveredByUserId,
                ToUuid = core.ToUuid,
                ToUuidType = ToDto(core.ToUuidType),
                PerformedByUserId = core.PerformedByUserId,
                PerformedByPartyId = core.PerformedByPartyId,
                PerformedByUuid = core.PerformedByUuid,
                PerformedByUuidType = ToDto(core.PerformedByUuidType),
                BlobStoragePolicyPath = core.BlobStoragePolicyPath,
                BlobStorageVersionId = core.BlobStorageVersionId,
                Created = core.Created
            };
        }

        /// <summary>
        /// Converts a <see cref="DelegationChangeInputDto"/> object to a <see cref="DelegationChangeInput"/> object.
        /// </summary>
        /// <param name="dto">The <see cref="DelegationChangeInputDto"/> object to convert.</param>
        /// <returns>A <see cref="DelegationChangeInput"/> object representing the converted data.</returns>
        public static DelegationChangeInput ToDelegationChangeInput(this DelegationChangeInputDto dto)
        {
            return new DelegationChangeInput
            {
                Subject = ToCore(dto.Subject),
                Party = ToCore(dto.Party),
                Resource = dto.Resource?.Select(ToCore).ToList(),
            };
        }

        private static AttributeMatch ToCore(AttributeMatchDto dto) =>
            dto is null ? null : new AttributeMatch { Id = dto.Id, Value = dto.Value };

        private static DelegationChangeTypeDto ToDto(DelegationChangeType core) => core switch
        {
            DelegationChangeType.Undefined => DelegationChangeTypeDto.Undefined,
            DelegationChangeType.Grant => DelegationChangeTypeDto.Grant,
            DelegationChangeType.Revoke => DelegationChangeTypeDto.Revoke,
            DelegationChangeType.RevokeLast => DelegationChangeTypeDto.RevokeLast,
            _ => throw new ArgumentOutOfRangeException(nameof(core), core, null),
        };

        private static UuidTypeDto ToDto(UuidType core) => core switch
        {
            UuidType.NotSpecified => UuidTypeDto.NotSpecified,
            UuidType.Person => UuidTypeDto.Person,
            UuidType.Organization => UuidTypeDto.Organization,
            UuidType.SystemUser => UuidTypeDto.SystemUser,
            UuidType.EnterpriseUser => UuidTypeDto.EnterpriseUser,
            UuidType.Resource => UuidTypeDto.Resource,
            UuidType.Party => UuidTypeDto.Party,
            _ => throw new ArgumentOutOfRangeException(nameof(core), core, null),
        };
    }
}
