using Altinn.Authorization.ABAC.Xacml.JsonProfile;
using Altinn.Platform.Authorization.Models.External;

namespace Altinn.Authorization.Tests.Unit;

[UnitTest]
public class RequestMapperTest
{
    [Fact]
    public void ToInternal_FullyPopulatedRequest_ReturnsRequestWithAllFieldsMapped()
    {
        XacmlJsonRequestRootExternal external = new()
        {
            Request = new XacmlJsonRequestExternal
            {
                ReturnPolicyIdList = true,
                CombinedDecision = true,
                XPathVersion = "http://www.w3.org/TR/1999/REC-xpath-19991116",
                Category = [CreateExternalCategory("c1")],
                Resource = [CreateExternalCategory("r1"), CreateExternalCategory("r2")],
                Action = [CreateExternalCategory("a1")],
                AccessSubject = [CreateExternalCategory("s1")],
                RecipientSubject = [CreateExternalCategory("rs1")],
                IntermediarySubject = [CreateExternalCategory("is1")],
                RequestingMachine = [CreateExternalCategory("rm1")],
                MultiRequests = new XacmlJsonMultiRequestsExternal
                {
                    RequestReference =
                    [
                        new XacmlJsonRequestReferenceExternal { ReferenceId = ["r1", "s1", "a1"] },
                        new XacmlJsonRequestReferenceExternal { ReferenceId = ["r2", "s1", "a1"] },
                    ],
                },
            },
        };

        XacmlJsonRequestRoot result = external.ToInternal();

        // The internal and external models share member names, so structural
        // equivalence against the external source verifies every mapped member.
        result.Request.Should().BeEquivalentTo(external.Request);
    }

    [Fact]
    public void ToInternal_NullCollectionsAndMultiRequests_ReturnsRequestWithNullMembers()
    {
        XacmlJsonRequestRootExternal external = new()
        {
            Request = new XacmlJsonRequestExternal
            {
                XPathVersion = null,
                Category = null,
                Resource = null,
                Action = null,
                AccessSubject = null,
                RecipientSubject = null,
                IntermediarySubject = null,
                RequestingMachine = null,
                MultiRequests = null,
            },
        };

        XacmlJsonRequestRoot result = external.ToInternal();

        result.Request.XPathVersion.Should().BeNull();
        result.Request.Category.Should().BeNull();
        result.Request.Resource.Should().BeNull();
        result.Request.Action.Should().BeNull();
        result.Request.AccessSubject.Should().BeNull();
        result.Request.RecipientSubject.Should().BeNull();
        result.Request.IntermediarySubject.Should().BeNull();
        result.Request.RequestingMachine.Should().BeNull();
        result.Request.MultiRequests.Should().BeNull();
    }

    [Fact]
    public void ToInternal_AnyRequest_LeavesXForwardedForHeaderNull()
    {
        XacmlJsonRequestRootExternal external = new()
        {
            Request = new XacmlJsonRequestExternal
            {
                Resource = [CreateExternalCategory("r1")],
            },
        };

        XacmlJsonRequestRoot result = external.ToInternal();

        result.Request.XForwardedForHeader.Should().BeNull();
    }

    [Fact]
    public void ToExternal_FullyPopulatedResponse_ReturnsResponseWithAllFieldsMapped()
    {
        XacmlJsonResponse response = new()
        {
            Response =
            [
                new XacmlJsonResult
                {
                    Decision = "Permit",
                    Status = new XacmlJsonStatus
                    {
                        StatusMessage = "ok",
                        StatusDetails = ["detail1", "detail2"],
                        StatusCode = new XacmlJsonStatusCode
                        {
                            Value = "urn:oasis:names:tc:xacml:1.0:status:ok",
                            StatusCode = new XacmlJsonStatusCode
                            {
                                Value = "urn:oasis:names:tc:xacml:1.0:status:nested",
                            },
                        },
                    },
                    Obligations =
                    [
                        new XacmlJsonObligationOrAdvice
                        {
                            Id = "urn:altinn:obligation:authenticationLevel1",
                            AttributeAssignment =
                            [
                                new XacmlJsonAttributeAssignment
                                {
                                    AttributeId = "urn:altinn:obligation1-assignment1",
                                    Value = "2",
                                    Category = "urn:altinn:minimum-authenticationlevel",
                                    DataType = "http://www.w3.org/2001/XMLSchema#integer",
                                    Issuer = "altinn",
                                },
                            ],
                        },
                    ],
                    AssociateAdvice =
                    [
                        new XacmlJsonObligationOrAdvice { Id = "urn:altinn:advice1" },
                    ],
                    Category =
                    [
                        new XacmlJsonCategory
                        {
                            CategoryId = "urn:oasis:names:tc:xacml:1.0:subject-category:access-subject",
                            Id = "s1",
                            Content = "<content />",
                            Attribute =
                            [
                                new XacmlJsonAttribute
                                {
                                    AttributeId = "urn:altinn:userid",
                                    Value = "1337",
                                    Issuer = "altinn",
                                    DataType = "http://www.w3.org/2001/XMLSchema#string",
                                    IncludeInResult = true,
                                },
                            ],
                        },
                    ],
                    PolicyIdentifierList = new XacmlJsonPolicyIdentifierList
                    {
                        PolicyIdReference = [new XacmlJsonIdReference { Id = "policy1", Version = "1.0" }],
                        PolicySetIdReference = [new XacmlJsonIdReference { Id = "policyset1", Version = "2.0" }],
                    },
                },
            ],
        };

        XacmlJsonResponseExternal result = response.ToExternal();

        result.Should().BeEquivalentTo(response);
    }

    [Fact]
    public void ToExternal_PopulatedStatusDetails_ReturnsCopiedListInstance()
    {
        XacmlJsonResponse response = new()
        {
            Response =
            [
                new XacmlJsonResult
                {
                    Decision = "Deny",
                    Status = new XacmlJsonStatus { StatusDetails = ["detail1"] },
                },
            ],
        };

        XacmlJsonResponseExternal result = response.ToExternal();

        result.Response[0].Status.StatusDetails.Should().NotBeSameAs(response.Response[0].Status.StatusDetails);
        result.Response[0].Status.StatusDetails.Should().Equal("detail1");
    }

    [Fact]
    public void ToExternal_NullStatusAndCollections_ReturnsResultWithNullMembers()
    {
        XacmlJsonResponse response = new()
        {
            Response =
            [
                new XacmlJsonResult
                {
                    Decision = "NotApplicable",
                    Status = null,
                    Obligations = null,
                    AssociateAdvice = null,
                    Category = null,
                    PolicyIdentifierList = null,
                },
            ],
        };

        XacmlJsonResponseExternal result = response.ToExternal();

        XacmlJsonResultExternal mapped = result.Response.Should().ContainSingle().Which;
        mapped.Decision.Should().Be("NotApplicable");
        mapped.Status.Should().BeNull();
        mapped.Obligations.Should().BeNull();
        mapped.AssociateAdvice.Should().BeNull();
        mapped.Category.Should().BeNull();
        mapped.PolicyIdentifierList.Should().BeNull();
    }

    [Fact]
    public void ToExternal_NullResponseList_ReturnsNullResponseList()
    {
        XacmlJsonResponse response = new() { Response = null };

        XacmlJsonResponseExternal result = response.ToExternal();

        result.Response.Should().BeNull();
    }

    private static XacmlJsonCategoryExternal CreateExternalCategory(string id) => new()
    {
        CategoryId = $"urn:category:{id}",
        Id = id,
        Content = $"<content id=\"{id}\" />",
        Attribute =
        [
            new XacmlJsonAttributeExternal
            {
                AttributeId = $"urn:attribute:{id}",
                Value = $"value-{id}",
                Issuer = "altinn",
                DataType = "http://www.w3.org/2001/XMLSchema#string",
                IncludeInResult = true,
            },
        ],
    };
}
