using System;
using System.Xml;

using Altinn.Authorization.ABAC.Constants;
using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.Platform.Authorization.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Altinn.Platform.Authorization.IntegrationTests.MockServices
{
    public class PolicyRetrievalPointMock : IPolicyRetrievalPoint
    {
        private readonly IMemoryCache _memoryCache;

        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly ILogger<PolicyRetrievalPointMock> _logger;   

        private readonly string _orgAttributeId = "urn:altinn:org";

        private readonly string _appAttributeId = "urn:altinn:app";

        private readonly string _resourceregistryAttributeId = "urn:altinn:resource";

        public PolicyRetrievalPointMock(IMemoryCache memoryCache, IHttpContextAccessor httpContextAccessor, ILogger<PolicyRetrievalPointMock> logger)
        {
            _memoryCache = memoryCache;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<XacmlPolicy> GetPolicyAsync(XacmlContextRequest request)
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
            else if (!string.IsNullOrEmpty(testID) && testID.ToLower().Contains("resourceregistry"))
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

        public async Task<XacmlPolicy> GetPolicyAsync(string org, string app)
        {
            if (File.Exists(Path.Combine(GetAltinnAppsPolicyPath(org, app), "policy.xml")))
            {
                return await Task.FromResult(ParsePolicy("policy.xml", GetAltinnAppsPolicyPath(org, app)));
            }

            return null;
        }

        public async Task<XacmlPolicy> GetPolicyVersionAsync(string policyPath, string version, CancellationToken cancellationToken = default)
        {
            string path = GetAltinnAppsDelegationPolicyPath(policyPath);
            if (File.Exists(path))
            {
                return await Task.FromResult(ParsePolicy(string.Empty, path));
            }

            _logger.LogWarning("Polcy Version did not found policy " + path);

            return null;
        }

        private string GetPolicyPath(XacmlContextRequest request)
        {
            string org = string.Empty;
            string app = string.Empty;
            string resourceregistry = string.Empty;
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

                        if (asd.AttributeId.OriginalString.Equals(_resourceregistryAttributeId))
                        {
                            foreach (var asff in asd.AttributeValues)
                            {
                                if (asff.Value.StartsWith("app_"))
                                {
                                    var orgApp = asff.Value.Split("_");
                                    org = orgApp[1];
                                    app = orgApp[2];
                                    break;
                                }

                                resourceregistry = asff.Value;
                                break;
                            }
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(resourceregistry))
            {
                return GetAltinnResourceRegistryPath(resourceregistry);
            }

            return GetAltinnAppsPolicyPath(org, app);
        }

        private string GetAltinnAppsPolicyPath(string org, string app)
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(AltinnApps_DecisionTests).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "Xacml", "3.0", "AltinnApps", org, app);
        }

        private static string GetAltinnAppsDelegationPolicyPath(string policyPath)
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(AltinnApps_DecisionTests).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "blobs", "input", policyPath);
        }

        private string GetTestId(HttpContext context)
        {
            return context.Request.Headers["testcase"];
        }

        private string GetAltinnAppsPath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(AltinnApps_DecisionTests).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "Xacml", "3.0", "AltinnApps");
        }

        private static string GetAltinnResourceRegistryPath(string resourceid)
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(AltinnApps_DecisionTests).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "Xacml", "3.0", "ResourceRegistry", resourceid);
        }

        private string GetConformancePath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(AltinnApps_DecisionTests).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "Xacml", "3.0", "ConformanceTests");
        }

        public XacmlPolicy ParsePolicy(string policyDocumentTitle, string policyPath)
        {
            string cacheKey = policyPath;
            if (!_memoryCache.TryGetValue(cacheKey, out XacmlPolicy policy))
            {
                XmlDocument policyDocument = new XmlDocument();

                policyDocument.Load(Path.Combine(policyPath, policyDocumentTitle));
                
                using (XmlReader reader = XmlReader.Create(new StringReader(policyDocument.OuterXml)))
                {
                    policy = XacmlParser.ParseXacmlPolicy(reader);
                }

                PutXacmlPolicyInCache(cacheKey, policy);
            }

            return policy;
        }

        private void PutXacmlPolicyInCache(string policyPath, XacmlPolicy policy)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
               .SetPriority(CacheItemPriority.High)
               .SetAbsoluteExpiration(new TimeSpan(0, 5, 0));

            _memoryCache.Set(policyPath, policy, cacheEntryOptions);
        }
    }
}
