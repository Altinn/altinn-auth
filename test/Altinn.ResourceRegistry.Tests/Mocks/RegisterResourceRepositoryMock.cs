using Altinn.ResourceRegistry.Core;
using Altinn.ResourceRegistry.Core.Extensions;
using Altinn.ResourceRegistry.Core.Models;

namespace Altinn.ResourceRegistry.Tests.Mocks
{
    public class RegisterResourceRepositoryMock : IResourceRegistryRepository
    {
        public async Task<ServiceResource> CreateResource(ServiceResource resource, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult<ServiceResource>(resource);
        }

        public async Task<ServiceResource> UpdateResource(ServiceResource resource, CancellationToken cancellationToken = default)
        {
            return await Task.FromResult<ServiceResource>(resource);
        }

        public async Task<ServiceResource?> DeleteResource(string id, CancellationToken cancellationToken = default)
        {
            return await GetResource(id);
        }

        public async Task<ServiceResource?> GetResource(string id, CancellationToken cancellationToken = default)
        {
            string? resourcePath = GetResourcePath(id);
            resourcePath.AsFilePath(true);
            if (File.Exists(resourcePath))
            {
                string content = await System.IO.File.ReadAllTextAsync(resourcePath, cancellationToken);
                ServiceResource? resource = System.Text.Json.JsonSerializer.Deserialize<ServiceResource>(content, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }) as ServiceResource;

                return resource;
            }

            return null;
        }

        public async Task<List<ServiceResource>> Search(ResourceSearch resourceSearch, CancellationToken cancellationToken = default)
        {
            List<ServiceResource> resources = new List<ServiceResource>();
            string? resourcePath = GetResourcePath();
            resourcePath.AsFilePath(true);
            if (resourcePath != null)
            { 
                string[] files =  Directory.GetFiles(resourcePath);
                if(files != null)
                {
                    foreach (string file in files)
                    {
                        string content = await System.IO.File.ReadAllTextAsync(file, cancellationToken);
                        ServiceResource? resource = System.Text.Json.JsonSerializer.Deserialize<ServiceResource>(content, new System.Text.Json.JsonSerializerOptions() { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }) as ServiceResource;
                        if (resource != null)
                        {
                            resources.Add(resource);
                        }
                    }
                }
            }

            return resources;
        }

        private static string? GetResourcePath(string id)
        {
            string? resourcePath = GetResourcePath();
            if (resourcePath != null)
            {
                return Path.Combine(resourcePath, id + ".json");
            }

            return null;
        }

        private static string? GetResourcePath()
        {
            string? unitTestFolder = Path.GetDirectoryName(new Uri(typeof(RegisterResourceRepositoryMock).Assembly.Location).LocalPath);
            if (unitTestFolder != null)
            {
                return Path.Combine(unitTestFolder, "..", "..", "..", "Data", "Resources");
            }

            return null;
        }

        public Task<List<SubjectResources>> FindResourcesForSubjects(IEnumerable<string> subjects, CancellationToken cancellationToken = default)
        {
            List<SubjectResources> resources = new List<SubjectResources>();
            resources.Add(GetSubjectResource("urn:altinn:rolecode:utinn", new List<string>{ "urn:altinn:resource:skd_mva", "urn:altinn:resource:skd_ax" }));
            return Task.FromResult(resources);
        }

        /// <inheritdoc/>
        public Task<List<ResourceSubjects>> FindSubjectsForResources(IEnumerable<string> resources, CancellationToken cancellationToken = default)
        {
            List<ResourceSubjects> resourceSubjects = new List<ResourceSubjects>();
            resourceSubjects.Add(GetResourceSubjects("urn:altinn:resource:skd_mva", new List<string> { "urn:altinn:rolecode:utinn", "urn:altinn:rolecode:dagl" }, "urn:altinn:org:skd"));
            return Task.FromResult(resourceSubjects);
        }


        private static SubjectResources GetSubjectResource(string subjectUrn, List<string> resources)
        {
             AttributeMatchV2 subjectMatch = new AttributeMatchV2 {
                Type = subjectUrn.Substring(0, subjectUrn.LastIndexOf(':')),
                Value = subjectUrn.Substring(subjectUrn.LastIndexOf(':') + 1),
                Urn = subjectUrn};

            SubjectResources subjectResources = new SubjectResources
                {
                   Subject = subjectMatch,
                   Resources = new List<AttributeMatchV2>()
                };


            subjectResources.Resources = new List<AttributeMatchV2>();
            foreach(string resource in resources)
            {
                subjectResources.Resources.Add(
                    new AttributeMatchV2 {
                        Type = resource.Substring(0, resource.LastIndexOf(':')),
                        Value = resource.Substring(resource.LastIndexOf(':') + 1),
                        Urn = resource});
               
            }
            return subjectResources;
        }

        private static ResourceSubjects GetResourceSubjects(string resourceUrn, List<string> subjects, string owner)
        {
            AttributeMatchV2 resourceMatch = new AttributeMatchV2 {
                    Type = resourceUrn.Substring(0, resourceUrn.LastIndexOf(':')),
                    Value = resourceUrn.Substring(resourceUrn.LastIndexOf(':') + 1),
                    Urn = resourceUrn};

            List<AttributeMatchV2> subjectMatches = new List<AttributeMatchV2>();

            foreach (string subject in subjects)
            {
                subjectMatches.Add(new AttributeMatchV2 {
                                    Type = subject.Substring(0, subject.LastIndexOf(':')),
                                    Value = subject.Substring(subject.LastIndexOf(':') + 1),
                                    Urn = subject});
            }

            return new ResourceSubjects
            { 
                Resource =  resourceMatch, 
                Subjects =  subjectMatches, 
                ResourceOwner = owner 
            };
        }

        public Task SetResourceSubjects(ResourceSubjects resourceSubjects, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
