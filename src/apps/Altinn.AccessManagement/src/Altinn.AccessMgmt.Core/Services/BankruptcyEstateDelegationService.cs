using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;

using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Altinn.AccessMgmt.Core.Services
{
    public class BankruptcyEstateDelegationService(
        AppDbContext db) : IBankruptcyEstateDelegationService
    {
        private const int MaxSaveAttempts = 3;

        private Task<(Entity From, Entity To)> GetFromAndToEntities(Guid? fromId, Guid? toId, CancellationToken cancellationToken) =>
           ConnectionWriteValidation.GetFromAndToEntitiesAsync(db, fromId, toId, cancellationToken);

        private static ValidationProblemInstance ValidateWriteOpInput(Entity from, Entity to, ConnectionOptions options) =>
            ConnectionWriteValidation.ValidateWriteOpInput(from, to, options);

        private async Task<List<Entity>> GetBankruptcyEstatesInternal(Guid party, CancellationToken cancellationToken = default)
        {
            var assignments = await db.Assignments
                .AsNoTracking()
                .Where(a => a.ToId == party && a.RoleId == RoleConstants.EstateAdministrator)
                .Include(a => a.From)
                .ToListAsync(cancellationToken);

            return assignments.Select(a => a.From).ToList();
        }

        /// <inheritdoc />
        public async Task<bool> HasBankruptcyEstates(Guid partyUuid, CancellationToken cancellationToken = default)
        {
            return await db.Assignments
                .AsNoTracking()
                .AnyAsync(a => a.ToId == partyUuid && a.RoleId == RoleConstants.EstateAdministrator, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Result<List<CompactEntityDto>>> GetBankruptcyEstates(Guid party, CancellationToken cancellationToken = default)
        {
            var estates = await GetBankruptcyEstatesInternal(party, cancellationToken);
            return estates.Select(a => DtoMapper.Convert(a)).ToList();
        }

        /// <inheritdoc />
        public async Task<Result<List<CompactEntityDto>>> GetBankruptcyEstateAdministrators(Guid partyUuid, CancellationToken cancellationToken = default)
        {
            var adminAssignments = await db.Assignments
                .AsNoTracking()
                .Include(a => a.To)
                .Where(a => a.FromId == partyUuid && a.RoleId == RoleConstants.BankruptcyEstateAdministrator)
                .ToListAsync(cancellationToken);

            var adminUsers = adminAssignments.Select(a => DtoMapper.Convert(a.To)).ToList();

            return adminUsers;
        }

        /// <inheritdoc />
        public async Task<Result<AssignmentDto>> AddBankruptcyEstateAdministrator(Guid partyUuid, Guid toUuid, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken = default)
        {
            var options = new ConnectionOptions(configureConnections);

            (Entity from, Entity to) = await GetFromAndToEntities(partyUuid, toUuid, cancellationToken);
            var problem = ValidateWriteOpInput(from, to, options);
            if (problem is { })
            {
                return problem;
            }

            Assignment adminAssignment = null;
            await SaveWithRetryOnConflict(
                async () =>
                {
                    adminAssignment = await db.Assignments.AsNoTracking().Where(p => p.FromId == partyUuid && p.ToId == toUuid && p.RoleId == RoleConstants.BankruptcyEstateAdministrator).FirstOrDefaultAsync(cancellationToken);
                    if (adminAssignment is null)
                    {
                        adminAssignment = new Assignment
                        {
                            FromId = partyUuid,
                            ToId = toUuid,
                            RoleId = RoleConstants.BankruptcyEstateAdministrator,
                        };

                        db.Assignments.Add(adminAssignment);
                    }

                    var estateIds = await GetBankruptcyEstateIds(partyUuid, cancellationToken);
                    await GrantEstateAccess(estateIds.Select(estateId => (estateId, toUuid)).ToList(), cancellationToken);
                },
                cancellationToken);

            return DtoMapper.Convert(adminAssignment);
        }

        /// <inheritdoc />
        public async Task<int> GrantEstateAccessToAdministrators(IReadOnlyCollection<(Guid EstateId, Guid EstateAdministratorPartyId)> estateAdministrators, CancellationToken cancellationToken = default)
        {
            if (estateAdministrators.Count == 0)
            {
                return 0;
            }

            return await SaveWithRetryOnConflict(
                async () => await GrantEstateAccess(await GetEstateAdministratorPairs(estateAdministrators, cancellationToken), cancellationToken),
                cancellationToken);
        }

        /// <summary>
        /// Stages the changes and saves them. The staging reads the current rows and then inserts or deletes, which
        /// races with concurrent requests for the same estates and administrators:
        /// - two grants can both see a row as missing, and the unique index on (FromId, ToId, RoleId) or
        ///   (AssignmentId, PackageId) rejects the second insert;
        /// - two revokes can both delete the same row, and the second affects no rows;
        /// - a grant can add a package to an assignment a revoke is deleting, and the foreign key rejects whichever
        ///   is saved last.
        /// On any of these the pending changes are discarded and staged again against the committed rows.
        /// </summary>
        private async Task<int> SaveWithRetryOnConflict(Func<Task> stageChanges, CancellationToken cancellationToken)
        {
            for (var attempt = 1; ; attempt++)
            {
                await stageChanges();
                try
                {
                    return await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex) when (attempt < MaxSaveAttempts && IsConcurrentWriteConflict(ex))
                {
                    db.ChangeTracker.Clear();
                }
            }
        }

        private static bool IsConcurrentWriteConflict(DbUpdateException ex) =>
            ex is DbUpdateConcurrencyException
            || ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation or PostgresErrorCodes.ForeignKeyViolation };

        /// <inheritdoc />
        public async Task<int> RevokeEstateAccessFromAdministrators(IReadOnlyCollection<(Guid EstateId, Guid EstateAdministratorPartyId)> estateAdministrators, CancellationToken cancellationToken = default)
        {
            if (estateAdministrators.Count == 0)
            {
                return 0;
            }

            return await SaveWithRetryOnConflict(
                async () => await RevokeEstateAccess(await GetEstateAdministratorPairs(estateAdministrators, cancellationToken), cancellationToken),
                cancellationToken);
        }

        /// <summary>
        /// Expands each (estate, estate administrator party) relation to one (estate, administrator) pair per
        /// bankruptcy estate administrator of that party, loading the administrators of all parties in one query.
        /// </summary>
        private async Task<List<(Guid EstateId, Guid AdminId)>> GetEstateAdministratorPairs(IReadOnlyCollection<(Guid EstateId, Guid EstateAdministratorPartyId)> estateAdministrators, CancellationToken cancellationToken)
        {
            var partyIds = estateAdministrators.Select(r => r.EstateAdministratorPartyId).Distinct().ToList();
            var adminsByParty = (await db.Assignments
                .AsNoTracking()
                .Where(a => partyIds.Contains(a.FromId) && a.RoleId == RoleConstants.BankruptcyEstateAdministrator)
                .Select(a => new { a.FromId, a.ToId })
                .ToListAsync(cancellationToken))
                .ToLookup(a => a.FromId, a => a.ToId);

            return estateAdministrators
                .SelectMany(r => adminsByParty[r.EstateAdministratorPartyId].Select(adminId => (r.EstateId, adminId)))
                .Distinct()
                .ToList();
        }

        private async Task<List<Guid>> GetBankruptcyEstateIds(Guid party, CancellationToken cancellationToken)
        {
            return await db.Assignments
                .AsNoTracking()
                .Where(a => a.ToId == party && a.RoleId == RoleConstants.EstateAdministrator)
                .Select(a => a.FromId)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// Loads the Rightholder assignments for the given (estate, administrator) pairs in one query. The query
        /// matches on estate ids and administrator ids separately, so combinations outside the pairs are filtered
        /// out in memory.
        /// </summary>
        private async Task<List<Assignment>> GetEstateAssignments(List<(Guid EstateId, Guid AdminId)> pairs, bool tracking, CancellationToken cancellationToken)
        {
            var estateIds = pairs.Select(p => p.EstateId).Distinct().ToList();
            var adminIds = pairs.Select(p => p.AdminId).Distinct().ToList();
            var pairSet = pairs.ToHashSet();

            var query = tracking ? db.Assignments.AsTracking() : db.Assignments.AsNoTracking();
            var assignments = await query
                .Where(a => estateIds.Contains(a.FromId) && adminIds.Contains(a.ToId) && a.RoleId == RoleConstants.Rightholder)
                .ToListAsync(cancellationToken);

            return assignments.Where(a => pairSet.Contains((a.FromId, a.ToId))).ToList();
        }

        /// <summary>
        /// Adds the Rightholder assignment from the estate to the administrator for every pair, with the packages
        /// MainAdministrator, BankruptcyEstateReadAccess and BankruptcyEstateWriteAccess, where missing.
        /// Existing assignments and packages are loaded in two queries regardless of the number of pairs.
        /// Changes are added to the context and must be saved by the caller.
        /// </summary>
        private async Task GrantEstateAccess(List<(Guid EstateId, Guid AdminId)> pairs, CancellationToken cancellationToken)
        {
            if (pairs.Count == 0)
            {
                return;
            }

            var existingAssignments = (await GetEstateAssignments(pairs, tracking: false, cancellationToken))
                .ToDictionary(a => (a.FromId, a.ToId));

            var existingAssignmentIds = existingAssignments.Values.Select(a => a.Id).ToList();
            var existingPackages = (await db.AssignmentPackages
                .AsNoTracking()
                .Where(p => existingAssignmentIds.Contains(p.AssignmentId) && EstateAdministratorPackages.Contains(p.PackageId))
                .Select(p => new { p.AssignmentId, p.PackageId })
                .ToListAsync(cancellationToken))
                .Select(p => (p.AssignmentId, p.PackageId))
                .ToHashSet();

            foreach (var (estateId, adminId) in pairs)
            {
                if (!existingAssignments.TryGetValue((estateId, adminId), out var estateAssignment))
                {
                    estateAssignment = new Assignment
                    {
                        FromId = estateId,
                        ToId = adminId,
                        RoleId = RoleConstants.Rightholder,
                    };

                    db.Assignments.Add(estateAssignment);
                }

                foreach (var packageId in EstateAdministratorPackages)
                {
                    if (!existingPackages.Contains((estateAssignment.Id, packageId)))
                    {
                        db.AssignmentPackages.Add(new AssignmentPackage
                        {
                            AssignmentId = estateAssignment.Id,
                            PackageId = packageId,
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Removes the Rightholder assignment from the estate to the administrator for every pair, including its
        /// packages. Assignments and packages are loaded in two queries regardless of the number of pairs.
        /// Changes are added to the context and must be saved by the caller.
        /// </summary>
        private async Task RevokeEstateAccess(List<(Guid EstateId, Guid AdminId)> pairs, CancellationToken cancellationToken)
        {
            if (pairs.Count == 0)
            {
                return;
            }

            var estateAssignments = await GetEstateAssignments(pairs, tracking: true, cancellationToken);

            if (estateAssignments.Count == 0)
            {
                return;
            }

            var estateAssignmentIds = estateAssignments.Select(a => a.Id).ToList();
            var estatePackages = await db.AssignmentPackages
                .AsTracking()
                .Where(p => estateAssignmentIds.Contains(p.AssignmentId))
                .ToListAsync(cancellationToken);
            db.AssignmentPackages.RemoveRange(estatePackages);

            // TODO: Open point in https://github.com/Altinn/altinn-auth/pull/4415: should we revoke the whole Rightholder
            // assignment or only the three estate administrator packages? For now the whole assignment is removed,
            // as it is better to remove it than to leave a user with access they should not have.
            db.Assignments.RemoveRange(estateAssignments);
        }

        private static readonly Guid[] EstateAdministratorPackages =
        [
            PackageConstants.MainAdministrator,
            PackageConstants.BankruptcyEstateReadAccess,
            PackageConstants.BankruptcyEstateWriteAccess,
        ];

        /// <inheritdoc />
        public async Task<ValidationProblemInstance?> RevokeBankruptcyEstateAdministrator(Guid partyUuid, Guid toUuid, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken = default)
        {
            var options = new ConnectionOptions(configureConnections);
            (Entity from, Entity to) = await GetFromAndToEntities(partyUuid, toUuid, cancellationToken);
            var problem = ValidateWriteOpInput(from, to, options);
            if (problem is { })
            {
                return problem;
            }

            await SaveWithRetryOnConflict(
                async () =>
                {
                    var existingAssignment = await db.Assignments
                        .AsTracking()
                        .Where(p => p.FromId == partyUuid && p.ToId == toUuid && p.RoleId == RoleConstants.BankruptcyEstateAdministrator)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (existingAssignment is not null)
                    {
                        var estateIds = await GetBankruptcyEstateIds(partyUuid, cancellationToken);
                        await RevokeEstateAccess(estateIds.Select(estateId => (estateId, toUuid)).ToList(), cancellationToken);

                        db.Assignments.Remove(existingAssignment);
                    }
                },
                cancellationToken);

            return null;
        }
    }

    /// <summary>
    /// Service for managing bankruptcy estate delegations.
    /// </summary>
    public interface IBankruptcyEstateDelegationService
    {
        /// <summary>
        /// Checks if a given party has any bankruptcy estates.
        /// </summary>
        /// <param name="partyUuid">The unique identifier of the party.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A result indicating whether the party has any bankruptcy estates.</returns>
        Task<bool> HasBankruptcyEstates(Guid partyUuid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of bankruptcy estates for a given party.
        /// </summary>
        /// <param name="party">The unique identifier of the party.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A result containing a list of bankruptcy estates.</returns>
        Task<Result<List<CompactEntityDto>>> GetBankruptcyEstates(Guid party, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a list of bankruptcy estate administrators for a given party.
        /// </summary>
        /// <param name="partyUuid">The unique identifier of the party.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A result containing a list of bankruptcy estate administrators.</returns>
        Task<Result<List<CompactEntityDto>>> GetBankruptcyEstateAdministrators(Guid partyUuid, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a bankruptcy estate administrator to a given party.
        /// </summary>
        /// <param name="partyUuid">The unique identifier of the party.</param>
        /// <param name="toUuid">The unique identifier of the administrator to add.</param>
        /// <param name="configureConnections">An action to configure connection options.</param>   
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A result containing a list of bankruptcy estate administrators.</returns>
        Task<Result<AssignmentDto>> AddBankruptcyEstateAdministrator(Guid partyUuid, Guid toUuid, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes a bankruptcy estate administrator from a given party.
        /// </summary>
        /// <param name="partyUuid">The unique identifier of the party.</param>
        /// <param name="toUuid">The unique identifier of the administrator to revoke.</param>
        /// <param name="configureConnections">An action to configure connection options.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A result containing the revoked assignment.</returns>
        Task<ValidationProblemInstance?> RevokeBankruptcyEstateAdministrator(Guid partyUuid, Guid toUuid, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken = default);

        /// <summary>
        /// For each (estate, estate administrator party) relation, gives every bankruptcy estate administrator of
        /// the party the Rightholder role from the estate, with the estate administrator packages. Used by the ER
        /// import when EstateAdministrator roles are added. The whole batch is read in a fixed number of queries
        /// and saved once, so the caller decides the batch size.
        /// </summary>
        /// <param name="estateAdministrators">The (estate (konkursbo), estate administrator party (bostyrer)) relations.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The number of changed rows.</returns>
        Task<int> GrantEstateAccessToAdministrators(IReadOnlyCollection<(Guid EstateId, Guid EstateAdministratorPartyId)> estateAdministrators, CancellationToken cancellationToken = default);

        /// <summary>
        /// For each (estate, estate administrator party) relation, revokes the Rightholder role from the estate for
        /// every bankruptcy estate administrator of the party. Used by the ER import when EstateAdministrator roles
        /// are removed. The whole batch is read in a fixed number of queries and saved once.
        /// </summary>
        /// <param name="estateAdministrators">The (estate (konkursbo), estate administrator party (bostyrer)) relations.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>The number of changed rows.</returns>
        Task<int> RevokeEstateAccessFromAdministrators(IReadOnlyCollection<(Guid EstateId, Guid EstateAdministratorPartyId)> estateAdministrators, CancellationToken cancellationToken = default);
    }
}
