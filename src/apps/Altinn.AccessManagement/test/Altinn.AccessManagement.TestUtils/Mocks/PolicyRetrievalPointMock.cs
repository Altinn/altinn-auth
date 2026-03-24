using System.Xml;
using System.Xml.Schema;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessManagement.TestUtils.Mocks
{
    /// <summary>
    /// Mock class for <see cref="IPolicyRetrievalPoint"></see> interface
    /// </summary>
    public class PolicyRetrievalPointMock : IPolicyRetrievalPoint
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly ILogger<PolicyRetrievalPointMock> _logger;

        private readonly string _orgAttributeId = "urn:altinn:org";

        private readonly string _appAttributeId = "urn:altinn:app";

        /// <summary>
        /// Constructor setting up dependencies
        /// </summary>
        /// <param name="httpContextAccessor">httpContextAccessor</param>
        /// <param name="logger">logger</param>
        public PolicyRetrievalPointMock(IHttpContextAccessor httpContextAccessor, ILogger<PolicyRetrievalPointMock> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Empty constructor.
        /// </summary>
        public PolicyRetrievalPointMock()
        {
        }

        /// <inheritdoc/>
        public async Task<XacmlPolicy> GetPolicyAsync(XacmlContextRequest request, CancellationToken cancellationToken = default)
        {
            string testID = GetTestId(_httpContextAccessor.HttpContext);
            if (!string.IsNullOrEmpty(testID) && testID.ToLower().Contains("altinnapps"))
            {
                if (File.Exists(Path.Combine(GetPolicyPath(request), "policy.xml")))
                {
                    return await Task.FromResult(ParsePolicy("policy.xml", GetPolicyPath(request)));
                }

                return await Task.FromResult(ParsePolicy(testID + "Policy.xml", GetAltinnAppsPath()));
            }
            else
            {
                return await Task.FromResult(ParsePolicy(testID + "Policy.xml", GetConformancePath()));
            }
        }

        /// <inheritdoc/>
        public async Task<XacmlPolicy> GetPolicyAsync(string org, string app, CancellationToken cancellationToken = default)
        {
            if (File.Exists(Path.Combine(GetAltinnAppsPolicyPath(org, app), "policy.xml")))
            {
                return await Task.FromResult(ParsePolicy("policy.xml", GetAltinnAppsPolicyPath(org, app)));
            }

            return null;
        }

        /// <inheritdoc/>
        public async Task<XacmlPolicy> GetPolicyAsync(string resourceRegistry, CancellationToken cancellationToken = default)
        {
            string sanitizedResourceRegistry = resourceRegistry?.Replace("\r", string.Empty).Replace("\n", string.Empty);
            if (string.IsNullOrEmpty(resourceRegistry) || !IsValidResourceRegistryId(resourceRegistry))
            {
                _logger.LogWarning("Invalid resource registry id provided to PolicyRetrievalPointMock: {ResourceRegistry}", sanitizedResourceRegistry);
                return await Task.FromResult<XacmlPolicy>(null);
            }

            if (resourceRegistry.StartsWith("app_", StringComparison.Ordinal))
            {
                string[] parts = resourceRegistry.Split('_');
                if (parts.Length >= 3)
                {
                    string app = parts[2];
                    string org = parts[1];
                    return await GetPolicyAsync(org, app, cancellationToken);
                }

                _logger.LogWarning("Resource registry id '{ResourceRegistry}' did not have the expected format 'app_{{org}}_{{app}}'.", sanitizedResourceRegistry);
                return await Task.FromResult<XacmlPolicy>(null);
            }

            string testDataFolder = GetAltinnResourcePolicyPath(resourceRegistry);
            if (File.Exists(Path.Combine(testDataFolder, "resourcepolicy.xml")))
            {
                return await Task.FromResult(ParsePolicy("resourcepolicy.xml", GetAltinnResourcePolicyPath(resourceRegistry)));
            }

            return await Task.FromResult<XacmlPolicy>(null);
        }

        /// <inheritdoc/>
        public async Task<XacmlPolicy> GetPolicyVersionAsync(string policyPath, string version, CancellationToken cancellationToken = default)
        {
            string path = GetAltinnAppsDelegationPolicyPath(policyPath);
            if (File.Exists(path))
            {
                return await Task.FromResult(ParsePolicy(string.Empty, path));
            }

            string safePath = path?.Replace("\r", string.Empty).Replace("\n", string.Empty);
            _logger.LogWarning("Policy Version was not found for the provided path.");

            return null;
        }

        private static XacmlPolicy ParsePolicy(string policyDocumentTitle, string policyPath)
        {
            XmlDocument policyDocument = new XmlDocument();

            policyDocument.Load(Path.Combine(policyPath, policyDocumentTitle));
            XacmlPolicy policy;
            using (XmlReader reader = XmlReader.Create(new StringReader(policyDocument.OuterXml), CreateSafeXmlReaderSettings()))
            {
                policy = XacmlParser.ParseXacmlPolicy(reader);
            }

            return policy;
        }

        private string GetPolicyPath(XacmlContextRequest request)
        {
            string org = string.Empty;
            string app = string.Empty;
            foreach (XacmlContextAttributes attr in request.Attributes)
            {
                if (attr.Category.OriginalString.Equals(XacmlConstants.MatchAttributeCategory.Resource))
                {
                    foreach (XacmlAttribute asd in attr.Attributes)
                    {
                        if (asd.AttributeId.OriginalString.Equals(_orgAttributeId))
                        {
                            foreach (var asff in asd.AttributeValues)
                            {
                                org = asff.Value;
                                break;
                            }
                        }

                        if (asd.AttributeId.OriginalString.Equals(_appAttributeId))
                        {
                            foreach (var asff in asd.AttributeValues)
                            {
                                app = asff.Value;
                                break;
                            }
                        }
                    }
                }
            }

            return GetAltinnAppsPolicyPath(org, app);
        }

        private string GetAltinnAppsPolicyPath(string org, string app)
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRetrievalPointMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "..", "AccessMgmt.Tests", "Data", "Xacml", "3.0", "AltinnApps", org, app);
        }

        private string GetAltinnResourcePolicyPath(string resourceRegistryId)
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRetrievalPointMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "..", "AccessMgmt.Tests", "Data", "Xacml", "3.0", "ResourceRegistry", resourceRegistryId);
        }

        private static bool IsValidResourceRegistryId(string resourceRegistryId)
        {
            if (string.IsNullOrEmpty(resourceRegistryId))
            {
                return false;
            }

            if (resourceRegistryId.Contains("..", StringComparison.Ordinal))
            {
                return false;
            }

            if (resourceRegistryId.Contains('/') || resourceRegistryId.Contains('\\'))
            {
                return false;
            }

            return true;
        }

        private static string GetAltinnAppsDelegationPolicyPath(string policyPath)
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRetrievalPointMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "..", "AccessMgmt.Tests", "Data", "blobs", "input", policyPath);
        }

        private string GetTestId(HttpContext context)
        {
            return context.Request.Headers["testcase"];
        }

        private string GetAltinnAppsPath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRetrievalPointMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "..", "AccessMgmt.Tests", "Data", "Xacml", "3.0", "AltinnApps");
        }

        private string GetConformancePath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRetrievalPointMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "..", "AccessMgmt.Tests", "Data", "Xacml", "3.0", "ConformanceTests");
        }

        private static XmlReaderSettings CreateSafeXmlReaderSettings()
        {
            XmlReaderSettings settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                DtdProcessing = DtdProcessing.Prohibit
            };

            settings.Schemas = new XmlSchemaSet();
            settings.ValidationFlags &= ~XmlSchemaValidationFlags.ProcessInlineSchema;
            settings.ValidationFlags &= ~XmlSchemaValidationFlags.ProcessSchemaLocation;

            return settings;
        }
    }
}
