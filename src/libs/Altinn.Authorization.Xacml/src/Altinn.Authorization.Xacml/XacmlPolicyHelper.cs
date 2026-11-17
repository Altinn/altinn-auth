using System.Xml;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Authorization.Xacml.Constants;

namespace Altinn.Authorization.Xacml;

/// <summary>
/// Policy helper methods shared by the verticals that read Altinn XACML policies
/// </summary>
public static class XacmlPolicyHelper
{
    /// <summary>
    /// Finds the correct policy path based on a XacmlContextRequest
    /// </summary>
    /// <param name="request">Xacml context request to use for finding the org and app for building the path</param>
    /// <returns>The policy path for the org and app in the request</returns>
    public static string GetPolicyPath(XacmlContextRequest request)
    {
        string org = string.Empty;
        string app = string.Empty;

        foreach (XacmlContextAttributes attr in request.Attributes.Where(attr => attr.Category.OriginalString.Equals(XacmlConstants.MatchAttributeCategory.Resource)))
        {
            foreach (XacmlAttribute asd in attr.Attributes)
            {
                if (asd.AttributeId.OriginalString.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.OrgAttribute))
                {
                    org = asd.AttributeValues.FirstOrDefault()?.Value ?? string.Empty;
                }

                if (asd.AttributeId.OriginalString.Equals(AltinnXacmlConstants.MatchAttributeIdentifiers.AppAttribute))
                {
                    app = asd.AttributeValues.FirstOrDefault()?.Value ?? string.Empty;
                }
            }
        }

        return GetAltinnAppsPolicyPath(org, app);
    }

    /// <summary>
    /// Builds the policy path based on org and app names
    /// </summary>
    /// <param name="org">The organization name/identifier</param>
    /// <param name="app">The altinn app name</param>
    /// <returns>The policy path for the app</returns>
    public static string GetAltinnAppsPolicyPath(string org, string app)
    {
        if (string.IsNullOrWhiteSpace(org))
        {
            throw new ArgumentException("Org was not defined");
        }

        if (string.IsNullOrWhiteSpace(app))
        {
            throw new ArgumentException("App was not defined");
        }

        return $"{org.AsFileName()}/{app.AsFileName()}/policy.xml";
    }

    /// <summary>
    /// Takes the file IO stream and parses the policy file to a XacmlPolicy <see cref="XacmlPolicy"/>
    /// </summary>
    /// <param name="stream">The file IO stream, which is read from the start</param>
    /// <returns>XacmlPolicy</returns>
    public static XacmlPolicy ParsePolicy(Stream stream)
    {
        stream.Position = 0;
        using XmlReader reader = XmlReader.Create(stream);
        return XacmlParser.ParseXacmlPolicy(reader);
    }

    /// <summary>
    /// Reads the stream from its current position into memory and parses it to a XacmlPolicy <see cref="XacmlPolicy"/>.
    /// Use this for streams that are not seekable or should not be read synchronously.
    /// </summary>
    /// <param name="stream">The file IO stream</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    /// <returns>XacmlPolicy</returns>
    public static async Task<XacmlPolicy> ParsePolicyAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using MemoryStream memoryStream = new();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return ParsePolicy(memoryStream);
    }

    /// <summary>
    /// Gets the authentication level requirement from the obligation expression of the XacmlPolicy if specified
    /// </summary>
    /// <param name="policy">The policy</param>
    /// <returns>Minimum authentication level requirement</returns>
    public static int GetMinimumAuthenticationLevelFromXacmlPolicy(XacmlPolicy policy)
    {
        foreach (XacmlObligationExpression oblExpr in policy.ObligationExpressions)
        {
            foreach (XacmlAttributeAssignmentExpression attrExpr in oblExpr.AttributeAssignmentExpressions)
            {
                if (attrExpr.Category?.OriginalString == AltinnXacmlConstants.MatchAttributeCategory.MinimumAuthenticationLevel &&
                    attrExpr.Property is XacmlAttributeValue attrValue &&
                    int.TryParse(attrValue.Value, out int minAuthLevel))
                {
                    return minAuthLevel;
                }
            }
        }

        return 0;
    }

    /// <summary>
    /// Gets a nested list of attribute matches for all XacmlMatch instances matching the specified attribute category.
    /// Each inner list holds the matches of one AllOf; AllOfs without a match in the category are left out.
    /// </summary>
    /// <typeparam name="TMatch">The attribute match model the caller wants back</typeparam>
    /// <param name="rule">The xacml rule to process</param>
    /// <param name="category">The attribute category to match</param>
    /// <param name="createMatch">Creates the model from the attribute id and the attribute value</param>
    /// <returns>Nested list of attribute matches</returns>
    public static List<List<TMatch>> GetRulePolicyAttributeMatchesForCategory<TMatch>(XacmlRule rule, string category, Func<string, string, TMatch> createMatch)
    {
        List<List<TMatch>> ruleAttributeMatches = [];

        foreach (XacmlAnyOf anyOf in rule.Target.AnyOf)
        {
            foreach (XacmlAllOf allOf in anyOf.AllOf)
            {
                List<TMatch> anyOfAttributeMatches = [];
                foreach (XacmlMatch xacmlMatch in allOf.Matches)
                {
                    if (xacmlMatch.AttributeDesignator.Category.Equals(category))
                    {
                        anyOfAttributeMatches.Add(createMatch(xacmlMatch.AttributeDesignator.AttributeId.OriginalString, xacmlMatch.AttributeValue.Value));
                    }
                }

                if (anyOfAttributeMatches.Count > 0)
                {
                    ruleAttributeMatches.Add(anyOfAttributeMatches);
                }
            }
        }

        return ruleAttributeMatches;
    }
}
