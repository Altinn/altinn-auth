using System.Net;
using Altinn.AccessManagement;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.Register;
using Microsoft.FeatureManagement;
using Role = Altinn.AccessMgmt.Core.Models.Role;

namespace Altinn.Authorization.AccessManagement;

/// <summary>
/// A hosted service responsible for synchronizing register data using leases.
/// </summary>
/// <param name="lease">Lease provider for distributed locking.</param>
/// <param name="register">Register integration service.</param>
/// <param name="logger">Logger for logging service activities.</param>
/// <param name="featureManager">for reading feature flags</param>
/// <param name="entityRepository">Repository for entity data.</param>
/// <param name="entityLookupRepository">Repository for entity lookup data.</param>
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
    IEntityRepository entityRepository,
    IEntityLookupRepository entityLookupRepository,
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
    private readonly IEntityRepository entityRepository = entityRepository;
    private readonly IEntityLookupRepository entityLookupRepository = entityLookupRepository;
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
        var cancellationToken = (CancellationToken)state;

        await using var ls = await _lease.TryAquireNonBlocking<LeaseContent>("access_management_register_sync", cancellationToken);
        if (!ls.HasLease || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            if (await _featureManager.IsEnabledAsync(AccessManagementFeatureFlags.HostedServicesRegisterSync))
            {
                await PrepareSync();
                await SyncParty(ls, cancellationToken);
                await SyncRoles(ls, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Log.SyncError(_logger, ex);
            return;
        }
        finally
        {
            await _lease.Release(ls, default);
        }
    }

    /// <summary>
    /// Synchronizes register data by first acquiring a remote lease and streaming register entries.
    /// Returns if lease is already taken.
    /// </summary>
    /// <param name="ls">The lease result containing the lease data and status.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    private async Task SyncRoles(LeaseResult<LeaseContent> ls, CancellationToken cancellationToken)
    {
        await foreach (var page in await _register.StreamRoles([], ls.Data?.RoleStreamNextPageLink, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!page.IsSuccessful)
            {
                Log.ResponseError(_logger, page.StatusCode);
            }

            foreach (var item in page.Content.Data)
            {
                // TODO: one for party, one for role
                Interlocked.Increment(ref _executionCount);
                Log.Role(_logger, item.FromParty, item.ToParty, item.RoleIdentifier);
                await WriteRolesToDb(item);
            }

            if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
            {
                return;
            }

            await _lease.Put(ls, new() { RoleStreamNextPageLink = page.Content.Links.Next, PartyStreamNextPageLink = ls.Data?.PartyStreamNextPageLink }, cancellationToken);
            await _lease.RefreshLease(ls, cancellationToken);
        }
    }

    /// <summary>
    /// Synchronizes register data by first acquiring a remote lease and streaming register entries.
    /// Returns if lease is already taken.
    /// </summary>
    /// <param name="ls">The lease result containing the lease data and status.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    private async Task SyncParty(LeaseResult<LeaseContent> ls, CancellationToken cancellationToken)
    {
        await foreach (var page in await _register.StreamParties(RegisterClient.AvailableFields, ls.Data?.PartyStreamNextPageLink, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            if (!page.IsSuccessful)
            {
                Log.ResponseError(_logger, page.StatusCode);
            }

            foreach (var item in page.Content.Data)
            {
                Interlocked.Increment(ref _executionCount);
                Log.Party(_logger, item.PartyUuid, _executionCount);
                await WritePartyToDb(item);
            }

            if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
            {
                return;
            }

            await _lease.Put(ls, new() { PartyStreamNextPageLink = page.Content.Links.Next, RoleStreamNextPageLink = ls.Data?.RoleStreamNextPageLink }, cancellationToken);
            await _lease.RefreshLease(ls, cancellationToken);
        }
    }

    private async Task PrepareSync()
    {
        EntityTypes = await entityTypeRepository.Get();
        EntityVariants = await entityVariantRepository.Get();
    }

    /// <summary>
    /// Writes the synchronized register data to the database.
    /// </summary>
    /// <param name="model">Party model containing register data.</param>
    /// <returns>A completed task.</returns>
    public async Task WritePartyToDb(PartyModel model)
    {
        try
        {
            var entity = ConvertPartyModel(model);
            await entityRepository.Upsert(entity);

            if (model.PartyType == "Person")
            {
                await entityLookupRepository.Upsert(new EntityLookup()
                {
                    Id = Guid.NewGuid(),
                    EntityId = Guid.Parse(model.PartyUuid),
                    Key = "PII",
                    Value = model.PersonIdentifier
                });
            }

            if (model.PartyType == "Organisasjon")
            {
                await entityLookupRepository.Upsert(new EntityLookup()
                {
                    Id = Guid.NewGuid(),
                    EntityId = Guid.Parse(model.PartyUuid),
                    Key = "OrgNo",
                    Value = model.OrganizationIdentifier
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to WritePartyToDb");
            Console.WriteLine(ex.ToString());
        }
    }

    private List<GenericFilter> entityLookupMergeFilter = new List<GenericFilter>()
        {
            new GenericFilter("EntityId", "EntityId"),
            new GenericFilter("Key", "Key"),
            new GenericFilter("Value", "Value"),
        };

    private Entity ConvertPartyModel(PartyModel model)
    {
        if (model.PartyType.Equals("Person", StringComparison.OrdinalIgnoreCase))
        {
            var type = EntityTypes.FirstOrDefault(t => t.Name == "Person") ?? throw new Exception(string.Format("Unable to find type '{0}'", "Person"));
            var variant = EntityVariants.FirstOrDefault(t => t.TypeId == type.Id && t.Name == "Person") ?? throw new Exception(string.Format("Unable to fint variant '{0}' for type '{1}'", "Person", "Organisasjon"));

            return new Entity()
            {
                Id = Guid.Parse(model.PartyUuid),
                Name = model.DisplayName,
                RefId = model.DateOfBirth.ToString(),
                TypeId = type.Id,
                VariantId = variant.Id
            };
        }
        else if (model.PartyType.Equals("Organisasjon", StringComparison.OrdinalIgnoreCase))
        {
            var type = EntityTypes.FirstOrDefault(t => t.Name == "Organisasjon") ?? throw new Exception(string.Format("Unable to find type '{0}'", "Organisasjon"));
            var variant = EntityVariants.FirstOrDefault(t => t.TypeId == type.Id && t.Name.Equals(model.UnitType, StringComparison.OrdinalIgnoreCase)) ?? throw new Exception(string.Format("Unable to fint variant '{0}' for type '{1}'", model.PartyType, "Organisasjon"));

            return new Entity()
            {
                Id = Guid.Parse(model.PartyUuid),
                Name = model.DisplayName,
                RefId = model.OrganizationIdentifier,
                TypeId = type.Id,
                VariantId = variant.Id
            };
        }
        else
        {
            var type = EntityTypes.FirstOrDefault(t => t.Name.Equals(model.PartyType, StringComparison.OrdinalIgnoreCase)) ?? throw new Exception(string.Format("Unable to find type '{0}'", model.PartyType));
            var variant = EntityVariants.FirstOrDefault(t => t.TypeId == type.Id && t.Name.Equals(model.UnitType, StringComparison.OrdinalIgnoreCase)) ?? throw new Exception(string.Format("Unable to fint variant '{0}' for type '{1}'", model.PartyType, model.UnitType));

            return new Entity()
            {
                Id = Guid.Parse(model.PartyUuid),
                Name = model.DisplayName,
                RefId = model.OrganizationIdentifier,
                TypeId = type.Id,
                VariantId = variant.Id
            };
        }
    }

    private IEnumerable<EntityType> EntityTypes { get; set; }

    private IEnumerable<EntityVariant> EntityVariants { get; set; }

    /// <summary>
    /// Writes the synchronized register data to the database.
    /// </summary>
    /// <param name="model">Role model containing register data.</param>
    /// <returns>A completed task.</returns>
    public async Task WriteRolesToDb(RoleModel model)
    {
        var role = await GetOrCreateRole(model.RoleIdentifier, model.RoleSource);
        await assignmentRepository.Upsert(ConvertRoleModel(model, role.Id), assignmentMergeFilter);
    }

    private Assignment ConvertRoleModel(RoleModel model, Guid roleId)
    {
        return new Assignment()
        {
            Id = Guid.NewGuid(),
            FromId = Guid.Parse(model.FromParty),
            ToId = Guid.Parse(model.ToParty),
            RoleId = roleId
        };
    }

    private async Task<Role> GetOrCreateRole(string roleIdentifier, string roleSource)
    {
        var role = (await roleRepository.Get(t => t.Urn, roleIdentifier)).FirstOrDefault();
        if (role == null)
        {
            var provider = (await providerRepository.Get(t => t.Name, roleSource == "ccr" ? "Brønnøysundregistrene" : "Digdir")).FirstOrDefault() ?? throw new Exception(string.Format("Provider '{0}' not found while creating new role.", roleSource));
            var entityType = (await entityTypeRepository.Get(t => t.Name, "Organisasjon")).FirstOrDefault() ?? throw new Exception(string.Format("Unable to get type for '{0}'", "Organisasjon"));

            await roleRepository.Create(new Role()
            {
                Id = Guid.NewGuid(),
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
                throw new Exception(string.Format("Unable to get or create role ''", roleIdentifier));
            }
        }

        return role;
    }

    private List<GenericFilter> assignmentMergeFilter = new List<GenericFilter>()
        {
            new GenericFilter("fromid", "fromid"),
            new GenericFilter("toid", "toid"),
            new GenericFilter("roleid", "roleid"),
        };

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
        /// The URL of the next page of Role data.
        /// </summary>
        public string RoleStreamNextPageLink { get; set; }
    }

    private static partial class Log
    {
        [LoggerMessage(EventId = 9, Level = LogLevel.Information, Message = "Error occured while fetching data from register, got {statusCode}")]
        internal static partial void ResponseError(ILogger logger, HttpStatusCode statusCode);

        [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "Processing party with uuid {partyUuid} from register. Count {count}")]
        internal static partial void Party(ILogger logger, string partyUuid, int count);

        [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "An error occured while streaming data from register")]
        internal static partial void SyncError(ILogger logger, Exception ex);

        [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Starting register hosted service")]
        internal static partial void StartRegisterSync(ILogger logger);

        [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "Quit register hosted service")]
        internal static partial void QuitRegisterSync(ILogger logger);

        [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Party {fromParty} assigned role {roleIdentifier} to {toParty}")]
        internal static partial void Role(ILogger logger, string fromParty, string toParty, string roleIdentifier);
    }
}
