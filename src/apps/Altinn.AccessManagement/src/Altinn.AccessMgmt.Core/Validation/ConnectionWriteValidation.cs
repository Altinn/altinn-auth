using System.Diagnostics;
using Altinn.AccessMgmt.Core.Services;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Validation;

/// <summary>
/// Shared helpers for the "look up From/To entities and validate them against <see cref="ConnectionOptions"/>"
/// pre-amble that every write-side connection operation performs (<see cref="ConnectionService"/>'s package /
/// resource / rightholder mutations and <see cref="ServiceOwnerConnectionService"/>'s package upsert).
/// </summary>
internal static class ConnectionWriteValidation
{
    /// <summary>
    /// Look up the From and To entities in a single round-trip. Either id may be null; the returned tuple
    /// element is null for the corresponding side when the entity does not exist or no id was provided.
    /// </summary>
    public static async Task<(Entity From, Entity To)> GetFromAndToEntitiesAsync(AppDbContext dbContext, Guid? fromId, Guid? toId, CancellationToken cancellationToken)
    {
        if (fromId is null && toId is null)
        {
            throw new UnreachableException();
        }

        var entities = await dbContext.Entities
            .AsNoTracking()
            .Where(e => e.Id == fromId || e.Id == toId)
            .Include(e => e.Type)
            .ToListAsync(cancellationToken);

        var fromEntity = entities.FirstOrDefault(e => e.Id == fromId);
        var toEntity = entities.FirstOrDefault(e => e.Id == toId);

        return (fromEntity, toEntity);
    }

    /// <summary>
    /// Validate that the From / To entities exist and (if the caller supplied <see cref="ConnectionOptions"/>
    /// constraints) that their types are on the allow-list for write operations. Returns null on success,
    /// a populated <see cref="ValidationProblemInstance"/> otherwise.
    /// </summary>
    public static ValidationProblemInstance ValidateWriteOpInput(Entity from, Entity to, ConnectionOptions options)
    {
        var problem = ValidationComposer.Validate(
            EntityValidation.FromExists(from),
            EntityValidation.ToExists(to)
        );

        if (problem is { })
        {
            return problem;
        }

        if (options.AllowedWriteFromEntityTypes.Any() && options.AllowedWriteToEntityTypes.Any())
        {
            problem = ValidationComposer.Validate(
                EntityTypeValidation.FromIsOfType(from.TypeId, [.. options.AllowedWriteFromEntityTypes]),
                EntityTypeValidation.ToIsOfType(to.TypeId, [.. options.AllowedWriteToEntityTypes])
            );
        }
        else if (options.AllowedWriteFromEntityTypes.Any())
        {
            problem = ValidationComposer.Validate(
                EntityTypeValidation.FromIsOfType(from.TypeId, [.. options.AllowedWriteFromEntityTypes])
            );
        }
        else if (options.AllowedWriteToEntityTypes.Any())
        {
            problem = ValidationComposer.Validate(
                EntityTypeValidation.ToIsOfType(to.TypeId, [.. options.AllowedWriteToEntityTypes])
            );
        }

        return problem;
    }
}
