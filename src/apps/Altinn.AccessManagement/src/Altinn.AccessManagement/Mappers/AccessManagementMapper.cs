using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
using Altinn.AccessManagement.Enums;
using Altinn.AccessManagement.Models;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;
using Altinn.Platform.Register.Models;

namespace Altinn.AccessManagement.Mappers
{
    /// <summary>
    /// A class that holds the access management mapper configuration
    /// </summary>
    public class AccessManagementMapper : AutoMapper.Profile
    {
        /// <summary>
        /// Configuration for accessmanagement mapper
        /// </summary>
        public AccessManagementMapper()
        {
            AllowNullCollections = true;
            CreateMap<Party, PartyExternal>();
            CreateMap<Delegation, DelegationExternal>();
            CreateMap<CompetentAuthority, CompetentAuthorityExternal>()
                .ForMember(dest => dest.Orgcode, act => act.MapFrom(src => src.Orgcode))
                .ForMember(dest => dest.Organization, act => act.MapFrom(src => src.Organization))
                .ForMember(dest => dest.Name, act => act.MapFrom(src => src.Name));
            CreateMap<ResourceReference, ResourceReferenceExternal>()
                .ForMember(dest => dest.ReferenceType, act => act.MapFrom(src => src.ReferenceType))
                .ForMember(dest => dest.ReferenceSource, act => act.MapFrom(src => src.ReferenceSource))
                .ForMember(dest => dest.Reference, act => act.MapFrom(src => src.Reference));
            CreateMap<AttributeMatch, AttributeMatchExternal>();
            CreateMap<AttributeMatchExternal, AttributeMatch>();
            CreateMap<BaseAttribute, AttributeDto>();
            CreateMap<AttributeDto, BaseAttribute>();
            CreateMap<PolicyAttributeMatch, PolicyAttributeMatchExternal>();
            CreateMap<PolicyAttributeMatchExternal, PolicyAttributeMatch>();

            // Rights
            CreateMap<Detail, DetailExternal>();

            // Delegation
            CreateMap<DelegationChange, DelegationChangeExternal>();
            CreateMap<DelegationChangeType, DelegationChangeTypeExternal>();

            CreateMap<AuthorizedParty, AuthorizedPartyDto>();
            CreateMap<AuthorizedParty.AuthorizedResourceInstance, AuthorizedPartyDto.AuthorizedResourceInstance>();
            CreateMap<AuthorizedPartyType, AuthorizedPartyTypeDto>();
            CreateMap<AppsInstanceDelegationRequestDto, AppsInstanceDelegationRequest>()
                .ForMember(dest => dest.From, act => act.MapFrom(src => src.From.Value))
                .ForMember(dest => dest.To, act => act.MapFrom(src => src.To.Value));
            CreateMap<Models.RightDto, RightInternal>()
                .ForMember(dest => dest.Action, act => act.MapFrom(src => src.Action.Value));
            CreateMap<AppsInstanceDelegationResponse, AppsInstanceDelegationResponseDto>();
            CreateMap<InstanceRightDelegationResult, RightDelegationResultDto>();
            CreateMap<InstanceDelegationModeExternal, InstanceDelegationMode>();
            CreateMap<AppsInstanceRevokeResponse, AppsInstanceRevokeResponseDto>();
            CreateMap<InstanceRightRevokeResult, RightRevokeResultDto>();
            CreateMap<ResourceRightDelegationCheckResult, ResourceRightDelegationCheckResultDto>();
        }

        private static bool IsGuid(string value) => Guid.TryParse(value, out _);
    }
}
