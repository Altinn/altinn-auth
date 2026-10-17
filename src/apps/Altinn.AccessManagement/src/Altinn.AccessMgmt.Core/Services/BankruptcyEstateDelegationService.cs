using Altinn.AccessMgmt.Core.Notifications;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;


using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services
{
    public class BankruptcyEstateDelegationService(
        AppDbContext db) : IBankruptcyEstateDelegationService
    {
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

            var adminAssignment = await db.Assignments.AsNoTracking().Where(p => p.FromId == partyUuid && p.ToId == toUuid && p.RoleId == RoleConstants.BankruptcyEstateAdministrator).FirstOrDefaultAsync(cancellationToken);
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

            var estates = await GetBankruptcyEstatesInternal(partyUuid, cancellationToken);

            foreach (var estate in estates)
            {
                var estateAssignment = await db.Assignments
                    .AsNoTracking()
                    .Where(a => a.FromId == estate.Id && a.ToId == toUuid && a.RoleId == RoleConstants.Rightholder)
                    .FirstOrDefaultAsync(cancellationToken);

                List<AssignmentPackage> estatePackages = null;

                if (estateAssignment is null)
                {
                    estateAssignment = new Assignment
                    {
                        FromId = estate.Id,
                        ToId = toUuid,
                        RoleId = RoleConstants.Rightholder,
                    };

                    db.Assignments.Add(estateAssignment);
                }
                else
                {
                    estatePackages = await db.AssignmentPackages
                        .AsNoTracking()
                        .Where(a => a.AssignmentId == estateAssignment.Id)
                        .ToListAsync(cancellationToken);
                }

                if (estatePackages is null || !estatePackages.Any(p => p.PackageId == PackageConstants.MainAdministrator))
                {
                    db.AssignmentPackages.Add(new AssignmentPackage
                    {
                        AssignmentId = estateAssignment.Id,
                        PackageId = PackageConstants.MainAdministrator,
                    });
                }

                if (estatePackages is null || !estatePackages.Any(p => p.PackageId == PackageConstants.BankruptcyEstateReadAccess))
                {
                    db.AssignmentPackages.Add(new AssignmentPackage
                    {
                        AssignmentId = estateAssignment.Id,
                        PackageId = PackageConstants.BankruptcyEstateReadAccess,
                    });
                }

                if (estatePackages is null || !estatePackages.Any(p => p.PackageId == PackageConstants.BankruptcyEstateWriteAccess))
                {
                    db.AssignmentPackages.Add(new AssignmentPackage
                    {
                        AssignmentId = estateAssignment.Id,
                        PackageId = PackageConstants.BankruptcyEstateWriteAccess,
                    });
                }                
            }

            await db.SaveChangesAsync(cancellationToken);

            return DtoMapper.Convert(adminAssignment);
        }

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

            var existingAssignment = await db.Assignments
                .AsTracking()
                .Where(p => p.FromId == partyUuid && p.ToId == toUuid && p.RoleId == RoleConstants.BankruptcyEstateAdministrator)
                .FirstOrDefaultAsync(cancellationToken);

            var estates = await GetBankruptcyEstatesInternal(partyUuid, cancellationToken);
            foreach (var estate in estates)
            {
                var estateAssignment = await db.Assignments
                    .AsTracking()
                    .Where(a => a.FromId == estate.Id && a.ToId == toUuid && a.RoleId == RoleConstants.Rightholder)
                    .FirstOrDefaultAsync(cancellationToken);
                if (estateAssignment is not null)
                {
                    var estatePackages = await db.AssignmentPackages
                        .AsTracking()
                        .Where(a => a.AssignmentId == estateAssignment.Id)
                        .ToListAsync(cancellationToken);
                    db.AssignmentPackages.RemoveRange(estatePackages);
                    
                    // TODO: Should we check if the assignment is used for other purposes before removing it? For now, we will remove it as better to remove it than to leave it and have a user with access they should not have.
                    db.Assignments.Remove(estateAssignment);
                }
            }

            if (existingAssignment is not null)
            {
                db.Assignments.Remove(existingAssignment);
            }

            await db.SaveChangesAsync(cancellationToken);
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
    }
}
