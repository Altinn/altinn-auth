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

        /// <summary>
        /// Converts a <see cref="DelegationChangeType"/> to its external counterpart.
        /// The external enum mirrors the internal one member for member, and the wire contract is the
        /// numeric value, so the conversion is a value cast. Keep the two enums in sync when either changes.
        /// </summary>
        private static DelegationChangeTypeExternal ToExternal(DelegationChangeType core) => (DelegationChangeTypeExternal)core;

        /// <summary>
        /// Converts a <see cref="UuidType"/> to its external counterpart.
        /// The wire contract is the numeric value, so the conversion is a value cast. Note that
        /// <see cref="UuidType.Resource"/> and <see cref="UuidType.Party"/> have no counterpart in
        /// <see cref="UuidTypeExternal"/> and pass through as their numeric value, which is what the
        /// AutoMapper profile this replaces did.
        /// </summary>
        private static UuidTypeExternal ToExternal(UuidType core) => (UuidTypeExternal)core;
    }
}
