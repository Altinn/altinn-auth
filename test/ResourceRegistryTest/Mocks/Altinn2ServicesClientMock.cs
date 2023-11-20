using Altinn.Authorization.ABAC.Utils;
using Altinn.Authorization.ABAC.Xacml;
using Altinn.ResourceRegistry.Core.Models;
using Altinn.ResourceRegistry.Core.Models.Altinn2;
using Altinn.ResourceRegistry.Core.Services;
using Castle.Components.DictionaryAdapter.Xml;
using FluentAssertions.Common;
using Microsoft.VisualStudio.TestPlatform.PlatformAbstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Altinn.ResourceRegistry.Tests.Mocks
{
    public class Altinn2ServicesClientMock : IAltinn2Services
    {
        public async Task<List<AvailableService>> AvailableServices(int languageId)
        {
            string availableServiceFilePath = Path.Combine(GetAltinn2TestDatafolder(), $"availableServices{languageId}.json");

            List<AvailableService>? availableServices = null;

            if (File.Exists(availableServiceFilePath))
            {
                string content = await File.ReadAllTextAsync(availableServiceFilePath);
                if (!string.IsNullOrEmpty(content))
                {
                    availableServices = System.Text.Json.JsonSerializer.Deserialize<List<AvailableService>>(content, new System.Text.Json.JsonSerializerOptions());
                }

                if(availableServices == null)
                {
                    availableServices = new List<AvailableService>();
                }

                return availableServices;
            }

            throw new FileNotFoundException("Could not find " + availableServiceFilePath);
        }

        public async Task<ServiceResource> GetServiceResourceFromService(string serviceCode, int serviceEditionCode)
        {
            List<AvailableService> services = await AvailableServices(1044);
            AvailableService? service = services.FirstOrDefault(r=> r.ExternalServiceCode == serviceCode);

            if(service == null) 
            {
                return null;
            }

            ServiceResource res = new ServiceResource();
            res.Title = new Dictionary<string, string>() { { "nb", service.ServiceName } };

            return res;
        }

        public Task<XacmlPolicy> GetXacmlPolicy(string serviceCode, int serviceEditionCode, string identifier)
        {
            string resourceId = Path.Combine(GetPolicyContainerPath(), "altinn_access_management", "resourcepolicy.xml");
            if (File.Exists(resourceId))
            {
                Stream stream =  new FileStream(resourceId, FileMode.Open, FileAccess.Read, FileShare.Read);
                stream.Position = 0;
                XacmlPolicy policy;
                using (XmlReader reader = XmlReader.Create(stream))
                {
                    policy = XacmlParser.ParseXacmlPolicy(reader);
                }
                
                return Task.FromResult(policy);
            }

            return null;
        }

        private static string GetAltinn2TestDatafolder()
        {
            string? unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRepositoryMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "Altinn2");
        }

        private static string GetPolicyContainerPath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRepositoryMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "ResourcePolicies");
        }
    }
}
