using System.Linq.Expressions;
using Altinn.AccessManagement.HostedServices.Contracts;
using Altinn.AccessManagement.HostedServices.Leases;
using Altinn.AccessMgmt.Core.Models;
using Altinn.AccessMgmt.Persistence.Core.Contracts;
using Altinn.AccessMgmt.Persistence.Core.Models;
using Altinn.AccessMgmt.Persistence.Data;
using Altinn.AccessMgmt.Persistence.Models;
using Altinn.AccessMgmt.Persistence.Repositories;
using Altinn.AccessMgmt.Persistence.Repositories.Contracts;
using Altinn.Authorization.AccessManagement;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.Register;
using Microsoft.FeatureManagement;

namespace Altinn.AccessManagement.HostedServices.Services;

/// <inheritdoc />
public class PartySyncService : BaseSyncService, IPartySyncService
{
    private readonly ILogger<RegisterHostedService> _logger;
    private readonly IIngestService _ingestService;
    private readonly IAltinnRegister _register;
    private readonly IEntityTypeRepository _entityTypeRepository;
    private readonly IEntityVariantRepository _entityVariantRepository;
    private readonly IEntityLookupRepository _lookupRepository;

    /// <summary>
    /// PartySyncService Constructor
    /// </summary>
    public PartySyncService(
        IAltinnLease lease,
        IFeatureManager featureManager,
        IAltinnRegister register,
        ILogger<RegisterHostedService> logger,
        IIngestService ingestService,
        IEntityLookupRepository lookupRepository,
        IEntityTypeRepository entityTypeRepository,
        IEntityVariantRepository entityVariantRepository
    ) : base(lease, featureManager)
    {
        _register = register;
        _logger = logger;
        _lookupRepository = lookupRepository;
        _ingestService = ingestService;
        _entityVariantRepository = entityVariantRepository;
        _entityTypeRepository = entityTypeRepository;
    }

    /// <summary>
    /// Synchronizes register data by first acquiring a remote lease and streaming register entries.
    /// Returns if lease is already taken.
    /// </summary>
    /// <param name="ls">The lease result containing the lease data and status.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    public async Task SyncParty(LeaseResult<RegisterLease> ls, CancellationToken cancellationToken)
    {
        var options = new ChangeRequestOptions()
        {
            ChangedBy = AuditDefaults.RegisterImportSystem,
            ChangedBySystem = AuditDefaults.RegisterImportSystem
        };

        var bulk = new List<Entity>();
        var bulkLookup = new List<EntityLookup>();

        EntityTypes = (await _entityTypeRepository.Get(cancellationToken: cancellationToken)).ToList();
        EntityVariants = (await _entityVariantRepository.Get(cancellationToken: cancellationToken)).ToList();

        await foreach (var page in await _register.StreamParties(AltinnRegisterClient.AvailableFields, ls.Data?.PartyStreamNextPageLink, cancellationToken))
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
            options.ChangeOperationId = batchId.ToString();
            var batchName = batchId.ToString().ToLower().Replace("-", string.Empty);
            _logger.LogInformation("Starting proccessing party page '{0}'", batchName);

            foreach (var item in page?.Content.Data ?? [])
            {
                try
                {
                    if (item.PartyType.Equals("self-identified-user", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var entity = ConvertPartyModel(item, options: options, cancellationToken: cancellationToken);

                    if (bulk.Count(t => t.Id.Equals(entity.Id)) > 0)
                    {
                        await Flush(batchId);
                    }

                    // UpsertEntityLookup(model, options, cancellationToken: cancellationToken);

                    bulk.Add(entity);
                    bulkLookup.AddRange(ConvertPartyModelToLookup(item));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "failed to sync party {partyUuid}", item.PartyUuid);
                    throw;
                }
            }

            await Flush(batchId);

            if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
            {
                return;
            }

            await UpdateLease(ls, data => data.PartyStreamNextPageLink = page.Content.Links.Next, cancellationToken);

            async Task Flush(Guid batchId)
            {
                try
                {
                    _logger.LogInformation("Ingest and Merge Entity and EntityLookup batch '{0}' to db", batchName);

                    var ingestedEntities = await _ingestService.IngestTempData<Entity>(bulk, batchId, options: options);
                    var ingestedLookups = await _ingestService.IngestTempData<EntityLookup>(bulkLookup, batchId, options: options);

                    if (ingestedEntities != bulk.Count || ingestedLookups != bulkLookup.Count)
                    {
                        _logger.LogWarning("Ingest partial complete: Entity ({0}/{1}) EntityLookup ({2}/{3})", ingestedEntities, bulk.Count, ingestedLookups, bulkLookup.Count);
                    }

                    var mergedEntities = await _ingestService.MergeTempData<Entity>(batchId, options: options, GetEntityMergeMatchFilter);
                    var mergedLookups = await _ingestService.MergeTempData<EntityLookup>(batchId, options: options, GetEntityLookupMergeMatchFilter);

                    _logger.LogInformation("Merge complete: Entity ({0}/{1}) EntityLookup ({2}/{3})", mergedEntities, ingestedEntities, mergedLookups, ingestedLookups);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to ingest and/or merge Entity and EntityLookup batch {0} to db", batchName);
                    await Task.Delay(2000, cancellationToken);
                }
                finally
                {
                    bulk.Clear();
                    bulkLookup.Clear();
                }
            }
        }
    }

    private static readonly IReadOnlyList<string> GetEntityMergeMatchFilter = new List<string>() { "id" }.AsReadOnly();

    private static readonly IReadOnlyList<string> GetEntityLookupMergeMatchFilter = new List<string>() { "entityid", "key" }.AsReadOnly();

    private Entity ConvertPartyModel(PartyModel model, ChangeRequestOptions options, bool createTypeIfMissing = false, CancellationToken cancellationToken = default)
    {
        if (model.PartyType.Equals("person", StringComparison.OrdinalIgnoreCase))
        {
            var type = EntityTypes.FirstOrDefault(t => t.Name == "Person") ?? throw new Exception(string.Format("Unable to find type '{0}'", "Person"));
            var variant = EntityVariants.FirstOrDefault(t => t.TypeId == type.Id && t.Name == "Person");
            if (variant == null)
            {
                variant = new EntityVariant() { Id = Guid.NewGuid(), Name = model.UnitType, Description = "Unknown", TypeId = type.Id };
                var res = _entityVariantRepository.Create(variant, options: options, cancellationToken: cancellationToken).Result;
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
                var res = _entityVariantRepository.Create(variant, options: options, cancellationToken: cancellationToken).Result;
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
                var res = _entityVariantRepository.Create(variant, options: options, cancellationToken: cancellationToken).Result;
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
                    var res = _entityVariantRepository.Create(variant, options: options, cancellationToken: cancellationToken).Result;
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
                Value = model.PersonIdentifier,
                IsProtected = true
            });
            if (model.User is { UserId: > 0 })
            {
                res.Add(new EntityLookup()
                {
                    EntityId = Guid.Parse(model.PartyUuid),
                    Key = "UserId",
                    Value = model.User.UserId.ToString(),
                    IsProtected = false,
                });
            }

            if (model.IsDeleted)
            {
                // DeletedAt missing in register. (18.juni. 2025)
                // res.Add(new EntityLookup()
                // {
                //     EntityId = Guid.Parse(model.PartyUuid),
                //     Key = "DeletedAt",
                //     Value = model.DeletedAt.ToUniversalTime().ToString(),
                //     IsProtected = false,
                // });
            }
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

    private List<EntityType> EntityTypes { get; set; } = [];

    private List<EntityVariant> EntityVariants { get; set; } = [];
}
