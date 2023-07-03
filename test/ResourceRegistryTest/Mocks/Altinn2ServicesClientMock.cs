using Altinn.ResourceRegistry.Core.Models.Altinn2;
using Altinn.ResourceRegistry.Core.Services;
using Castle.Components.DictionaryAdapter.Xml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


        private static string GetAltinn2TestDatafolder()
        {
            string? unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRepositoryMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "Altinn2");
        }
    }
}
