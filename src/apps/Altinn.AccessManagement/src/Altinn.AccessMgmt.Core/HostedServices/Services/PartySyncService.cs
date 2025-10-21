using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.Register;
using Altinn.Register.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessMgmt.Core.HostedServices.Services;

/// <inheritdoc />
public class PartySyncService : BaseSyncService, IPartySyncService
{
    private readonly ILogger<RegisterHostedService> _logger;
    private readonly IAltinnRegister _register;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// PartySyncService Constructor
    /// </summary>
    public PartySyncService(
        IAltinnRegister register,
        ILogger<RegisterHostedService> logger,
        IServiceProvider serviceProvider
    )
    {
        _register = register;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public async Task SyncParty(ILease lease, bool isInit = false,  CancellationToken cancellationToken = default)
    {
        var options = new AuditValues(SystemEntityConstants.RegisterImportSystem);
        var leaseData = await lease.Get<RegisterLease>(cancellationToken);
        if (isInit == false && leaseData.IsDbIngested == false)
        {
            return;
        }

        var seen = new HashSet<string>();
        var ingestEntities = new List<Entity>();
        var ingestEntitiesLookup = new List<EntityLookup>();

        using var scope = _serviceProvider.CreateEFScope(options);
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ingestService = scope.ServiceProvider.GetRequiredService<IIngestService>();

        await foreach (var page in await _register.StreamParties(AltinnRegisterClient.DefaultFields, leaseData?.PartyStreamNextPageLink, cancellationToken))
        {
            if (page.IsProblem)
            {
                Log.ResponseError(_logger, page.StatusCode);
                throw new Exception("Stream page is not successful");
            }

            _logger.LogInformation("Starting proccessing party page ({0}-{1})", page.Content.Stats.PageStart, page.Content.Stats.PageEnd);

            foreach (var item in page?.Content.Data ?? [])
            {
                var data = item switch
                {
                    Person person => MapPerson(person),
                    Organization organization => MapOrganization(organization),
                    SelfIdentifiedUser selfIdentifiedUser => MapSelfIdentifiedUser(selfIdentifiedUser),
                    SystemUser systemUser => MapSystemUser(systemUser),
                    EnterpriseUser enterpriseUser => MapEnterpriseUser(enterpriseUser),
                    _ => throw new InvalidDataException($"Unkown Party type {item.Type}"),
                };

                if (!seen.Add(data.Entity.RefId))
                {
                    await Flush();
                }

                ingestEntities.Add(data.Entity);
                ingestEntitiesLookup.AddRange(data.EntityLookups);
            }

            await Flush();

            if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
            {
                return;
            }

            leaseData.PartyStreamNextPageLink = page.Content.Links.Next;
            await lease.Update(leaseData, cancellationToken);
        }

        async Task Flush()
        {
            var batchId = Guid.CreateVersion7();
            var batchName = batchId.ToString("N");

            if (ingestEntities.Count == 0 && ingestEntitiesLookup.Count == 0)
            {
                return;
            }

            try
            {
                _logger.LogInformation("Ingest and Merge Entity and EntityLookup batch '{0}' to db", batchName);

                var ingestedEntities = await ingestService.IngestTempData(ingestEntities, batchId, cancellationToken);
                var ingestedLookups = await ingestService.IngestTempData(ingestEntitiesLookup, batchId, cancellationToken);

                if (ingestedEntities != ingestEntities.Count || ingestedLookups != ingestEntitiesLookup.Count)
                {
                    _logger.LogWarning("Ingest partial complete: Entity ({0}/{1}) EntityLookup ({2}/{3})", ingestedEntities, ingestEntities.Count, ingestedLookups, ingestEntitiesLookup.Count);
                }

                var mergedEntities = await ingestService.MergeTempData<Entity>(batchId, options, ["id"], cancellationToken);
                var mergedLookups = await ingestService.MergeTempData<EntityLookup>(batchId, options, ["entityid", "key"], cancellationToken);

                _logger.LogInformation("Merge complete: Entity ({0}/{1}) EntityLookup ({2}/{3})", mergedEntities, ingestedEntities, mergedLookups, ingestedLookups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest and/or merge Entity and EntityLookup batch {0} to db", batchName);
                await Task.Delay(2000, cancellationToken);
            }
            finally
            {
                ingestEntities.Clear();
                ingestEntitiesLookup.Clear();
                seen.Clear();
            }
        }
    }

    private List<EntityLookup> AddDefaultEntityLookups(Party party)
    {
        var result = new List<EntityLookup>();
        if (party.PartyId.HasValue && party.PartyId.Value > 0)
        {
            result.Add(NewEntityLookup("PartyId", party.PartyId.ToString()));
        }

        if (party.User.Value?.UserId.Value is { } userId)
        {
            result.Add(NewEntityLookup("UserId", userId.ToString()));
        }

        if (party.User.Value?.Username.Value is { } username && !string.IsNullOrEmpty(username))
        {
            result.Add(NewEntityLookup("Username", username));
        }

        if (party.DeletedAt.HasValue)
        {
            result.Add(NewEntityLookup("DeletedAt", party.DeletedAt.Value.UtcDateTime.ToString()));
        }

        return result;

        EntityLookup NewEntityLookup(string key, string value)
        {
            return new EntityLookup()
            {
                EntityId = party.Uuid,
                Key = key,
                Value = value,
                IsProtected = false,
            };
        }
    }

    private (Entity Entity, IEnumerable<EntityLookup> EntityLookups) MapPerson(Person person)
    {
        var entity = CreateEntity(person, e =>
        {
            e.DateOfBirth = person.DateOfBirth.HasValue ? person.DateOfBirth.Value : null;
            e.DateOfDeath = person.DateOfDeath.HasValue ? person.DateOfDeath.Value : null;
            e.RefId = person.PersonIdentifier.ToString();
            e.PersonIdentifier = person.PersonIdentifier.ToString();
            e.TypeId = EntityTypeConstants.Person;
            e.VariantId = EntityVariantConstants.Person;
        });

        List<EntityLookup> entityLookups = [
            new()
            {
                EntityId = person.Uuid,
                Key = "DateOfBirth",
                Value = person.DateOfBirth.ToString()
            },
            new()
            {
                EntityId = person.Uuid,
                Key = "PersonIdentifier",
                Value = person.PersonIdentifier.ToString(),
            },
        ];

        entityLookups.AddRange(AddDefaultEntityLookups(person));

        return (entity, entityLookups);
    }

    private (Entity Entity, IEnumerable<EntityLookup> EntityLookups) MapOrganization(Organization organization)
    {
        if (!EntityVariantConstants.TryGetByName(organization.UnitType.Value, out var variant))
        {
            throw new InvalidDataException($"Invalid Unit Type {organization.UnitType}");
        }

        var entity = CreateEntity(organization, o =>
        {
            o.RefId = organization.OrganizationIdentifier.ToString();
            o.OrganizationIdentifier = organization.OrganizationIdentifier.ToString();
            o.VariantId = variant;
            o.TypeId = EntityTypeConstants.Organisation;
        });

        List<EntityLookup> entityLookups = [
            new EntityLookup()
            {
                EntityId = organization.Uuid,
                Key = "OrganizationIdentifier",
                Value = organization.OrganizationIdentifier.ToString(),
            },
        ];

        entityLookups.AddRange(AddDefaultEntityLookups(organization));

        return (entity, entityLookups);
    }

    private (Entity Entity, IEnumerable<EntityLookup> EntityLookups) MapSelfIdentifiedUser(SelfIdentifiedUser selfIdentifiedUser)
    {
        var entity = CreateEntity(selfIdentifiedUser, s =>
        {
            s.RefId = selfIdentifiedUser.User.Value.Username.Value;
            s.TypeId = EntityTypeConstants.SelfIdentified;
            s.VariantId = EntityVariantConstants.SI;
        });

        List<EntityLookup> entityLookups = [];
        entityLookups.AddRange(AddDefaultEntityLookups(selfIdentifiedUser));

        return (entity, entityLookups);
    }

    private (Entity Entity, IEnumerable<EntityLookup> EntityLookups) MapSystemUser(SystemUser systemUser)
    {
        var systemTypeVariant = systemUser.SystemUserType.Value.Value switch
        {
            SystemUserType.ClientPartySystemUser => EntityVariantConstants.AgentSystem,
            SystemUserType.FirstPartySystemUser => EntityVariantConstants.StandardSystem,
            _ => throw new InvalidDataException($"Missing mapping for system type {systemUser.SystemUserType}")
        };

        var entity = CreateEntity(systemUser, s =>
        {
            s.RefId = systemUser.Uuid.ToString();
            s.TypeId = EntityTypeConstants.SystemUser;
            s.VariantId = systemTypeVariant;
        });
        return (entity, []);
    }

    private (Entity Entity, IEnumerable<EntityLookup> EntityLookups) MapEnterpriseUser(EnterpriseUser enterpriseUser)
    {
        var entity = CreateEntity(enterpriseUser, e =>
        {
            e.RefId = enterpriseUser.User.Value.Username.Value;
            e.TypeId = EntityTypeConstants.EnterpriseUser;
            e.VariantId = EntityVariantConstants.EnterpriseUser;
        });

        List<EntityLookup> entityLookups = [];
        entityLookups.AddRange(AddDefaultEntityLookups(enterpriseUser));
        return (entity, entityLookups);
    }

    private Entity CreateEntity(Party party, Action<Entity> configureEntity)
    {
        var entity = new Entity()
        {
            Id = party.Uuid,
            Name = party.DisplayName.ToString(),
            DeletedAt = party?.DeletedAt.HasValue == true ? party.DeletedAt.Value : null,
            PartyId = party?.PartyId.HasValue == true ? Convert.ToInt32(party.PartyId.Value) : null,
            UserId = party?.User.Value?.UserId.HasValue == true ? Convert.ToInt32(party.User.Value.UserId.Value) : null,
            Username = party?.User.Value?.Username.HasValue == true ? party.User.Value.Username.ToString() : null,
            IsDeleted = party?.IsDeleted.HasValue == true ? party.IsDeleted.Value : false,
        };

        configureEntity(entity);
        return entity;
    }
}
