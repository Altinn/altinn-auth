using Altinn.AccessManagement.Api.Internal.Enums;
using Altinn.AccessManagement.Api.Internal.Models;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Enums;

namespace Altinn.AccessManagement.Api.Internal.Extensions
{
    /// <summary>
    /// Provides extension methods for transforming <see cref="DelegationChange"/> to <see cref="DelegationChangeExternal"/>.
    /// </summary>
    public static class DelegationChangeExtensions
    {
        /// <summary>
        /// Converts a <see cref="DelegationChange"/> object to a <see cref="DelegationChangeExternal"/> object.
        /// </summary>
        /// <param name="core">The <see cref="DelegationChange"/> object to convert.</param>
        /// <returns>A <see cref="DelegationChangeExternal"/> object representing the converted data.</returns>
        public static DelegationChangeExternal ToDelegationChangeExternal(this DelegationChange core)
        {
            return new DelegationChangeExternal
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

        private static DelegationChangeTypeExternal ToExternal(DelegationChangeType core) => core switch
        {
            DelegationChangeType.Undefined => DelegationChangeTypeExternal.Undefined,
            DelegationChangeType.Grant => DelegationChangeTypeExternal.Grant,
            DelegationChangeType.Revoke => DelegationChangeTypeExternal.Revoke,
            DelegationChangeType.RevokeLast => DelegationChangeTypeExternal.RevokeLast,
            _ => throw new ArgumentOutOfRangeException(nameof(core), core, null),
        };

        private static UuidTypeExternal ToExternal(UuidType core) => core switch
        {
            UuidType.NotSpecified => UuidTypeExternal.NotSpecified,
            UuidType.Person => UuidTypeExternal.Person,
            UuidType.Organization => UuidTypeExternal.Organization,
            UuidType.SystemUser => UuidTypeExternal.SystemUser,
            UuidType.EnterpriseUser => UuidTypeExternal.EnterpriseUser,
            UuidType.Resource => UuidTypeExternal.Resource,
            UuidType.Party => UuidTypeExternal.Party,
            _ => throw new ArgumentOutOfRangeException(nameof(core), core, null),
        };
    }
}
