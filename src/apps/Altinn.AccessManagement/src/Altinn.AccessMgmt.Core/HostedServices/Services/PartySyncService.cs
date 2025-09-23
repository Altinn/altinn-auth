using System.Diagnostics;
using Altinn.AccessMgmt.Core.HostedServices.Contracts;
using Altinn.AccessMgmt.Core.HostedServices.Leases;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.AccessMgmt.PersistenceEF.Utils;
using Altinn.Authorization.Host.Lease;
using Altinn.Authorization.Integration.Platform.Register;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altinn.AccessMgmt.Core.HostedServices.Services;

/// <inheritdoc />
public class PartySyncService : BaseSyncService, IPartySyncService
{
    private readonly ILogger<RegisterHostedService> _logger;
    private readonly IAltinnRegister _register;
    private readonly IServiceProvider _serviceProvider;
    private readonly int _bulkSize = 10_000;

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

    /// <summary>
    /// Synchronizes register data by first acquiring a remote lease and streaming register entries.
    /// Returns if lease is already taken.
    /// </summary>
    /// <param name="lease">The lease result containing the lease data and status.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    public async Task SyncParty(ILease lease, CancellationToken cancellationToken)
    {
        var options = new AuditValues(
            AuditDefaults.RegisterImportSystem,
            AuditDefaults.RegisterImportSystem,
            Activity.Current?.TraceId.ToString() ?? Guid.CreateVersion7().ToString()
        );
        var leaseData = await lease.Get<RegisterLease>(cancellationToken);

        var seen = new HashSet<Guid>();
        var bulk = new List<Entity>();
        var bulkLookup = new List<EntityLookup>();

        using var scope = _serviceProvider.CreateScope();
        var appDbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var ingestService = scope.ServiceProvider.GetRequiredService<IIngestService>();

        await foreach (var page in await _register.StreamParties(AltinnRegisterClient.AvailableFields, leaseData?.PartyStreamNextPageLink, cancellationToken))
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

            _logger.LogInformation("Starting proccessing party page ({0}-{1})", page.Content.Stats.PageStart, page.Content.Stats.PageEnd);

            foreach (var item in page?.Content.Data ?? [])
            {
                try
                {
                    if (item.PartyType.Equals("self-identified-user", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var entity = ConvertPartyModel(item, cancellationToken: cancellationToken);
                    if (entity is { })
                    {
                        if (!seen.Add(entity.Id) || seen.Count > _bulkSize)
                        {
                            await Flush();
                        }

                        bulk.Add(entity);
                        bulkLookup.AddRange(ConvertPartyModelToLookup(item));
                    }
                    else
                    {
                        _logger.LogWarning("skipped adding entity of type '{type}' with id '{id}'", item.PartyType, item.PartyUuid);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "failed to sync party {partyUuid}", item.PartyUuid);
                    throw;
                }
            }

            await Flush();

            if (string.IsNullOrEmpty(page?.Content?.Links?.Next))
            {
                return;
            }

            leaseData.PartyStreamNextPageLink = page.Content.Links.Next;
            await lease.Update(leaseData, cancellationToken);

            async Task Flush()
            {
                var batchId = Guid.CreateVersion7();
                var batchName = batchId.ToString("N");

                try
                {
                    _logger.LogInformation("Ingest and Merge Entity and EntityLookup batch '{0}' to db", batchName);

                    var ingestedEntities = await ingestService.IngestTempData(bulk, batchId, cancellationToken);
                    var ingestedLookups = await ingestService.IngestTempData(bulkLookup, batchId, cancellationToken);

                    if (ingestedEntities != bulk.Count || ingestedLookups != bulkLookup.Count)
                    {
                        _logger.LogWarning("Ingest partial complete: Entity ({0}/{1}) EntityLookup ({2}/{3})", ingestedEntities, bulk.Count, ingestedLookups, bulkLookup.Count);
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
                    bulk.Clear();
                    bulkLookup.Clear();
                    seen.Clear();
                }
            }
        }
    }

    private Entity ConvertPartyModel(PartyModel model, CancellationToken cancellationToken = default)
    {
        if (model.PartyType.Equals("person", StringComparison.OrdinalIgnoreCase))
        {
            return new Entity()
            {
                Id = Guid.Parse(model.PartyUuid),
                Name = model.DisplayName,
                RefId = model.PersonIdentifier,
                TypeId = EntityTypeConstants.Person,
                VariantId = EntityVariantConstants.Person,
            };
        }
        else if (model.PartyType.Equals("organization", StringComparison.OrdinalIgnoreCase))
        {
            if (!EntityVariantConstants.TryGetByName(model.UnitType, out var variant))
            {
                throw new InvalidDataException($"Invalid Unit Type {model.UnitType}");
            }

            return new Entity()
            {
                Id = Guid.Parse(model.PartyUuid),
                Name = model.DisplayName,
                RefId = model.OrganizationIdentifier,
                TypeId = EntityTypeConstants.Organisation,
                VariantId = variant,
            };
        }
        else if (model.PartyType.Equals("self-identified-user", StringComparison.OrdinalIgnoreCase))
        {
            return new Entity()
            {
                Id = Guid.Parse(model.PartyUuid),
                Name = model.DisplayName,
                RefId = model.VersionId.ToString(),
                TypeId = EntityTypeConstants.Person,
                VariantId = EntityVariantConstants.SI
            };
        }

        return null;
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
}
