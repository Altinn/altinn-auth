using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Extensions;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;

using Microsoft.EntityFrameworkCore;

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
        public async Task<Result<List<BankruptcyEstateAdministratorDto>>> GetBankruptcyEstateAdministrators(Guid partyUuid, CancellationToken cancellationToken = default)
        {
            var adminAssignments = await db.Assignments
                .AsNoTracking()
                .Include(a => a.To)
                .Include(a => a.Role)
                .Where(a => a.FromId == partyUuid && a.RoleId == RoleConstants.BankruptcyEstateAdministrator)
                .ToListAsync(cancellationToken);

            if (adminAssignments.Count == 0)
            {
                return new List<BankruptcyEstateAdministratorDto>();
            }

            var estateIds = await GetBankruptcyEstateIds(partyUuid, cancellationToken);
            var adminIds = adminAssignments.Select(a => a.ToId).Distinct().ToList();

            // The Rightholder assignments from each estate of the party to each administrator, and the packages and
            // resources the administrator holds through them.
            var estateAssignments = await db.Assignments
                .AsNoTracking()
                .Include(a => a.From)
                .Where(a => estateIds.Contains(a.FromId) && adminIds.Contains(a.ToId) && a.RoleId == RoleConstants.Rightholder)
                .ToListAsync(cancellationToken);

            var estateAssignmentIds = estateAssignments.Select(a => a.Id).ToList();

            var packagesByAssignment = (await db.AssignmentPackages
                .AsNoTracking()
                .Include(p => p.Package)
                .Where(p => estateAssignmentIds.Contains(p.AssignmentId))
                .ToListAsync(cancellationToken))
                .ToLookup(p => p.AssignmentId, p => p.Package);

            var resourcesByAssignment = (await db.AssignmentResources
                .AsNoTracking()
                .Include(r => r.Resource)
                .Where(r => estateAssignmentIds.Contains(r.AssignmentId))
                .ToListAsync(cancellationToken))
                .ToLookup(r => r.AssignmentId, r => r.Resource);

            var accessByAdmin = estateAssignments
                .Where(a => packagesByAssignment.Contains(a.Id) || resourcesByAssignment.Contains(a.Id))
                .GroupBy(a => a.ToId)
                .ToDictionary(
                    admin => admin.Key,
                    admin => admin
                        .Select(a => new BankruptcyEstateAdministratorDto.AdminRoleAccessPackage
                        {
                            Estate = DtoMapper.Convert(a.From),
                            Packages = packagesByAssignment[a.Id].Select(DtoMapper.ConvertCompactPackage).ToArray(),
                            Resources = resourcesByAssignment[a.Id].Select(DtoMapper.ConvertCompactResource).ToList(),
                        })
                        .ToList());

            return adminAssignments.Select(a => new BankruptcyEstateAdministratorDto
            {
                Administrator = DtoMapper.Convert(a.To),
                AdminRole = DtoMapper.ConvertCompactRole(a.Role),
                AdminAddedAt = a.Audit_ValidFrom,
                EstateAccess = accessByAdmin.GetValueOrDefault(a.ToId, []),
            }).ToList();
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
            if (await TrySaveWithRetryOnConflict(
                async () =>
                {
                    await LockEstateAdministratorParties(db, [partyUuid], cancellationToken);
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
                cancellationToken) is { } conflict)
            {
                return conflict;
            }

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
                async () =>
                {
                    await LockEstateAdministratorParties(db, estateAdministrators.Select(r => r.EstateAdministratorPartyId), cancellationToken);
                    await GrantEstateAccess(await GetEstateAdministratorPairs(estateAdministrators, cancellationToken), cancellationToken);
                },
                cancellationToken);
        }

        /// <summary>
        /// Stages the changes and saves them in one transaction. The staging must start with
        /// <see cref="LockEstateAdministratorParties"/> for every estate administrator party (bostyrer) it touches.
        /// Without that lock the staging reads the current rows and then inserts or deletes against a state another
        /// request may already have changed. A grant could read an administrator relationship, a revoke could delete it,
        /// and the grant could then insert estate access that nothing would ever revoke, without any constraint failing.
        /// With the lock, operations on the same party run one after the other, and each reads what the previous one
        /// committed.
        /// Constraint failures can still happen against writers that do not take the lock, such as a concurrent
        /// insert of the same assignment, and the import can deadlock with another batch. On any of these the
        /// transaction is rolled back when it is disposed uncommitted, and the changes are staged again against the
        /// committed rows.
        /// </summary>
        private Task<int> SaveWithRetryOnConflict(Func<Task> stageChanges, CancellationToken cancellationToken) =>
            db.RetryOnConcurrentWriteConflict(
                async () =>
                {
                    await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
                    await stageChanges();
                    var changed = await db.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return changed;
                },
                MaxSaveAttempts);

        /// <summary>
        /// Same as <see cref="SaveWithRetryOnConflict"/>, for the API. If the conflict remains after the last attempt,
        /// returns a 409 problem instead of throwing, so the caller can try again.
        /// </summary>
        private async Task<ProblemInstance?> TrySaveWithRetryOnConflict(Func<Task> stageChanges, CancellationToken cancellationToken)
        {
            try
            {
                await SaveWithRetryOnConflict(stageChanges, cancellationToken);
                return null;
            }
            catch (Exception ex) when (ex.IsConcurrentWriteConflict())
            {
                return Problems.ConcurrentPersistenceConflict;
            }
        }

        /// <summary>
        /// Locks the entity rows of the estate administrator parties (bostyrer) until the transaction ends. Every
        /// operation that reads or changes a party's administrator relationships, or the estate access derived from
        /// them, takes this lock first: adding and revoking administrators, the ER import granting and revoking estate
        /// access, and the cleanup of dead persons. The lock is on the party and not on the administrator relationship,
        /// because adding an administrator creates that relationship and has no row to lock until then.
        /// FOR NO KEY UPDATE conflicts only with itself and with updates and deletes of the entity. It does not block the
        /// foreign key checks of other inserts that reference the party. Rows are locked in id order, so two batches
        /// over overlapping parties do not deadlock.
        /// </summary>
        internal static Task LockEstateAdministratorParties(AppDbContext db, IEnumerable<Guid> partyIds, CancellationToken cancellationToken)
        {
            var ids = partyIds.Distinct().Order().ToArray();
            if (ids.Length == 0)
            {
                return Task.CompletedTask;
            }

            return db.Database.ExecuteSqlAsync(
                $"SELECT id FROM dbo.entity WHERE id = ANY({ids}) ORDER BY id FOR NO KEY UPDATE",
                cancellationToken);
        }

        /// <inheritdoc />
        public async Task<int> RevokeEstateAccessFromAdministrators(IReadOnlyCollection<(Guid EstateId, Guid EstateAdministratorPartyId)> estateAdministrators, CancellationToken cancellationToken = default)
        {
            if (estateAdministrators.Count == 0)
            {
                return 0;
            }

            return await SaveWithRetryOnConflict(
                async () =>
                {
                    await LockEstateAdministratorParties(db, estateAdministrators.Select(r => r.EstateAdministratorPartyId), cancellationToken);
                    await RevokeEstateAccess(await GetEstateAdministratorPairs(estateAdministrators, cancellationToken), cancellationToken);
                },
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
        public async Task<ProblemInstance?> RevokeBankruptcyEstateAdministrator(Guid partyUuid, Guid toUuid, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken = default)
        {
            var options = new ConnectionOptions(configureConnections);
            (Entity from, Entity to) = await GetFromAndToEntities(partyUuid, toUuid, cancellationToken);
            var problem = ValidateWriteOpInput(from, to, options);
            if (problem is { })
            {
                return problem;
            }

            return await TrySaveWithRetryOnConflict(
                async () =>
                {
                    await LockEstateAdministratorParties(db, [partyUuid], cancellationToken);
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
        Task<Result<List<BankruptcyEstateAdministratorDto>>> GetBankruptcyEstateAdministrators(Guid partyUuid, CancellationToken cancellationToken = default);

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
        Task<ProblemInstance?> RevokeBankruptcyEstateAdministrator(Guid partyUuid, Guid toUuid, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken = default);

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
