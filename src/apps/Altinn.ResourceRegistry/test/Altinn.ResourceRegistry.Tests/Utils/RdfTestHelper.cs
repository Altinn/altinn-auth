using VDS.RDF;
using VDS.RDF.Parsing;

namespace Altinn.ResourceRegistry.Tests.Utils
{
    /// <summary>
    /// Helpers for asserting on the Turtle produced by the RDF export
    /// </summary>
    public static class RdfTestHelper
    {
        public const string ResourceBaseUri = "https://platform.altinn.no/resourceRegistry/";

        public static readonly Uri RdfType = new("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");
        public static readonly Uri PublicService = new("http://purl.org/vocab/cpsv#PublicService");
        public static readonly Uri ContactPointClass = new("http://data.europa.eu/m8g/ContactPoint");
        public static readonly Uri HasContactPoint = new("http://data.europa.eu/m8g/contactPoint");
        public static readonly Uri Email = new("http://data.europa.eu/m8g/email");
        public static readonly Uri Telephone = new("http://data.europa.eu/m8g/telephone");
        public static readonly Uri ContactPage = new("http://data.europa.eu/m8g/contactPage");
        public static readonly Uri Category = new("http://www.w3.org/2006/vcard/ns#category");

        /// <summary>
        /// The language tag expected on vcard:category. ContactPoint.Category stores no language, and the
        /// stored values are mostly Norwegian bokmål.
        /// </summary>
        public const string CategoryLanguage = "nb";

        /// <summary>
        /// Parses the export output as Turtle. Fails the test when the output is not Turtle,
        /// for example when the export returned its error text instead.
        /// </summary>
        public static IGraph ParseTurtle(string turtle)
        {
            Graph graph = new Graph();
            try
            {
                new TurtleParser(TurtleSyntax.W3C, false).Load(graph, new StringReader(turtle));
            }
            catch (RdfParseException ex)
            {
                Assert.Fail($"Export output is not valid Turtle: {ex.Message}\n{turtle}");
            }

            return graph;
        }

        /// <summary>
        /// Returns the node of an exported public service, failing the test if it is not exported.
        /// </summary>
        public static INode GetServiceNode(IGraph graph, string identifier)
        {
            INode serviceNode = graph.CreateUriNode(new Uri(ResourceBaseUri + identifier));
            Assert.True(
                graph.ContainsTriple(new Triple(serviceNode, graph.CreateUriNode(RdfType), graph.CreateUriNode(PublicService))),
                $"Expected {identifier} to be exported as a cpsv:PublicService");
            return serviceNode;
        }

        /// <summary>
        /// Returns the cv:contactPoint nodes of a service, whether they are IRIs or blank nodes.
        /// </summary>
        public static List<INode> GetContactPoints(IGraph graph, string identifier)
        {
            INode serviceNode = GetServiceNode(graph, identifier);
            return graph.GetTriplesWithSubjectPredicate(serviceNode, graph.CreateUriNode(HasContactPoint))
                .Select(t => t.Object)
                .ToList();
        }

        /// <summary>
        /// Returns the objects of the given property on a node.
        /// </summary>
        public static List<INode> GetObjects(IGraph graph, INode subject, Uri predicate)
        {
            return graph.GetTriplesWithSubjectPredicate(subject, graph.CreateUriNode(predicate))
                .Select(t => t.Object)
                .ToList();
        }

        /// <summary>
        /// Asserts that a node has exactly one value for the property, as a literal with the given
        /// language tag, or with no language tag when <paramref name="language"/> is empty.
        /// </summary>
        public static void AssertLiteral(IGraph graph, INode subject, Uri predicate, string expected, string language = "")
        {
            INode value = Assert.Single(GetObjects(graph, subject, predicate));
            ILiteralNode literal = Assert.IsAssignableFrom<ILiteralNode>(value);
            Assert.Equal(expected, literal.Value);
            Assert.Equal(language, literal.Language);
        }

        /// <summary>
        /// Asserts the contact point's vcard:category. CPSV-AP-NO v1.2 gives it the range rdf:langString,
        /// and the SHACL shapes reject an untagged value as a Violation.
        /// </summary>
        public static void AssertCategory(IGraph graph, INode contactPoint, string expected)
            => AssertLiteral(graph, contactPoint, Category, expected, CategoryLanguage);

        /// <summary>
        /// Asserts that a node has no value for the property.
        /// </summary>
        public static void AssertAbsent(IGraph graph, INode subject, Uri predicate)
        {
            Assert.Empty(GetObjects(graph, subject, predicate));
        }
    }
}
