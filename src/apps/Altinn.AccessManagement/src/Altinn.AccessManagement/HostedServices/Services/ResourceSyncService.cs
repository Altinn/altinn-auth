using Altinn.AccessManagement.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Data;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.Authorization.AccessManagement.HostedServices;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.Register;
using Altinn.Authorization.Integration.Platform.ResourceRegister;
using Microsoft.FeatureManagement;

namespace Altinn.AccessManagement.HostedServices.Services;

/// <inheritdoc />
public class ResourceSyncService : BaseSyncService, IResourceSyncService
{
    private readonly ILogger<ResourceSyncService> _logger;
    private readonly IAltinnLease _lease;
    private readonly IAltinnResourceRegister _resourceRegister;
    private readonly IIngestService _ingestService;

    private readonly IResourceTypeRepository _resourceTypeRepository;
    private readonly IResourceRepository _resourceRepository;
    private readonly IProviderRepository _providerRepository;
    private readonly IProviderTypeRepository _providerTypeRepository;

    /// <summary>
    /// Constructor
    /// </summary>
    public ResourceSyncService(
        IAltinnLease lease,
        IFeatureManager featureManager,
        IAltinnRegister register,
        IAltinnResourceRegister resourceRegister,
        IIngestService ingestService,
        IResourceTypeRepository resourceTypeRepository,
        IResourceRepository resourceRepository,
        IProviderRepository providerRepository,
        IProviderTypeRepository providerTypeRepository,
        ILogger<ResourceSyncService> logger
        ) : base(lease, featureManager, register)
    {
        _logger = logger;

        _resourceRegister = resourceRegister;
        _ingestService = ingestService;

        _resourceRepository = resourceRepository;
        _resourceTypeRepository = resourceTypeRepository;
        _providerRepository = providerRepository;
        _providerTypeRepository = providerTypeRepository;
    }

    /// <inheritdoc />
    public async Task<bool> SyncResourceOwners(CancellationToken cancellationToken)
    {
        var serviceOwners = await _resourceRegister.GetServiceOwners(cancellationToken);
        if (!serviceOwners.IsSuccessful)
        {
            Log.ServiceOwnerError(_logger, serviceOwners.StatusCode);
            return false;
        }

        var options = new ChangeRequestOptions()
        {
            ChangedBy = AuditDefaults.RegisterImportSystem,
            ChangedBySystem = AuditDefaults.RegisterImportSystem
        };

        var providerType = (await _providerTypeRepository.Get(t => t.Name, "Tjenesteeier")).FirstOrDefault();
        if (providerType == null)
        {
            throw new Exception("Provider type 'ServiceOwner' not found");
        }

        var resourceOwners = new List<Provider>();
        foreach (var serviceOwner in serviceOwners.Content.Orgs)
        {
            // Check if exists without Id => RefId/Code
            resourceOwners.Add(new Provider()
            {
                Id = serviceOwner.Value.Id,
                LogoUrl = serviceOwner.Value.Logo,
                Name = serviceOwner.Value.Name.Nb,
                RefId = serviceOwner.Value.Orgnr,
                Code = serviceOwner.Key,
                TypeId = providerType.Id
            });
        }

        // IngestService will map in Id property and update properties not matchaed
        await _ingestService.IngestAndMergeData(resourceOwners, options: options, ["Id"]);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SyncResources(CancellationToken cancellationToken)
    {
        var response = await _resourceRegister.GetResources(cancellationToken);
        if (!response.IsSuccessful)
        {
            Log.ServiceOwnerError(_logger, response.StatusCode);
            return false;
        }

        var options = new ChangeRequestOptions()
        {
            ChangedBy = AuditDefaults.RegisterImportSystem,
            ChangedBySystem = AuditDefaults.RegisterImportSystem
        };

        var resources = new List<Resource>();
        var failed = new List<ResourceModel>();
        foreach (var res in response.Content)
        {
            //if (res.Status.ToString().Equals("Disabled", StringComparison.OrdinalIgnoreCase))
            //{
            //    Console.WriteLine($"Delete: {res.Identifier}");
            //    continue;
            //}

            try
            {
                resources.Add(await ConvertToResource(res, options, cancellationToken));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                failed.Add(res);
            }
        }

        await _ingestService.IngestAndMergeData(resources, options: options, ["Id"]);

        return true;
    }

    /// <inheritdoc />
    public async Task SyncResourceMapping(LeaseResult<LeaseContent> ls, CancellationToken cancellationToken)
    {
        var options = new ChangeRequestOptions()
        {
            ChangedBy = AuditDefaults.RegisterImportSystem,
            ChangedBySystem = AuditDefaults.RegisterImportSystem
        };

        Providers = (await _providerRepository.Get()).ToList();
        ResourceTypes = (await _resourceTypeRepository.Get()).ToList();

        await foreach (var page in await _resourceRegister.StreamResources(ls.Data?.ResourcesNextPageLink, cancellationToken))
        {
            if (!page.IsSuccessful)
            {
                Log.UpdatedResourceError(_logger, page.StatusCode);
                return;
            }

            var resources = new List<Resource>();
            var failed = new List<Failed>();

            foreach (var updatedResource in page.Content.Data)
            {
                if (updatedResource.Deleted)
                {
                    // Flush and delete from db
                    await Flush();

                    var filter = _resourceRepository.CreateFilterBuilder();
                    filter.Equal(t => t.RefId, updatedResource.ResourceUrn);
                    filter.Equal(t => t.ProviderId, Guid.Empty); // Get provider...
                    await _resourceRepository.Delete(filter, options: options, cancellationToken: cancellationToken);
                }

                var resourceResponse = await _resourceRegister.GetResource(updatedResource.ResourceUrn.Split(":").Last(), cancellationToken);
                if (!resourceResponse.IsSuccessful)
                {
                    throw new Exception(resourceResponse.StatusCode.ToString() + updatedResource.ResourceUrn.Split(":").Last());
                }

                try
                {
                    var resource = await ConvertToResource(resourceResponse.Content, options: options, cancellationToken: cancellationToken);

                    if (!resources.Any(t => t.Id.Equals(resource.Id)))
                    {
                        resources.Add(resource);
                    }
                }
                catch (Exception ex)
                {
                    var f = new Failed();
                    f.Exception = ex;
                    f.UpdatedModel = updatedResource;
                    f.RawResource = resourceResponse.Content;
                    failed.Add(f);

                    if (failed.Count > 10)
                    {
                        break;
                    }
                }
            }

            foreach (var f in failed)
            {
                Console.WriteLine(f.UpdatedModel.ResourceUrn);
                Console.WriteLine(f.Exception.Message);
            }

            await Flush();

            await UpdateLease(ls, data => data.ResourcesNextPageLink = page.Content.Links.Next, cancellationToken);

            async Task Flush()
            {
                await _ingestService.IngestAndMergeData(resources, options: options, ["RefId", "ProviderId"]);
                resources.Clear();
            }
        }
    }

    public IEnumerable<Provider> Providers { get; set; }

    public IEnumerable<ResourceType> ResourceTypes { get; set; }

    private async Task<Resource> ConvertToResource(ResourceModel model, ChangeRequestOptions options, CancellationToken cancellationToken)
    {
        var provider = await GetOrCreateProvider(model, options, cancellationToken) ?? throw new Exception("Unable to get or create provider");
        var resourceType = await GetOrCreateResourceType(model, options, cancellationToken) ?? throw new Exception("Unable to get or create resourcetype");

        var filter = _resourceRepository.CreateFilterBuilder();
        filter.Equal(t => t.RefId, model.Identifier);
        filter.Equal(t => t.ProviderId, provider.Id);
        var res = (await _resourceRepository.Get(filter)).FirstOrDefault();

        if (model.Title == null || string.IsNullOrEmpty(model.Title.Nb))
        {
            throw new Exception("Missing title");
        }

        return new Resource()
        {
            Id = res == null ? Guid.CreateVersion7() : res.Id,
            Name = model.Title.Nb,
            Description = "-",
            RefId = model.Identifier,
            ProviderId = provider.Id,
            TypeId = resourceType.Id
        };
    }

    private async Task LoadResourceTypes()
    {
        ResourceTypes = await _resourceTypeRepository.Get();
    }

    private async Task<ResourceType> GetOrCreateResourceType(ResourceModel model, ChangeRequestOptions options, CancellationToken cancellationToken)
    {
        if (ResourceTypes == null || !ResourceTypes.Any())
        {
            await LoadResourceTypes();
        }

        var type = ResourceTypes.FirstOrDefault(t => t.Name.Equals(model.ResourceType, StringComparison.OrdinalIgnoreCase));

        if (type == null)
        {
            type = new ResourceType()
            {
                Id = Guid.CreateVersion7(),
                Name = model.ResourceType
            };

            await _resourceTypeRepository.Create(type, options: options);
            await LoadResourceTypes();
        }

        return type;
    }

    private async Task LoadProviders()
    {
        Providers = await _providerRepository.Get();
    }

    private async Task<Provider> GetOrCreateProvider(ResourceModel model, ChangeRequestOptions options, CancellationToken cancellationToken)
    {
        if (Providers == null || !Providers.Any())
        {
            await LoadProviders();
        }

        var orgNo = model.HasCompetentAuthority.Organization.ToString() ?? string.Empty;
        var orgCode = model.HasCompetentAuthority.Orgcode.ToString() ?? string.Empty;

        var provider = Providers.Where(t => !string.IsNullOrEmpty(t.RefId)).FirstOrDefault(t => t.RefId.Equals(orgNo, StringComparison.OrdinalIgnoreCase));

        if (provider == null)
        {
            provider = Providers.Where(t => !string.IsNullOrEmpty(t.Code)).FirstOrDefault(t => t.Code.Equals(orgCode, StringComparison.OrdinalIgnoreCase));
        }

        if (provider == null)
        {
            if (model.HasCompetentAuthority != null)
            {
                var providerType = (await _providerTypeRepository.Get(t => t.Name, "Tjenesteeier")).FirstOrDefault();
                if (providerType == null)
                {
                    throw new Exception("Provider type 'ServiceOwner' not found");
                }

                provider = new Provider()
                {
                    Id = Guid.CreateVersion7(),
                    Name = model.HasCompetentAuthority.Name == null ? model.HasCompetentAuthority.Orgcode : model.HasCompetentAuthority.Name.Nb,
                    RefId = model.HasCompetentAuthority.Organization,
                    Code = model.HasCompetentAuthority.Orgcode,
                    TypeId = providerType.Id,
                };
                await _providerRepository.Create(provider, options: options, cancellationToken: cancellationToken);

                //if (model.HasCompetentAuthority.Name != null && model.HasCompetentAuthority.Name.En != null)
                //{
                //    provider = new Provider()
                //    {
                //        Name = model.HasCompetentAuthority.Name == null ? model.HasCompetentAuthority.Orgcode : model.HasCompetentAuthority.Name.En,
                //        RefId = model.HasCompetentAuthority.Orgcode
                //    };

                //    await _providerRepository.CreateTranslation(provider, "eng", options: options, cancellationToken: cancellationToken);
                //}

                //if (model.HasCompetentAuthority.Name != null && model.HasCompetentAuthority.Name.Nn != null)
                //{
                //    provider = new Provider()
                //    {
                //        Name = model.HasCompetentAuthority.Name == null ? model.HasCompetentAuthority.Orgcode : model.HasCompetentAuthority.Name.Nn,
                //        RefId = model.HasCompetentAuthority.Orgcode
                //    };

                //    await _providerRepository.CreateTranslation(provider, "nno", options: options, cancellationToken: cancellationToken);
                //}

                await LoadProviders();
            }
        }

        return provider;
    }
}

internal class Failed
{
    /// <summary>
    /// Model from page
    /// </summary>
    internal ResourceUpdatedModel UpdatedModel { get; set; }
    
    /// <summary>
    /// Single resource from Registry
    /// </summary>
    internal ResourceModel RawResource { get; set; }
    
    /// <summary>
    /// Exception
    /// </summary>
    internal Exception Exception { get; set; }
}
