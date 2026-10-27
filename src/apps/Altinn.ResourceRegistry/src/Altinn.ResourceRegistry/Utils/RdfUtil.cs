using System.Text;
using Altinn.ResourceRegistry.Core.Enums;
using Altinn.ResourceRegistry.Core.Models;
using VDS.RDF;
using VDS.RDF.Writing;

namespace Altinn.ResourceRegistry.Utils
{
    /// <summary>
    /// Utility class for working with the Rdf standard
    /// </summary>
    public static class RdfUtil
    {
        /// <summary>
        /// ContactPoint.Category is stored without a language, and the stored values are mostly Norwegian bokmål.
        /// </summary>
        private const string ContactPointCategoryLanguage = "nb";

        /// <summary>
        /// Creates an Rdf string representation for a service resource from the resource registry
        /// </summary>
        /// <param name="serviceResources">List of  service resource models to create Rdf representation of</param>
        /// <returns>Rdf representation as a string value</returns>
        public static string CreateRdf(List<ServiceResource> serviceResources)
        {
            Graph g = new Graph();
            g.NamespaceMap.AddNamespace("cpsv", UriFactory.Create("http://purl.org/vocab/cpsv#"));
            g.NamespaceMap.AddNamespace("dct", UriFactory.Create("http://purl.org/dc/terms/"));
            g.NamespaceMap.AddNamespace("cv", UriFactory.Create("http://data.europa.eu/m8g/"));
            g.NamespaceMap.AddNamespace("skos", UriFactory.Create("http://www.w3.org/2004/02/skos/core#"));
            g.NamespaceMap.AddNamespace("dcat", UriFactory.Create("http://www.w3.org/ns/dcat#"));
            g.NamespaceMap.AddNamespace("schema", UriFactory.Create("http://schema.org/"));
            g.NamespaceMap.AddNamespace("eli", UriFactory.Create("http://data.europa.eu/eli/ontology#"));
            g.NamespaceMap.AddNamespace("foaf", UriFactory.Create("http://xmlns.com/foaf/0.1/"));
            g.NamespaceMap.AddNamespace("vcard", UriFactory.Create("http://www.w3.org/2006/vcard/ns#"));

            StringBuilder resourceErrors = new StringBuilder();

            foreach (ServiceResource serviceResource in serviceResources)
            {
                if (!IncludeInExport(serviceResource))
                {
                    // Skip this element
                    continue;
                }

                try
                {
                    IUriNode serviceNode = g.CreateUriNode(UriFactory.Create("https://platform.altinn.no/resourceRegistry/" + serviceResource.Identifier));
                    IUriNode predicate = g.CreateUriNode("rdf:type");
                    IUriNode objectNode = g.CreateUriNode("cpsv:PublicService");

                    g.Assert(new Triple(serviceNode, predicate, objectNode));

                    IUriNode identfierPredicate = g.CreateUriNode("dct:identifier");
                    ILiteralNode identiferObject = g.CreateLiteralNode(serviceResource.Identifier);
                    g.Assert(new Triple(serviceNode, identfierPredicate, identiferObject));

                    if (serviceResource.Title != null && serviceResource.Title.Count > 0)
                    {
                        IUriNode titlePredicate = g.CreateUriNode("dct:title");
                        foreach (KeyValuePair<string, string> kvp in serviceResource.Title)
                        {
                            ILiteralNode titleObject = g.CreateLiteralNode(kvp.Value, kvp.Key);
                            g.Assert(new Triple(serviceNode, titlePredicate, titleObject));
                        }
                    }

                    if (serviceResource.Description != null && serviceResource.Description.Count > 0)
                    {
                        IUriNode descriptionPredicate = g.CreateUriNode("dct:description");
                        foreach (KeyValuePair<string, string> kvp in serviceResource.Description)
                        {
                            ILiteralNode descriptionObject = g.CreateLiteralNode(kvp.Value, kvp.Key);
                            g.Assert(new Triple(serviceNode, descriptionPredicate, descriptionObject));
                        }
                    }

                    if (serviceResource.HasCompetentAuthority != null)
                    {
                        IUriNode competentAuthorityPredicate = g.CreateUriNode("cv:hasCompetentAuthority");
                        IUriNode competentAuthorityObject = g.CreateUriNode(UriFactory.Create("https://organization-catalogue.fellesdatakatalog.digdir.no/organizations/" + serviceResource.HasCompetentAuthority.Organization));
                        g.Assert(new Triple(serviceNode, competentAuthorityPredicate, competentAuthorityObject));
                    }

                    if (serviceResource.Keywords != null && serviceResource.Keywords.Count > 0)
                    {
                        IUriNode keywordPredicate = g.CreateUriNode("dct:keyword");

                        foreach (Keyword keyword in serviceResource.Keywords)
                        {
                            ILiteralNode keywordObject = g.CreateLiteralNode(keyword.Word, keyword.Language);
                            g.Assert(new Triple(serviceNode, keywordPredicate, keywordObject));
                        }
                    }

                    AddContactPoints(g, serviceNode, serviceResource.ContactPoints);
                }
                catch (Exception ex)
                {
                    resourceErrors.AppendLine("Error for " + serviceResource.Identifier);
                    resourceErrors.AppendLine($"{ex.Message}");
                }
            }

            // Assume that the Graph to be saved has already been loaded into a variable g
            CompressingTurtleWriter rdfTurtlewriter = new CompressingTurtleWriter(VDS.RDF.Parsing.TurtleSyntax.W3C);

            // https://dotnetrdf.org/docs/stable/api/VDS.RDF.Writing.WriterCompressionLevel.html
            rdfTurtlewriter.CompressionLevel = 0;

            System.IO.StringWriter sw = new System.IO.StringWriter();

            // Call the Save() method to write to the StringWriter
            rdfTurtlewriter.Save(g, sw);

            if (!string.IsNullOrEmpty(resourceErrors.ToString()))
            {
                return resourceErrors.ToString();
            }

            // We can now retrieve the written RDF by using the ToString() method of the StringWriter
            return sw.ToString();
        }

        private static void AddContactPoints(IGraph g, IUriNode serviceNode, List<ContactPoint> contactPoints)
        {
            if (contactPoints == null)
            {
                return;
            }

            IUriNode contactPointPredicate = g.CreateUriNode("cv:contactPoint");

            for (int i = 0; i < contactPoints.Count; i++)
            {
                ContactPoint contactPoint = contactPoints[i];
                if (contactPoint == null)
                {
                    continue;
                }

                string category = contactPoint.Category?.Trim();
                string email = contactPoint.Email?.Trim();
                string telephone = contactPoint.Telephone?.Trim();
                Uri contactPage = CreateContactPageUri(contactPoint.ContactPage);

                if (string.IsNullOrEmpty(category) && string.IsNullOrEmpty(email) && string.IsNullOrEmpty(telephone) && contactPage == null)
                {
                    // Nothing usable left, so do not export an empty cv:ContactPoint
                    continue;
                }

                // Numbered by position in the stored list, so the IRI stays the same between exports
                IUriNode contactPointNode = g.CreateUriNode(UriFactory.Create(serviceNode.Uri.AbsoluteUri + "/contactpoint/" + i));
                g.Assert(new Triple(serviceNode, contactPointPredicate, contactPointNode));
                g.Assert(new Triple(contactPointNode, g.CreateUriNode("rdf:type"), g.CreateUriNode("cv:ContactPoint")));

                if (!string.IsNullOrEmpty(category))
                {
                    g.Assert(new Triple(contactPointNode, g.CreateUriNode("vcard:category"), g.CreateLiteralNode(category, ContactPointCategoryLanguage)));
                }

                if (!string.IsNullOrEmpty(email))
                {
                    g.Assert(new Triple(contactPointNode, g.CreateUriNode("cv:email"), g.CreateLiteralNode(email)));
                }

                if (!string.IsNullOrEmpty(telephone))
                {
                    g.Assert(new Triple(contactPointNode, g.CreateUriNode("cv:telephone"), g.CreateLiteralNode(telephone)));
                }

                if (contactPage != null)
                {
                    g.Assert(new Triple(contactPointNode, g.CreateUriNode("cv:contactPage"), g.CreateUriNode(contactPage)));
                }
            }
        }

        /// <summary>
        /// Returns the contact page as an absolute http(s) IRI, or null when it is not one.
        /// An invalid value is skipped rather than thrown, since one exception replaces the whole export.
        /// </summary>
        private static Uri CreateContactPageUri(string contactPage)
        {
            if (string.IsNullOrWhiteSpace(contactPage)
                || !Uri.TryCreate(contactPage.Trim(), UriKind.Absolute, out Uri uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                return null;
            }

            return uri;
        }

        private static bool IncludeInExport(ServiceResource resource)
        {
            if (resource.ResourceType.Equals(ResourceType.MaskinportenSchema) || resource.ResourceType.Equals(ResourceType.Systemresource))
            {
                return false;
            }

            if (resource.HasCompetentAuthority == null
                || string.IsNullOrWhiteSpace(resource.HasCompetentAuthority.Orgcode)
                || resource.HasCompetentAuthority.Orgcode.Equals("ttd", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(resource.HasCompetentAuthority.Organization))
            {
                return false;
            }

            return true;
        }
    }
}
