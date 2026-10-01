using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Enums;
using Altinn.Authorization.Api.Contracts.Authorization;

namespace Altinn.AccessManagement.Api.Internal.Extensions
{
    /// <summary>
    /// Provides extension methods for transforming <see cref="DelegationChange"/> to <see cref="DelegationChangeDto"/>.
    /// </summary>
    public static class DelegationChangeExtensions
    {
        /// <summary>
        /// Converts a <see cref="DelegationChange"/> object to a <see cref="DelegationChangeDto"/> object.
        /// </summary>
        /// <param name="core">The <see cref="DelegationChange"/> object to convert.</param>
        /// <returns>A <see cref="DelegationChangeDto"/> object representing the converted data.</returns>
        public static DelegationChangeDto ToDelegationChangeExternal(this DelegationChange core)
        {
            return new DelegationChangeDto
            {
                DelegationChangeId = core.DelegationChangeId,
                ResourceRegistryDelegationChangeId = core.ResourceRegistryDelegationChangeId,
                DelegationChangeType = ToExternal(core.DelegationChangeType),
                ResourceId = core.ResourceId,
                ResourceType = core.ResourceType,
                InstanceId = core.InstanceId,
                OfferedByPartyId = core.OfferedByPartyId,
                FromUuid = core.FromUuid,
                FromUuidType = ToExternal(core.FromUuidType),
                CoveredByPartyId = core.CoveredByPartyId,
                CoveredByUserId = core.CoveredByUserId,
                ToUuid = core.ToUuid,
                ToUuidType = ToExternal(core.ToUuidType),
                PerformedByUserId = core.PerformedByUserId,
                PerformedByPartyId = core.PerformedByPartyId,
                PerformedByUuid = core.PerformedByUuid,
                PerformedByUuidType = ToExternal(core.PerformedByUuidType),
                BlobStoragePolicyPath = core.BlobStoragePolicyPath,
                BlobStorageVersionId = core.BlobStorageVersionId,
                Created = core.Created
            };
        }

        private static DelegationChangeTypeDto ToExternal(DelegationChangeType core) => core switch
        {
            DelegationChangeType.Undefined => DelegationChangeTypeDto.Undefined,
            DelegationChangeType.Grant => DelegationChangeTypeDto.Grant,
            DelegationChangeType.Revoke => DelegationChangeTypeDto.Revoke,
            DelegationChangeType.RevokeLast => DelegationChangeTypeDto.RevokeLast,
            _ => throw new ArgumentOutOfRangeException(nameof(core), core, null),
        };

        private static UuidTypeDto ToExternal(UuidType core) => core switch
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
