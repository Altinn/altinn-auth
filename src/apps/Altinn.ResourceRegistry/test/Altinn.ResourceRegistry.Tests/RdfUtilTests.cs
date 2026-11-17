using Altinn.ResourceRegistry.Core.Enums;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Utils;
using VDS.RDF;

using static Altinn.ResourceRegistry.Tests.Utils.RdfTestHelper;

namespace Altinn.ResourceRegistry.Tests;

/// <summary>
/// Tests for the CPSV-AP-NO export in <see cref="RdfUtil"/>, covering cv:contactPoint (issue #4269).
/// </summary>
public class RdfUtilTests
{
    private const string Id = "test-resource";

    [Fact]
    public void CreateRdf_NoContactPoints_ExportsServiceWithoutContactPoint()
    {
        ServiceResource resource = CreateResource(contactPoints: null);

        IGraph graph = ParseTurtle(RdfUtil.CreateRdf([resource]));

        Assert.Empty(GetContactPoints(graph, Id));
    }

    [Fact]
    public void CreateRdf_EmptyContactPointList_ExportsServiceWithoutContactPoint()
    {
        ServiceResource resource = CreateResource(contactPoints: []);

        IGraph graph = ParseTurtle(RdfUtil.CreateRdf([resource]));

        Assert.Empty(GetContactPoints(graph, Id));
    }

    [Fact]
    public void CreateRdf_ContactPointWithAllFields_MapsEveryField()
    {
        ServiceResource resource = CreateResource(contactPoints:
        [
            new ContactPoint
            {
                Category = "Support",
                Email = "support@example.org",
                Telephone = "+47 12345678",
                ContactPage = "https://example.org/kontakt",
            },
        ]);

        IGraph graph = ParseTurtle(RdfUtil.CreateRdf([resource]));

        INode contactPoint = Assert.Single(GetContactPoints(graph, Id));
        Assert.True(
            graph.ContainsTriple(new Triple(contactPoint, graph.CreateUriNode(RdfType), graph.CreateUriNode(ContactPointClass))),
            "Expected the contact point to be typed cv:ContactPoint");

        AssertCategory(graph, contactPoint, "Support");
        AssertLiteral(graph, contactPoint, Email, "support@example.org");
        AssertLiteral(graph, contactPoint, Telephone, "+47 12345678");

        INode contactPage = Assert.Single(GetObjects(graph, contactPoint, ContactPage));
        IUriNode contactPageUri = Assert.IsAssignableFrom<IUriNode>(contactPage);
        Assert.Equal(new Uri("https://example.org/kontakt"), contactPageUri.Uri);
    }

    [Fact]
    public void CreateRdf_SeveralContactPoints_ExportsOneNodePerContactPoint()
    {
        ServiceResource resource = CreateResource(contactPoints:
        [
            new ContactPoint { Category = "Utvikler", Email = "dev@example.org" },
            new ContactPoint { Category = "Brukerstøtte", Email = "support@example.org" },
            new ContactPoint { Category = "Sentralbord", Telephone = "+47 12345678" },
        ]);

        IGraph graph = ParseTurtle(RdfUtil.CreateRdf([resource]));

        List<INode> contactPoints = GetContactPoints(graph, Id);
        Assert.Equal(3, contactPoints.Count);
        Assert.Equal(3, contactPoints.Distinct().Count());

        List<string> emails = contactPoints
            .SelectMany(cp => GetObjects(graph, cp, Email))
            .Cast<ILiteralNode>()
            .Select(l => l.Value)
            .ToList();
        Assert.Equivalent(new[] { "dev@example.org", "support@example.org" }, emails);

        List<ILiteralNode> categories = contactPoints
            .SelectMany(cp => GetObjects(graph, cp, Category))
            .Cast<ILiteralNode>()
            .ToList();
        Assert.Equivalent(new[] { "Utvikler", "Brukerstøtte", "Sentralbord" }, categories.Select(l => l.Value));
        Assert.All(categories, l => Assert.Equal(CategoryLanguage, l.Language));
    }

    [Fact]
    public void CreateRdf_ContactPointsOnTwoResources_AreNotShared()
    {
        ServiceResource first = CreateResource("first-resource", [new ContactPoint { Email = "first@example.org" }]);
        ServiceResource second = CreateResource("second-resource", [new ContactPoint { Email = "second@example.org" }]);

        IGraph graph = ParseTurtle(RdfUtil.CreateRdf([first, second]));

        INode firstContactPoint = Assert.Single(GetContactPoints(graph, "first-resource"));
        INode secondContactPoint = Assert.Single(GetContactPoints(graph, "second-resource"));
        Assert.NotEqual(firstContactPoint, secondContactPoint);
        AssertLiteral(graph, firstContactPoint, Email, "first@example.org");
        AssertLiteral(graph, secondContactPoint, Email, "second@example.org");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRdf_ContactPointWithOnlyEmptyFields_IsSkipped(string? empty)
    {
        ServiceResource resource = CreateResource(contactPoints:
        [
            new ContactPoint { Category = empty!, Email = empty!, Telephone = empty!, ContactPage = empty! },
        ]);

        IGraph graph = ParseTurtle(RdfUtil.CreateRdf([resource]));

        Assert.Empty(GetContactPoints(graph, Id));
        Assert.Empty(graph.GetTriplesWithObject(graph.CreateUriNode(ContactPointClass)));
    }

    [Fact]
    public void CreateRdf_EmptyContactPointNextToValidOne_OnlyExportsTheValidOne()
    {
        ServiceResource resource = CreateResource(contactPoints:
        [
            new ContactPoint { Category = string.Empty, Email = string.Empty, Telephone = string.Empty, ContactPage = string.Empty },
            new ContactPoint { Category = "Brukerstøtte", Email = "postmottak@example.org", Telephone = string.Empty, ContactPage = string.Empty },
        ]);

        IGraph graph = ParseTurtle(RdfUtil.CreateRdf([resource]));

        INode contactPoint = Assert.Single(GetContactPoints(graph, Id));
        AssertLiteral(graph, contactPoint, Email, "postmottak@example.org");
    }

    [Fact]
    public void CreateRdf_EmptyFields_AreNotEmitted()
    {
        // Mirrors the stored data, where unused fields are "" rather than null
        ServiceResource resource = CreateResource(contactPoints:
        [
            new ContactPoint { Category = "Brukerstøtte", Email = "postmottak@example.org", Telephone = string.Empty, ContactPage = "  " },
        ]);

        IGraph graph = ParseTurtle(RdfUtil.CreateRdf([resource]));

        INode contactPoint = Assert.Single(GetContactPoints(graph, Id));
        AssertCategory(graph, contactPoint, "Brukerstøtte");
        AssertLiteral(graph, contactPoint, Email, "postmottak@example.org");
        AssertAbsent(graph, contactPoint, Telephone);
        AssertAbsent(graph, contactPoint, ContactPage);
    }

    [Fact]
    public void CreateRdf_ValuesWithSurroundingWhitespace_AreTrimmed()
    {
        // The stored data has emails with a leading space, for example in eformidling-dpo-meldingsutveksling
        ServiceResource resource = CreateResource(contactPoints:
        [
            new ContactPoint { Category = " Produktteamleiar ", Email = " person@example.org", Telephone = "+47 12345678 " },
        ]);

        IGraph graph = ParseTurtle(RdfUtil.CreateRdf([resource]));

        INode contactPoint = Assert.Single(GetContactPoints(graph, Id));
        AssertCategory(graph, contactPoint, "Produktteamleiar");
        AssertLiteral(graph, contactPoint, Email, "person@example.org");
        AssertLiteral(graph, contactPoint, Telephone, "+47 12345678");
    }

    [Theory]
    [InlineData("www.skatteetaten.no")] // as stored for ske-innrapportering-boligsameie
    [InlineData("not a url")]
    [InlineData("/kontakt")]
    public void CreateRdf_InvalidContactPage_IsSkippedAndExportSucceeds(string contactPage)
    {
        ServiceResource resource = CreateResource(contactPoints:
        [
            new ContactPoint { Category = "Innrapportering", Email = string.Empty, Telephone = string.Empty, ContactPage = contactPage },
        ]);
        ServiceResource other = CreateResource("other-resource", [new ContactPoint { Email = "other@example.org" }]);

        IGraph graph = ParseTurtle(RdfUtil.CreateRdf([resource, other]));

        INode contactPoint = Assert.Single(GetContactPoints(graph, Id));
        AssertCategory(graph, contactPoint, "Innrapportering");
        AssertAbsent(graph, contactPoint, ContactPage);

        // One resource's bad contact page must not take down the rest of the export
        INode otherContactPoint = Assert.Single(GetContactPoints(graph, "other-resource"));
        AssertLiteral(graph, otherContactPoint, Email, "other@example.org");
    }

    [Fact]
    public void CreateRdf_ContactPointWithOnlyInvalidContactPage_IsSkipped()
    {
        ServiceResource resource = CreateResource(contactPoints:
        [
            new ContactPoint { Category = string.Empty, Email = string.Empty, Telephone = string.Empty, ContactPage = "not a url" },
        ]);

        IGraph graph = ParseTurtle(RdfUtil.CreateRdf([resource]));

        Assert.Empty(GetContactPoints(graph, Id));
    }

    [Fact]
    public void CreateRdf_ExcludedResource_ExportsNoContactPoints()
    {
        ServiceResource resource = CreateResource(contactPoints: [new ContactPoint { Email = "support@example.org" }]);
        resource.ResourceType = ResourceType.MaskinportenSchema;

        IGraph graph = ParseTurtle(RdfUtil.CreateRdf([resource]));

        Assert.Empty(graph.GetTriplesWithPredicate(graph.CreateUriNode(HasContactPoint)));
        Assert.Empty(graph.GetTriplesWithObject(graph.CreateUriNode(ContactPointClass)));
    }

    private static ServiceResource CreateResource(List<ContactPoint>? contactPoints)
        => CreateResource(Id, contactPoints);

    private static ServiceResource CreateResource(string identifier, List<ContactPoint>? contactPoints)
    {
        return new ServiceResource
        {
            Identifier = identifier,
            Title = new Dictionary<string, string> { { "nb", "Testtjeneste" } },
            Description = new Dictionary<string, string> { { "nb", "Beskrivelse" } },
            ResourceType = ResourceType.GenericAccessResource,
            HasCompetentAuthority = new CompetentAuthority
            {
                Organization = "991825827",
                Orgcode = "digdir",
            },
            ContactPoints = contactPoints,
        };
    }
}
