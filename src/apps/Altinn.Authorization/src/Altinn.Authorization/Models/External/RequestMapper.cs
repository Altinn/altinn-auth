using Altinn.Authorization.ABAC.Xacml.JsonProfile;

namespace Altinn.Platform.Authorization.Models.External
{
    /// <summary>
    /// A class that hold access managment mapping
    /// </summary>
    public class RequestMapper : AutoMapper.Profile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RequestMapper"/> class.
        /// </summary>
        public RequestMapper()
        {
            AllowNullCollections = true;
            CreateMap<XacmlJsonAttributeExternal, XacmlJsonAttribute>();

            CreateMap<XacmlJsonIdReferenceExternal, XacmlJsonIdReference>();
            CreateMap<XacmlJsonMultiRequestsExternal, XacmlJsonMultiRequests>();

            CreateMap<XacmlJsonPolicyIdentifierListExternal, XacmlJsonPolicyIdentifierList>();
            CreateMap<XacmlJsonRequestExternal, XacmlJsonRequest>();
            CreateMap<XacmlJsonRequestReferenceExternal, XacmlJsonRequestReference>();
            CreateMap<XacmlJsonRequestRootExternal, XacmlJsonRequestRoot>();
            CreateMap<XacmlJsonRequestExternal, XacmlJsonRequest>();
            CreateMap<XacmlJsonObligationOrAdviceExternal, XacmlJsonObligationOrAdvice>();
            CreateMap<XacmlJsonCategoryExternal, XacmlJsonCategory>();
            CreateMap<XacmlJsonStatusCodeExternal, XacmlJsonStatusCode>();
            CreateMap<XacmlJsonStatusExternal, XacmlJsonStatus>();

            CreateMap<XacmlJsonResponse, XacmlJsonResponseExternal>();
            CreateMap<XacmlJsonResult, XacmlJsonResultExternal>();

            CreateMap<XacmlJsonObligationOrAdvice, XacmlJsonObligationOrAdviceExternal>();
            CreateMap<XacmlJsonCategory, XacmlJsonCategoryExternal>();
            CreateMap<XacmlJsonStatus, XacmlJsonStatusExternal>();
            CreateMap<XacmlJsonStatusCode, XacmlJsonStatusCodeExternal>();
            CreateMap<XacmlJsonPolicyIdentifierList, XacmlJsonPolicyIdentifierListExternal>();
            CreateMap<XacmlJsonAttribute, XacmlJsonAttributeExternal>();
            CreateMap<XacmlJsonIdReference, XacmlJsonIdReferenceExternal>();
            CreateMap<XacmlJsonAttributeAssignment, XacmlJsonAttributeAssignmentExternal>();
        }
    }
}
