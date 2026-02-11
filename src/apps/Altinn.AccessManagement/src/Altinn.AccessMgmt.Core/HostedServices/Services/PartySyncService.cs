using System.Diagnostics;
using System.Net.Http.Headers;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.PersistenceEF.Audit;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.Register;
using Altinn.Authorization.ModelUtils;
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
    public async Task SyncParty(ILease lease, bool isInit = false, CancellationToken cancellationToken = default)
    {
        var options = new AuditValues(SystemEntityConstants.RegisterImportSystem);
        var leaseData = await lease.Get<RegisterLease>(cancellationToken);
        if (isInit == false && leaseData.IsDbIngested == false)
        {
            return;
        }

        var seen = new HashSet<string>();
        var ingestEntities = new List<Entity>();
        HashSet<Guid> seenDeadPeople = [];

        using IServiceScope scope = _serviceProvider.CreateEFScope(options);
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ingestService = scope.ServiceProvider.GetRequiredService<IIngestService>();
        IAssignmentService assignmentService = scope.ServiceProvider.GetRequiredService<IAssignmentService>();

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
                var entity = item switch
                {
                    Person person => MapPerson(person),
                    Organization organization => MapOrganization(organization),
                    SelfIdentifiedUser selfIdentifiedUser => MapSelfIdentifiedUser(selfIdentifiedUser),
                    SystemUser systemUser => MapSystemUser(systemUser),
                    EnterpriseUser enterpriseUser => MapEnterpriseUser(enterpriseUser),
                    _ => throw new InvalidDataException($"Unkown Party type {item.Type}"),
                };

                if (!seen.Add(entity.RefId))
                {
                    await Flush();
                }

                if (entity.TypeId == EntityTypeConstants.Person && entity.DateOfDeath.HasValue)
                {
                    seenDeadPeople.Add(entity.Id);
                }

                ingestEntities.Add(entity);
            }

            var flushed = await Flush();

            if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
            {
                return;
            }

            if (flushed > 0)
            {
                leaseData.PartyStreamNextPageLink = page.Content.Links.Next;
                await lease.Update(leaseData, cancellationToken);
            }
        }

        async Task<int> Flush()
        {
            var batchId = Guid.CreateVersion7();
            var batchName = batchId.ToString("N");

            if (ingestEntities.Count == 0)
            {
                return 0;
            }

            try
            {
                _logger.LogInformation("Ingest and Merge Entity batch '{0}' to db", batchName);

                var ingestedEntities = await ingestService.IngestTempData(ingestEntities, batchId, cancellationToken);

                if (seenDeadPeople.Count > 0)
                {
                    foreach (Guid refId in seenDeadPeople)
                    {
                        await assignmentService.ClearAssignmentsInAfterLife(refId, options,  cancellationToken);
                    }
                }

                if (ingestedEntities != ingestEntities.Count)
                {
                    _logger.LogWarning("Ingest partial complete: Entity ({0}/{1})", ingestedEntities, ingestEntities.Count);
                }

                var mergedEntities = await ingestService.MergeTempData<Entity>(batchId, options, matchColumns: ["id"], ignoreColumns: ["parentid"], cancellationToken);

                _logger.LogInformation("Merge complete: Entity ({0}/{1})", mergedEntities, ingestedEntities);
                return mergedEntities;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ingest and/or merge Entity batch {0} to db", batchName);
                await Task.Delay(2000, cancellationToken);
            }
            finally
            {
                ingestEntities.Clear();
                seen.Clear();
                seenDeadPeople.Clear();
            }

            return 0;
        }
    }

    private Entity MapPerson(Person person)
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

        return entity;
    }

    private Entity MapOrganization(Organization organization)
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
            o.TypeId = EntityTypeConstants.Organization;
        });

        return entity;
    }

    private Entity MapSelfIdentifiedUser(SelfIdentifiedUser selfIdentifiedUser)
    {
        FieldValue<NonExhaustiveEnum<SelfIdentifiedUserType>> userType = selfIdentifiedUser.SelfIdentifiedUserType;
        var entity = CreateEntity(selfIdentifiedUser, s =>
        {
            s.RefId = selfIdentifiedUser.User.Value.Username.Value;
            s.TypeId = EntityTypeConstants.SelfIdentified;
            s.EmailIdentifier = selfIdentifiedUser.Email.Value;
            s.VariantId = userType switch
            {
                { IsNull: true } => EntityVariantConstants.SI,
                { IsUnset: true } => throw new UnreachableException("Missing selfidentfiedUserType from register. Should never happen."),
                { Value.IsUnknown: true } => throw new InvalidOperationException("..."),
                { Value.Value: SelfIdentifiedUserType rrr } => rrr switch 
                {
                    SelfIdentifiedUserType.Legacy => EntityVariantConstants.SI,
                    SelfIdentifiedUserType.Educational => EntityVariantConstants.SI_EDU,
                    SelfIdentifiedUserType.IdPortenEmail => EntityVariantConstants.SI_EMAIL,
                    _ => throw new InvalidDataException($"Missing mapping for SI user type {rrr}")
                },
            };
        });

        return entity;
    }

    private Entity MapSystemUser(SystemUser systemUser)
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

        return entity;
    }

    private Entity MapEnterpriseUser(EnterpriseUser enterpriseUser)
    {
        var entity = CreateEntity(enterpriseUser, e =>
        {
            e.RefId = enterpriseUser.User.Value.Username.Value;
            e.TypeId = EntityTypeConstants.EnterpriseUser;
            e.VariantId = EntityVariantConstants.EnterpriseUser;
        });

        return entity;
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
