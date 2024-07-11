using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Altinn2;
using Altinn.ResourceRegistry.Core.Services;
using System.Xml;

namespace Altinn.ResourceRegistry.Tests.Mocks
{
    public class Altinn2ServicesClientMock : IAltinn2Services
    {
        public async Task<List<AvailableService>> AvailableServices(int languageId, bool includeExpired,  CancellationToken cancellationToken)
        {
            string? testDataFolder = GetAltinn2TestDatafolder();
            if (testDataFolder != null)
            {
                string availableServiceFilePath = Path.Combine(testDataFolder, $"availableServices{languageId}.json");

                List<AvailableService>? availableServices = null;

                if (File.Exists(availableServiceFilePath))
                {
                    string content = await File.ReadAllTextAsync(availableServiceFilePath, cancellationToken);
                    if (!string.IsNullOrEmpty(content))
                    {
                        availableServices = System.Text.Json.JsonSerializer.Deserialize<List<AvailableService>>(content, new System.Text.Json.JsonSerializerOptions());
                    }
                     
                    return availableServices ?? [];
                }

                throw new FileNotFoundException("Could not find " + availableServiceFilePath);
            }

            throw new FileNotFoundException($"Could not find tesdata folder for langauge {languageId}" );
        }

        public async Task<ServiceResource?> GetServiceResourceFromService(string serviceCode, int serviceEditionCode, CancellationToken cancellationToken)
        {
            List<AvailableService> services = await AvailableServices(1044,false, cancellationToken);
            AvailableService? service = services.FirstOrDefault(r=> r.ExternalServiceCode == serviceCode);

            if(service == null) 
            {
                return null;
            }

            ServiceResource res = new ServiceResource();
            res.Title = new Dictionary<string, string>() { { "nb", service.ServiceName } };

            return res;
        }

        public Task<XacmlPolicy?> GetXacmlPolicy(string serviceCode, int serviceEditionCode, string identifier, CancellationToken cancellationToken)
        {
            string? policyContainerPath = GetPolicyContainerPath();
            if (policyContainerPath != null)
            {
                string resourceId = Path.Combine(policyContainerPath, "altinn_access_management", "resourcepolicy.xml");
                if (File.Exists(resourceId))
                {
                    Stream stream = new FileStream(resourceId, FileMode.Open, FileAccess.Read, FileShare.Read);
                    stream.Position = 0;
                    XacmlPolicy policy;
                    using (XmlReader reader = XmlReader.Create(stream))
                    {
                        policy = XacmlParser.ParseXacmlPolicy(reader);
                    }

                    return Task.FromResult<XacmlPolicy?>(policy);
                }
            }

            return Task.FromResult<XacmlPolicy?>(null);
        }

        private static string? GetAltinn2TestDatafolder()
        {
            string? unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRepositoryMock).Assembly.Location).LocalPath);

            if (unitTestFolder != null)
            {
                return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "Altinn2");
            }

            return null;
        }

        private static string? GetPolicyContainerPath()
        {
            string? unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRepositoryMock).Assembly.Location).LocalPath);
            if (unitTestFolder != null)
            {
                return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "ResourcePolicies");
            }

            return null;
        }

        public Task<DelegationCountOverview> GetDelegationCount(string serviceCode, int serviceEditionCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DelegationCountOverview() { NumberOfDelegations = 13337, NumberOfRelations = 13336 });
        }

        public Task ExportDelegations(ExportDelegationsRequestBE exportDelegationsRequestBE, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
