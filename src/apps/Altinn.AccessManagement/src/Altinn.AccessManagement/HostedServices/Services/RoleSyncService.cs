using Altinn.AccessManagement.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Data;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.Authorization.AccessManagement.HostedServices;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.Register;
using Microsoft.FeatureManagement;

namespace Altinn.AccessManagement.HostedServices.Services;

/// <inheritdoc />
public class RoleSyncService : BaseSyncService, IRoleSyncService
{

    public RoleSyncService(
        IAltinnLease lease,
        IAltinnRegister register,
        ILogger<RoleSyncService> logger,
        IFeatureManager featureManager,
        IIngestService ingestService,
        IRoleRepository roleRepository,
        IProviderRepository providerRepository,
        IAssignmentRepository assignmentRepository,
        IEntityRepository entityRepository,
        IEntityTypeRepository entityTypeRepository
    ) : base(lease, featureManager, register)
    {
        _register = register;
        _logger = logger;
        _ingestService = ingestService;
        _roleRepository = roleRepository;
        _providerRepository = providerRepository;
        _assignmentRepository = assignmentRepository;
        _entityRepository = entityRepository;
        _entityTypeRepository = entityTypeRepository;
    }

    private readonly IAltinnRegister _register;
    private readonly ILogger<RoleSyncService> _logger;
    private readonly IRoleRepository _roleRepository;
    private readonly IProviderRepository _providerRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IEntityRepository _entityRepository;
    private readonly IEntityTypeRepository _entityTypeRepository;
    private readonly IIngestService _ingestService;

    /// <inheritdoc />
    public async Task SyncRoles(LeaseResult<LeaseContent> ls, CancellationToken cancellationToken)
    {
        var batchData = new List<Assignment>();
        Guid batchId = Guid.CreateVersion7();

        var options = new ChangeRequestOptions()
        {
            ChangedBy = AuditDefaults.RegisterImportSystem,
            ChangedBySystem = AuditDefaults.RegisterImportSystem
        };

        OrgType = (await _entityTypeRepository.Get(t => t.Name, "Organisasjon")).FirstOrDefault();
        Provider = (await _providerRepository.Get(t => t.Code, "ccr")).FirstOrDefault();

        await foreach (var page in await _register.StreamRoles([], ls.Data?.RoleStreamNextPageLink, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!page.IsSuccessful)
            {
                Log.ResponseError(_logger, page.StatusCode);
                throw new Exception("Stream page is not successful");
            }

            options.ChangeOperationId = batchId.ToString();
            var batchName = batchId.ToString().ToLower().Replace("-", string.Empty);
            _logger.LogInformation("Starting proccessing role page '{0}'", batchName);

            if (page.Content != null)
            {
                foreach (var item in page.Content.Data)
                {
                    var assignment = await ConvertRoleModel(item, options: options) ?? throw new Exception("Failed to convert RoleModel to Assignment");

                    if (batchData.Any(t => t.FromId == assignment.FromId && t.ToId == assignment.ToId && t.RoleId == assignment.RoleId))
                    {
                        // If changes on same assignment then execute as-is before continuing.
                        await Flush(batchId);
                    }

                    if (item.Type == "Added")
                    {
                        batchData.Add(assignment);
                        if (item.RoleIdentifier == "hovedenhet" || item.RoleIdentifier == "ikke-naeringsdrivende-hovedenhet")
                        {
                            await SetParent(assignment.FromId, assignment.ToId, options: options, cancellationToken: cancellationToken);
                        }
                    }
                    else
                    {
                        var filter = _assignmentRepository.CreateFilterBuilder();
                        filter.Equal(t => t.FromId, assignment.FromId);
                        filter.Equal(t => t.ToId, assignment.ToId);
                        filter.Equal(t => t.RoleId, assignment.RoleId);
                        await _assignmentRepository.Delete(filter, options: options, cancellationToken: cancellationToken);

                        if (item.RoleIdentifier == "hovedenhet" || item.RoleIdentifier == "ikke-naeringsdrivende-hovedenhet")
                        {
                            await RemoveParent(assignment.FromId, options: options, cancellationToken: cancellationToken);
                        }
                    }
                }
            }

            await Flush(batchId);

            if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
            {
                return;
            }

            await UpdateLease(ls, data => data.RoleStreamNextPageLink = page.Content.Links.Next, cancellationToken);

            await Flush(batchId);

            async Task Flush(Guid batchId)
            {
                try
                {
                    _logger.LogInformation("Ingest and Merge Assignment batch '{0}' to db", batchId.ToString());
                    var ingested = await _ingestService.IngestTempData<Assignment>(batchData, batchId, options: options, cancellationToken: cancellationToken);

                    if (ingested != batchData.Count)
                    {
                        _logger.LogWarning("Ingest partial complete: Assignment ({0}/{1})", ingested, batchData.Count);
                    }

                    var merged = await _ingestService.MergeTempData<Assignment>(batchId, options: options, GetAssignmentMergeMatchFilter, cancellationToken: cancellationToken);

                    _logger.LogInformation("Merge complete: Assignment ({0}/{1})", merged, ingested);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to ingest and/or merge Assignment and EntityLookup batch {0} to db", batchId.ToString());
                    throw new Exception(string.Format("Failed to ingest and/or merge Assignment and EntityLookup batch {0} to db", batchId.ToString()), ex);
                }
                finally
                {
                    batchId = Guid.CreateVersion7();
                    batchData.Clear();
                }
            }
        }
    }
    
    private async Task SetParent(Guid childId, Guid parentId, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await _entityRepository.Update(t => t.ParentId, parentId, childId, options: options, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("Unable to set '{1}' as parent to '{0}'", childId, parentId), ex);
        }
    }

    private async Task RemoveParent(Guid childId, ChangeRequestOptions options, CancellationToken cancellationToken = default)
    {
        try
        {
            await _entityRepository.Update(t => t.ParentId, childId, options, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("Unable to remove parent for '{0}'", childId), ex);
        }
    }

    private static readonly IReadOnlyList<string> GetAssignmentMergeMatchFilter = new List<string>() { "fromid", "roleid", "toid" }.AsReadOnly();

    private List<Role> Roles { get; set; } = [];

    private async Task<Assignment> ConvertRoleModel(RoleModel model, ChangeRequestOptions options)
    {
        try
        {
            var role = await GetOrCreateRole(model.RoleIdentifier, model.RoleSource, options: options);
            return new Assignment()
            {
                FromId = Guid.Parse(model.FromParty),
                ToId = Guid.Parse(model.ToParty),
                RoleId = role.Id
            };
        }
        catch
        {
            throw new Exception(string.Format("Failed to convert model to Assignment. From:{0} To:{1} Role:{2}", model.FromParty, model.ToParty, model.RoleIdentifier));
        }
    }

    private async Task<Role> GetOrCreateRole(string roleIdentifier, string roleSource, ChangeRequestOptions options)
    {
        if (Roles.Count(t => t.Code == roleIdentifier) == 1)
        {
            return Roles.First(t => t.Code == roleIdentifier);
        }

        var role = (await _roleRepository.Get(t => t.Code, roleIdentifier)).FirstOrDefault();
        if (role == null)
        {
            await _roleRepository.Create(
                new Role()
                {
                    Id = Guid.CreateVersion7(),
                    Name = roleIdentifier,
                    Description = roleIdentifier,
                    Code = roleIdentifier,
                    Urn = roleIdentifier,
                    EntityTypeId = OrgType.Id,
                    ProviderId = Provider.Id,
                },
                options: options
            );

            role = (await _roleRepository.Get(t => t.Urn, roleIdentifier)).FirstOrDefault();
            if (role == null)
            {
                throw new Exception(string.Format("Unable to get or create role '{0}'", roleIdentifier));
            }
        }

        Roles.Add(role);
        return role;
    }

    private EntityType OrgType { get; set; }

    private Provider Provider { get; set; }

    private List<GenericFilter> assignmentMergeFilter = new List<GenericFilter>()
        {
            new GenericFilter("fromid", "fromid"),
            new GenericFilter("toid", "toid"),
            new GenericFilter("roleid", "roleid"),
        };

}
