using Altinn.Platform.Storage.Interface.Models;
using Altinn.ResourceRegistry.Core.Services.Interfaces;

namespace Altinn.ResourceRegistry.Tests.Mocks
{
    public class ApplicationsClientMock : IApplications
    {
        public async Task<ApplicationList> GetApplicationList(CancellationToken cancellationToken)
        {
            string? testdataFolder = GetAltinn2TestDatafolder();
            if(testdataFolder != null)
            {
                string applicationsFilePath = Path.Combine(testdataFolder, $"applications.json");

                ApplicationList? applicationList = new ApplicationList();

                if (File.Exists(applicationsFilePath))
                {
                    string content = await File.ReadAllTextAsync(applicationsFilePath);
                    if (!string.IsNullOrEmpty(content))
                    {
                        applicationList = System.Text.Json.JsonSerializer.Deserialize<ApplicationList>(content, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase });
                    }
                
                    if(applicationList == null)
                    {
                        applicationList = new ApplicationList();
                    }

                    return applicationList;
                }

                throw new FileNotFoundException("Could not find " + applicationsFilePath);
            }

            throw new FileNotFoundException("Could not find testdatafolder");
        }

        private static string? GetAltinn2TestDatafolder()
        {
            string? unitTestFolder = Path.GetDirectoryName(new Uri(typeof(PolicyRepositoryMock).Assembly.Location).LocalPath);
            if (unitTestFolder != null)
            {
                return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "Altinn3Storage");
            }

            return null;
        }
    }
}
