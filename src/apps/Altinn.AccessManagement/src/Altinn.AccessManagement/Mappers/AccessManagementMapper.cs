using Altinn.AccessManagement.Core.Enums;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Models.ResourceRegistry;
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
            CreateMap<BaseAttribute, AttributeDto>();
            CreateMap<AttributeDto, BaseAttribute>();

            // Rights
            CreateMap<AuthorizedParty, AuthorizedPartyDto>();
            CreateMap<AuthorizedParty.AuthorizedResourceInstance, AuthorizedPartyDto.AuthorizedResourceInstance>();
            CreateMap<AuthorizedPartyType, AuthorizedPartyTypeDto>();
        }

        private static bool IsGuid(string value) => Guid.TryParse(value, out _);
    }
}
