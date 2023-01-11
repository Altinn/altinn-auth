using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Tests.Mocks
{
    public class RegisterResourceRepositoryMock : IResourceRegistryRepository
    {
        public async Task<ServiceResource> CreateResource(ServiceResource resource)
        {
            return await Task.FromResult<ServiceResource>(null);
        }

        public async Task<ServiceResource> UpdateResource(ServiceResource resource)
        {
            return await Task.FromResult<ServiceResource>(resource);
        }

        public async Task<ServiceResource> DeleteResource(string id)
        {
            return await GetResource(id);
        }

        public async Task<ServiceResource> GetResource(string id)
        {
            string resourcePath = GetResourcePath(id);
            if (File.Exists(resourcePath))
            {
                try
                {
                    string content = System.IO.File.ReadAllText(resourcePath);
                    ServiceResource? resource = System.Text.Json.JsonSerializer.Deserialize<ServiceResource>(content, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }) as ServiceResource;
                    return resource;
                }
                catch(Exception ex)
                {
                    throw;
                }
            }

            return null;
        }

        public async Task<List<ServiceResource>> Search(ResourceSearch resourceSearch)
        {
            List<ServiceResource> resources = new List<ServiceResource>();
            string[] files =  Directory.GetFiles(GetResourcePath());
            if(files != null)
            {
                foreach (string file in files)
                {
                    string content = System.IO.File.ReadAllText(file);
                    ServiceResource? resource = System.Text.Json.JsonSerializer.Deserialize<ServiceResource>(content, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }) as ServiceResource;
                    resources.Add(resource);
                }
            }

            return resources;
        }

        private static string GetResourcePath(string id)
        {
            return Path.Combine(GetResourcePath(), id + ".json");
        }

        private static string GetResourcePath()
        {
            string unitTestFolder = Path.GetDirectoryName(new Uri(typeof(RegisterResourceRepositoryMock).Assembly.Location).LocalPath);
            return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "Resources");
        }
    }
}
