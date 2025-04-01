using System.Net;
using Altinn.AccessManagement;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.AccessMgmt.Persistence.Services;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.Register;
using Microsoft.FeatureManagement;

namespace Altinn.Authorization.AccessManagement;

/// <summary>
/// A hosted service responsible for synchronizing register data using leases.
/// </summary>
/// <param name="lease">Lease provider for distributed locking.</param>
/// <param name="register">Register integration service.</param>
/// <param name="logger">Logger for logging service activities.</param>
/// <param name="featureManager">for reading feature flags</param>
/// <param name="ingestService">Ingest service</param>
/// <param name="statusService">Status service</param>
/// <param name="entityRepository">Repository for entity data.</param>
/// <param name="roleRepository">Repository for role data.</param>
/// <param name="assignmentRepository">Repository for assignment data.</param>
/// <param name="entityTypeRepository">Repository for entity type data.</param>
/// <param name="entityVariantRepository">Repository for entity variant data.</param>
/// <param name="providerRepository">Repository for provider data.</param>
public partial class RegisterHostedService(
    IAltinnLease lease,
    IAltinnRegister register,
    ILogger<RegisterHostedService> logger,
    IFeatureManager featureManager,
    IIngestService ingestService,
    IStatusService statusService,
    IEntityRepository entityRepository,
    IRoleRepository roleRepository,
    IAssignmentRepository assignmentRepository,
    IEntityTypeRepository entityTypeRepository,
    IEntityVariantRepository entityVariantRepository,
    IProviderRepository providerRepository
    ) : IHostedService, IDisposable
{
    private readonly IAltinnLease _lease = lease;
    private readonly IAltinnRegister _register = register;
    private readonly ILogger<RegisterHostedService> _logger = logger;
    private readonly IFeatureManager _featureManager = featureManager;
    private readonly IIngestService ingestService = ingestService;
    private readonly IStatusService statusService = statusService;
    private readonly IEntityRepository entityRepository = entityRepository;
    private readonly IRoleRepository roleRepository = roleRepository;
    private readonly IAssignmentRepository assignmentRepository = assignmentRepository;
    private readonly IEntityTypeRepository entityTypeRepository = entityTypeRepository;
    private readonly IEntityVariantRepository entityVariantRepository = entityVariantRepository;
    private readonly IProviderRepository providerRepository = providerRepository;
    private int _executionCount = 0;
    private Timer _timer = null;
    private readonly CancellationTokenSource _stop = new();

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        Log.StartRegisterSync(_logger);

        _timer = new Timer(async state => await SyncRegisterDispatcher(state), _stop.Token, TimeSpan.Zero, TimeSpan.FromMinutes(2));

        return Task.CompletedTask;
    }

    /// <summary>
    /// Dispatches the register synchronization process in a separate task.
    /// </summary>
    /// <param name="state">Cancellation token for stopping execution.</param>
    private async Task SyncRegisterDispatcher(object state)
    {
        if (!await _featureManager.IsEnabledAsync(AccessManagementFeatureFlags.HostedServicesRegisterSync))
        {
            return;
        }

        var cancellationToken = (CancellationToken)state;
        await using var ls = await _lease.TryAquireNonBlocking<LeaseContent>("access_management_register_sync", cancellationToken);
        if (!ls.HasLease || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            var partyStatus = await statusService.GetOrCreateRecord(Guid.Parse("C18B67F6-B07E-482C-AB11-7FE12CD1F48D"), "accessmgmt-sync-register-party", 5);
            var roleStatus = await statusService.GetOrCreateRecord(Guid.Parse("84E9726D-E61B-4DFF-91D7-9E17C8BB41A6"), "accessmgmt-sync-register-role", 5);

            bool canRunPartySync = await statusService.TryToRun(partyStatus);
            bool canRunRoleSync = await statusService.TryToRun(roleStatus);

            if (!canRunPartySync && !canRunRoleSync)
            {
                return;
            }

            try
            {
                await PrepareSync();
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
                return;
            }

            try
            {
                if (canRunPartySync)
                {
                    await SyncParty(ls, cancellationToken);
                    await statusService.RunSuccess(partyStatus);
                }
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
                await statusService.RunFailed(partyStatus, ex);
            }

            try
            {
                if (canRunPartySync)
                {
                    await SyncRoles(ls, cancellationToken);
                    await statusService.RunSuccess(roleStatus);
                }
            }
            catch (Exception ex)
            {
                Log.SyncError(_logger, ex);
                await statusService.RunFailed(partyStatus, ex);
            }

            _logger.LogInformation("Register sync completed!");
        }
        catch (Exception ex)
        {
            Log.SyncError(_logger, ex);
        }
        finally
        {
            await _lease.Release(ls, default);
        }
    }
    
    private async Task PrepareSync()
    {
        EntityTypes = [.. await entityTypeRepository.Get()];
        EntityVariants = [.. await entityVariantRepository.Get()];
        Roles = [.. await roleRepository.Get()];
        Providers = [.. await providerRepository.Get()];
    }

    #region Roles

    private async Task SyncRoles(LeaseResult<LeaseContent> ls, CancellationToken cancellationToken)
    {
        var batchData = new List<Assignment>();

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

            Guid batchId = Guid.CreateVersion7();
            var batchName = batchId.ToString().ToLower().Replace("-", string.Empty);
            _logger.LogInformation("Starting proccessing role page '{0}'", batchName);

            if (page.Content != null)
            {
                foreach (var item in page.Content.Data)
                {
                    var assignment = await ConvertRoleModel(item) ?? throw new Exception("Failed to convert RoleModel to Assignment");

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
                            await SetParent(assignment.FromId, assignment.ToId, cancellationToken);
                        }
                    }
                    else
                    {
                        var filter = assignmentRepository.CreateFilterBuilder();
                        filter.Equal(t => t.FromId, assignment.FromId);
                        filter.Equal(t => t.ToId, assignment.ToId);
                        filter.Equal(t => t.RoleId, assignment.RoleId);
                        await assignmentRepository.Delete(filter);

                        if (item.RoleIdentifier == "hovedenhet" || item.RoleIdentifier == "ikke-naeringsdrivende-hovedenhet")
                        {
                            await RemoveParent(assignment.FromId, cancellationToken);
                        }
                    }

                    Interlocked.Increment(ref _executionCount);
                }
            }

            await Flush(batchId);

            if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
            {
                return;
            }

            await _lease.Put(ls, new() { RoleStreamNextPageLink = page.Content.Links.Next, PartyStreamNextPageLink = ls.Data?.PartyStreamNextPageLink }, cancellationToken);
            await _lease.RefreshLease(ls, cancellationToken);

            async Task Flush(Guid batchId)
            {
                try
                {
                    _logger.LogInformation("Ingest and Merge Assignment batch '{0}' to db", batchName);
                    var ingested = await ingestService.IngestTempData<Assignment>(batchData, batchId, cancellationToken);

                    if (ingested != batchData.Count)
                    {
                        _logger.LogWarning("Ingest partial complete: Assignment ({0}/{1})", ingested, batchData.Count);
                    }

                    var merged = await ingestService.MergeTempData<Assignment>(batchId, GetAssignmentMergeMatchFilter, cancellationToken);

                    _logger.LogInformation("Merge complete: Assignment ({0}/{1})", merged, ingested);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to ingest and/or merge Assignment and EntityLookup batch {0} to db", batchName);
                    throw new Exception(string.Format("Failed to ingest and/or merge Assignment and EntityLookup batch {0} to db", batchName), ex);
                }
                finally
                {
                    batchId = Guid.CreateVersion7();
                    batchData.Clear();
                }
            }
        }
    }

    private async Task SyncRolesSingle(LeaseResult<LeaseContent> ls, CancellationToken cancellationToken)
    {
        var failedAdds = new List<Assignment>();
        var failedRemoves = new List<Assignment>();

        await foreach (var page in await _register.StreamRoles([], ls.Data?.RoleStreamNextPageLink, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!page.IsSuccessful || page.Content == null)
            {
                Log.ResponseError(_logger, page.StatusCode);
            }

            foreach (var item in page.Content.Data)
            {
                var assignment = await ConvertRoleModel(item) ?? throw new Exception("Failed to convert RoleModel to Assignment");
                if (item.Type == "Added")
                {
                    try
                    {
                        var res = await assignmentRepository.Create(assignment);
                        Log.AssignmentSuccess(_logger, "added", item.FromParty, item.ToParty, item.RoleIdentifier);
                    } 
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to add assignment");
                        Log.AssignmentFailed(_logger, "add", assignment.FromId.ToString(), assignment.ToId.ToString(), assignment.RoleId.ToString());
                        failedAdds.Add(assignment);
                    }
                }
                else
                {
                    try
                    {
                        var filter = assignmentRepository.CreateFilterBuilder();
                        filter.Equal(t => t.FromId, assignment.FromId);
                        filter.Equal(t => t.ToId, assignment.ToId);
                        filter.Equal(t => t.RoleId, assignment.RoleId);
                        var res = await assignmentRepository.Delete(filter);
                        Log.AssignmentSuccess(_logger, "removed", item.FromParty, item.ToParty, item.RoleIdentifier);
                    } 
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to remove assignment");
                        Log.AssignmentFailed(_logger, "remove", assignment.FromId.ToString(), assignment.ToId.ToString(), assignment.RoleId.ToString());
                        failedRemoves.Add(assignment);
                    }
                }

                Interlocked.Increment(ref _executionCount);
            }

            if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
            {
                return;
            }

            await _lease.Put(ls, new() { RoleStreamNextPageLink = page.Content.Links.Next, PartyStreamNextPageLink = ls.Data?.PartyStreamNextPageLink }, cancellationToken);
            await _lease.RefreshLease(ls, cancellationToken);
        }

        _logger.LogInformation("Role import failures;");
        foreach (var ass in failedAdds)
        {
            _logger.LogInformation("Failed to {0} assingment from '{1}' to '{2}' with role '{3}'", "add", ass.FromId, ass.ToId, ass.RoleId);
        }

        foreach (var ass in failedRemoves)
        {
            _logger.LogInformation("Failed to {0} assingment from '{1}' to '{2}' with role '{3}'", "remove", ass.FromId, ass.ToId, ass.RoleId);
        }
    }
    
    private async Task SyncRolesBatched(LeaseResult<LeaseContent> ls, CancellationToken cancellationToken)
    {
        int batchSize = 1000;
        Guid batchId = Guid.CreateVersion7();
        var batchData = new List<Assignment>();

        await foreach (var page in await _register.StreamRoles([], ls.Data?.RoleStreamNextPageLink, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!page.IsSuccessful || page.Content == null)
            {
                Log.ResponseError(_logger, page.StatusCode);
            }

            foreach (var item in page.Content.Data)
            {
                try
                {
                    var assignment = await ConvertRoleModel(item) ?? throw new Exception("Failed to convert RoleModel to Assignment");
                    
                    if (batchData.Any(t => t.FromId == assignment.FromId && t.ToId == assignment.ToId && t.RoleId == assignment.RoleId))
                    {
                        // If changes on same assignment then execute as-is before continuing.
                        await FlushBatchAsync(cancellationToken);
                    }

                    if (item.Type == "Added")
                    {
                        try
                        {
                            batchData.Add(assignment);
                            if (item.RoleIdentifier == "hovedenhet" || item.RoleIdentifier == "ikke-naeringsdrivende-hovedenhet")
                            {
                                await SetParent(assignment.FromId, assignment.ToId, cancellationToken);
                            }

                            Log.AssignmentSuccess(_logger, "added", item.FromParty, item.ToParty, item.RoleIdentifier);
                        }
                        catch
                        {
                            Log.AssignmentFailed(_logger, "add", item.FromParty, item.ToParty, item.RoleIdentifier);
                        }
                    }
                    else
                    {
                        try
                        {
                            var filter = assignmentRepository.CreateFilterBuilder();
                            filter.Equal(t => t.FromId, assignment.FromId);
                            filter.Equal(t => t.ToId, assignment.ToId);
                            filter.Equal(t => t.RoleId, assignment.RoleId);
                            await assignmentRepository.Delete(filter);

                            if (item.RoleIdentifier == "hovedenhet" || item.RoleIdentifier == "ikke-naeringsdrivende-hovedenhet")
                            {
                                await RemoveParent(assignment.FromId, cancellationToken);
                            }

                            Log.AssignmentSuccess(_logger, "removed", item.FromParty, item.ToParty, item.RoleIdentifier);
                        }
                        catch
                        {
                            Log.AssignmentFailed(_logger, "remove", item.FromParty, item.ToParty, item.RoleIdentifier);
                        }

                    }

                    Interlocked.Increment(ref _executionCount);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Faild to ingest assignments");
                }
            }

            if (batchData.Count >= batchSize)
            {
                await FlushBatchAsync(cancellationToken);
            }

            if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
            {
                return;
            }

            await _lease.Put(ls, new() { RoleStreamNextPageLink = page.Content.Links.Next, PartyStreamNextPageLink = ls.Data?.PartyStreamNextPageLink }, cancellationToken);
            await _lease.RefreshLease(ls, cancellationToken);
        }

        if (batchData.Count > 0)
        {
            await FlushBatchAsync(cancellationToken);
        }

        async Task FlushBatchAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                Console.WriteLine("Write batchData to db");
                await ingestService.IngestTempData<Assignment>(batchData, batchId, cancellationToken);

                Console.WriteLine("Merge batchData to db");
                await ingestService.MergeTempData<Assignment>(batchId, GetAssignmentMergeMatchFilter, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to merge batchData '{batchId}'", batchId);
                await Task.Delay(2000);
            }

            batchId = Guid.CreateVersion7();
            batchData.Clear();
        }
    }

    private async Task SetParent(Guid childId, Guid parentId, CancellationToken cancellationToken = default)
    {
        try
        {
            await entityRepository.Update(t => t.ParentId, parentId, childId, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("Unable to set '{1}' as parent to '{0}'", childId, parentId), ex);
        }
    }

    private async Task RemoveParent(Guid childId, CancellationToken cancellationToken = default)
    {
        try
        {
            await entityRepository.Update(t => t.ParentId, childId, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new Exception(string.Format("Unable to remove parent for '{0}'", childId), ex);
        }
    }

    private static readonly IReadOnlyList<GenericParameter> GetAssignmentMergeMatchFilter = new List<GenericParameter>()
    {
        new GenericParameter("fromid", "fromid"),
        new GenericParameter("roleid", "roleid"),
        new GenericParameter("toid", "toid")
    }.AsReadOnly();
    
    private List<Role> Roles { get; set; } = [];

    private async Task<Assignment> ConvertRoleModel(RoleModel model)
    {
        try
        {
            var role = await GetOrCreateRole(model.RoleIdentifier, model.RoleSource);
            return new Assignment()
            {
                FromId = Guid.Parse(model.FromParty),
                ToId = Guid.Parse(model.ToParty),
                RoleId = role.Id
            };
        } 
        catch (Exception ex)
        {
            throw new Exception(string.Format("Failed to convert model to Assignment. From:{0} To:{1} Role:{2}", model.FromParty, model.ToParty, model.RoleIdentifier));
        }
    }
    
    private async Task<Role> GetOrCreateRole(string roleIdentifier, string roleSource)
    {
        if (Roles.Count(t => t.Code == roleIdentifier) == 1)
        {
            return Roles.First(t => t.Code == roleIdentifier);
        }

        var role = (await roleRepository.Get(t => t.Urn, roleIdentifier)).FirstOrDefault();
        if (role == null)
        {
            var provider = Providers.FirstOrDefault(t => t.Name == (roleSource == "ccr" ? "Brønnøysundregistrene" : "Digitaliseringsdirektoratet")) ?? throw new Exception(string.Format("Provider '{0}' not found while creating new role.", roleSource));
            var entityType = EntityTypes.FirstOrDefault(t => t.Name == "Organisasjon") ?? throw new Exception(string.Format("Unable to find type '{0}'", "Organisasjon"));

            await roleRepository.Create(new Role()
            {
                Id = Guid.CreateVersion7(),
                Name = roleIdentifier,
                Description = roleIdentifier,
                Code = roleIdentifier,
                Urn = roleIdentifier,
                EntityTypeId = entityType.Id,
                ProviderId = provider.Id,
            });

            role = (await roleRepository.Get(t => t.Urn, roleIdentifier)).FirstOrDefault();
            if (role == null)
            {
                throw new Exception(string.Format("Unable to get or create role '{0}'", roleIdentifier));
            }
        }

        Roles.Add(role);
        return role;
    }
    
    private List<GenericFilter> assignmentMergeFilter = new List<GenericFilter>()
        {
            new GenericFilter("fromid", "fromid"),
            new GenericFilter("toid", "toid"),
            new GenericFilter("roleid", "roleid"),
        };
    #endregion

    #region Party

    /// <summary>
    /// Synchronizes register data by first acquiring a remote lease and streaming register entries.
    /// Returns if lease is already taken.
    /// </summary>
    /// <param name="ls">The lease result containing the lease data and status.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    private async Task SyncParty(LeaseResult<LeaseContent> ls, CancellationToken cancellationToken)
    {        
        var bulk = new List<Entity>();
        var bulkLookup = new List<EntityLookup>();

        await foreach (var page in await _register.StreamParties(RegisterClient.AvailableFields, ls.Data?.PartyStreamNextPageLink, cancellationToken))
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
            
            Guid batchId = Guid.CreateVersion7();
            var batchName = batchId.ToString().ToLower().Replace("-", string.Empty);
            _logger.LogInformation("Starting proccessing party page '{0}'", batchName);

            if (page.Content != null)
            {
                foreach (var item in page.Content.Data)
                {
                    var entity = ConvertPartyModel(item);
                    
                    if (bulk.Count(t => t.Id.Equals(entity.Id)) > 0)
                    {
                        await Flush(batchId);
                    }

                    bulk.Add(entity);
                    bulkLookup.AddRange(ConvertPartyModelToLookup(item));

                    Interlocked.Increment(ref _executionCount);
                }
            }

            await Flush(batchId);

            if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
            {
                return;
            }

            await _lease.Put(ls, new() { PartyStreamNextPageLink = page.Content.Links.Next, RoleStreamNextPageLink = ls.Data?.RoleStreamNextPageLink }, cancellationToken);
            await _lease.RefreshLease(ls, cancellationToken);

            async Task Flush(Guid batchId)
            {
                try
                {
                    _logger.LogInformation("Ingest and Merge Entity and EntityLookup batch '{0}' to db", batchName);

                    var ingestedEntities = await ingestService.IngestTempData<Entity>(bulk, batchId);
                    var ingestedLookups = await ingestService.IngestTempData<EntityLookup>(bulkLookup, batchId);

                    if (ingestedEntities != bulk.Count || ingestedLookups != bulkLookup.Count)
                    {
                        _logger.LogWarning("Ingest partial complete: Entity ({0}/{1}) EntityLookup ({2}/{3})", ingestedEntities, bulk.Count, ingestedLookups, bulkLookup.Count);
                    }

                    var mergedEntities = await ingestService.MergeTempData<Entity>(batchId, GetEntityMergeMatchFilter);
                    var mergedLookups = await ingestService.MergeTempData<EntityLookup>(batchId, GetEntityLookupMergeMatchFilter);

                    _logger.LogInformation("Merge complete: Entity ({0}/{1}) EntityLookup ({2}/{3})", mergedEntities, ingestedEntities, mergedLookups, ingestedLookups);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to ingest and/or merge Entity and EntityLookup batch {0} to db", batchName);
                    await Task.Delay(2000);
                }
                finally
                {
                    bulk.Clear();
                    bulkLookup.Clear();
                }
            }
        }
    }

    private static readonly IReadOnlyList<GenericParameter> GetEntityMergeMatchFilter = new List<GenericParameter>()
    {
        new GenericParameter("id", "id")
    }.AsReadOnly();

    private static readonly IReadOnlyList<GenericParameter> GetEntityLookupMergeMatchFilter = new List<GenericParameter>()
    {
        new GenericParameter("entityid", "entityid"),
        new GenericParameter("key", "key"),
    }.AsReadOnly();

    private Entity ConvertPartyModel(PartyModel model, bool createTypeIfMissing = false)
    {
        if (model.PartyType.Equals("person", StringComparison.OrdinalIgnoreCase))
        {
            var type = EntityTypes.FirstOrDefault(t => t.Name == "Person") ?? throw new Exception(string.Format("Unable to find type '{0}'", "Person"));
            var variant = EntityVariants.FirstOrDefault(t => t.TypeId == type.Id && t.Name == "Person");
            if (variant == null)
            {
                variant = new EntityVariant() { Id = Guid.NewGuid(), Name = model.UnitType, Description = "Unknown", TypeId = type.Id };
                var res = entityVariantRepository.Create(variant).Result;
                if (res == 0)
                {
                    throw new Exception(string.Format("Unable to find or create variant '{0}' for type '{1}'", model.UnitType, type.Name));
                }

                EntityVariants.Add(variant);
            }

            return new Entity()
            {
                Id = Guid.Parse(model.PartyUuid),
                Name = model.DisplayName,
                RefId = model.PersonIdentifier,
                TypeId = type.Id,
                VariantId = variant.Id
            };
        }
        else if (model.PartyType.Equals("organization", StringComparison.OrdinalIgnoreCase))
        {
            var type = EntityTypes.FirstOrDefault(t => t.Name == "Organisasjon") ?? throw new Exception(string.Format("Unable to find type '{0}'", "Organisasjon"));
            var variant = EntityVariants.FirstOrDefault(t => t.TypeId == type.Id && t.Name.Equals(model.UnitType, StringComparison.OrdinalIgnoreCase));
            if (variant == null)
            {
                variant = new EntityVariant() { Id = Guid.NewGuid(), Name = model.UnitType, Description = "Unknown", TypeId = type.Id };
                var res = entityVariantRepository.Create(variant).Result;
                if (res == 0)
                {
                    throw new Exception(string.Format("Unable to find or create variant '{0}' for type '{1}'", model.UnitType, type.Name));
                }

                EntityVariants.Add(variant);
            }

            return new Entity()
            {
                Id = Guid.Parse(model.PartyUuid),
                Name = model.DisplayName,
                RefId = model.OrganizationIdentifier,
                TypeId = type.Id,
                VariantId = variant.Id
            };
        }
        else if (model.PartyType.Equals("self-identified-user", StringComparison.OrdinalIgnoreCase))
        {
            var type = EntityTypes.FirstOrDefault(t => t.Name == "Person") ?? throw new Exception(string.Format("Unable to find type '{0}'", "Person"));
            var variant = EntityVariants.FirstOrDefault(t => t.TypeId == type.Id && t.Name == "SI") ?? throw new Exception(string.Format("Unable to find variant '{0}' for type '{1}'", "SI", type.Name));
            if (variant == null)
            {
                variant = new EntityVariant() { Id = Guid.NewGuid(), Name = model.UnitType, Description = "Unknown", TypeId = type.Id };
                var res = entityVariantRepository.Create(variant).Result;
                if (res == 0)
                {
                    throw new Exception(string.Format("Unable to find or create variant '{0}' for type '{1}'", model.UnitType, type.Name));
                }

                EntityVariants.Add(variant);
            }

            return new Entity()
            {
                Id = Guid.Parse(model.PartyUuid),
                Name = model.DisplayName,
                RefId = model.VersionId.ToString(),
                TypeId = type.Id,
                VariantId = variant.Id
            };
        }
        else
        {
            if (createTypeIfMissing)
            {
                var type = EntityTypes.FirstOrDefault(t => t.Name.Equals(model.PartyType, StringComparison.OrdinalIgnoreCase));
                if (type == null)
                {
                    type = EntityTypes.FirstOrDefault(t => t.Name.Equals("Ukjent", StringComparison.OrdinalIgnoreCase)) ?? throw new Exception("EntityType 'Ukjent' not found");
                }

                var variant = EntityVariants.FirstOrDefault(t => t.TypeId == type.Id && t.Name.Equals(model.UnitType, StringComparison.OrdinalIgnoreCase)) ?? throw new Exception(string.Format("Unable to fint variant '{0}' for type '{1}'", model.PartyType, model.UnitType));
                if (variant == null)
                {
                    variant = new EntityVariant() { Id = Guid.NewGuid(), Name = model.UnitType, Description = "Unknown", TypeId = type.Id };
                    var res = entityVariantRepository.Create(variant).Result;
                    if (res == 0)
                    {
                        throw new Exception(string.Format("Unable to create variant '{0}' for type '{1}'", model.UnitType, type.Name));
                    }

                    EntityVariants.Add(variant);
                }

                return new Entity()
                {
                    Id = Guid.Parse(model.PartyUuid),
                    Name = model.DisplayName,
                    RefId = string.Empty,
                    TypeId = type.Id,
                    VariantId = variant.Id
                };
            }
            else
            {
                throw new Exception(string.Format("Unable to find type '{0}' and variant '{1}'", model.PartyType, model.UnitType));
            }
        }
       
    }

    private List<EntityLookup> ConvertPartyModelToLookup(PartyModel model)
    {
        var res = new List<EntityLookup>();

        if (model.PartyType.Equals("person", StringComparison.OrdinalIgnoreCase))
        {
            res.Add(new EntityLookup()
            {
                EntityId = Guid.Parse(model.PartyUuid),
                Key = "DateOfBirth",
                Value = model.DateOfBirth
            });

            res.Add(new EntityLookup()
            {
                EntityId = Guid.Parse(model.PartyUuid),
                Key = "PartyId",
                Value = model.PartyId.ToString()
            });

            res.Add(new EntityLookup()
            {
                EntityId = Guid.Parse(model.PartyUuid),
                Key = "PersonIdentifier",
                Value = model.PersonIdentifier
            });
        }
        else if (model.PartyType.Equals("organization", StringComparison.OrdinalIgnoreCase))
        {
            res.Add(new EntityLookup()
            {
                EntityId = Guid.Parse(model.PartyUuid),
                Key = "PartyId",
                Value = model.PartyId.ToString()
            });

            res.Add(new EntityLookup()
            {
                EntityId = Guid.Parse(model.PartyUuid),
                Key = "OrganizationIdentifier",
                Value = model.OrganizationIdentifier
            });
        }
        else if (model.PartyType.Equals("self-identified-user", StringComparison.OrdinalIgnoreCase))
        {
            res.Add(new EntityLookup()
            {
                EntityId = Guid.Parse(model.PartyUuid),
                Key = "PartyId",
                Value = model.PartyId.ToString()
            });
        }

        return res;
    }

    private List<Provider> Providers { get; set; } = [];

    private List<EntityType> EntityTypes { get; set; } = [];

    private List<EntityVariant> EntityVariants { get; set; } = [];
    #endregion

    #region Base

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            Log.QuitRegisterSync(_logger);
        }
        finally
        {
            _timer?.Change(Timeout.Infinite, 0);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases unmanaged resources.
    /// </summary>
    /// <param name="disposing">Indicates whether the method is called from Dispose.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer?.Dispose();
            _stop?.Cancel();
            _stop?.Dispose();
        }
    }

    /// <summary>
    /// Represents lease content, including pagination link.
    /// </summary>
    public class LeaseContent()
    {
        /// <summary>
        /// The URL of the next page of Party data.
        /// </summary>
        public string PartyStreamNextPageLink { get; set; }

        /// <summary>
        /// The URL of the next page of AssignmentSuccess data.
        /// </summary>
        public string RoleStreamNextPageLink { get; set; }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Error occured while fetching data from register, got {statusCode}")]
        internal static partial void ResponseError(ILogger logger, HttpStatusCode statusCode);

        [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Processing party with uuid {partyUuid} from register. RetryCount {count}")]
        internal static partial void Party(ILogger logger, string partyUuid, int count);

        [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "An error occured while streaming data from register")]
        internal static partial void SyncError(ILogger logger, Exception ex);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Starting register hosted service")]
        internal static partial void StartRegisterSync(ILogger logger);

        [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Quit register hosted service")]
        internal static partial void QuitRegisterSync(ILogger logger);

        [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Assignment {action} from '{from}' to '{to}' with role '{role}'")]
        internal static partial void AssignmentSuccess(ILogger logger, string action, string from, string to, string role);

        [LoggerMessage(EventId = 5, Level = LogLevel.Warning, Message = "Failed to {action} assingment from '{from}' to '{to}' with role '{role}'")]
        internal static partial void AssignmentFailed(ILogger logger, string action, string from, string to, string role);
    }

    #endregion
}
