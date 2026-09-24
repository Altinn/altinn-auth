using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Helpers.Extensions;
using Altinn.AccessMgmt.Core.Appsettings;
using Altinn.AccessMgmt.Core.Notifications;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Utils.Helper;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Enums;

using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Altinn.AccessMgmt.Core.Services
{
    public class BankruptcyDelegationService(AppDbContext db, IConnectionService connectionService, IAssignmentService assignmentService, IOptions<CoreAppsettings> appsettings) : IBankruptcyDelegationService
    {
        private Task<(Entity From, Entity To)> GetFromAndToEntities(Guid? fromId, Guid? toId, CancellationToken cancellationToken) =>
            ConnectionWriteValidation.GetFromAndToEntitiesAsync(db, fromId, toId, cancellationToken);

        private static ValidationProblemInstance ValidateWriteOpInput(Entity from, Entity to, ConnectionOptions options) =>
            ConnectionWriteValidation.ValidateWriteOpInput(from, to, options);

        private static Result<List<Guid>> GetPackageListFromUrnList(List<PackageReferenceDto> packages)
        {
            List<Guid> packageIds = [];
            ValidationErrorBuilder errors = default;

            foreach (PackageReferenceDto package in packages)
            {
                if (!PackageConstants.TryGetByUrn(package.Urn, out var packageObj))
                {
                    errors.Add(ValidationErrors.PackageNotExists, "QUERY/packages", [new("package", $"Package with urn '{package.Urn}' not found.")]);
                    continue;
                }

                packageIds.Add(packageObj.Entity.Id);
            }

            if (errors.TryBuild(out var errorResult))
            {
                return errorResult;
            }

            return packageIds;
        }

        private async Task<List<RolePackage>> GetDelegablePackagesForBankruptcyEstateAdmin( CancellationToken cancellationToken)
        {
            var directAvilablePackages = await db.RolePackages
                .AsNoTracking()
                .Where(rp =>
                    rp.RoleId == RoleConstants.EstateAdministrator.Id &&
                    (rp.EntityVariantId == null || rp.EntityVariantId == EntityVariantConstants.KBO.Id) &&
                    rp.CanDelegate)
                .Select(rp => rp)
                .ToListAsync(cancellationToken);

            List<RolePackage> mainAdminPackages = new List<RolePackage>();
            if (directAvilablePackages.Any(p => p.PackageId.Equals(PackageConstants.MainAdministrator)))
            {
                mainAdminPackages = await db.RolePackages
                .AsNoTracking()
                .Where(rp =>
                    rp.RoleId == RoleConstants.MainAdministrator.Id &&
                    (rp.EntityVariantId == null || rp.EntityVariantId == EntityVariantConstants.KBO.Id) &&
                    rp.CanDelegate)
                .Select(rp => rp)
                .ToListAsync(cancellationToken);
            }

            var combinedPackages = directAvilablePackages
                .Concat(mainAdminPackages.Where(m => !directAvilablePackages.Any(d => d.Id == m.Id)))
                .ToList();

            return combinedPackages;
        }

        /// <inheritdoc />
        public async Task<bool> CheckBankruptcyEstateConnection(Guid party, Guid estate, CancellationToken cancellationToken = default)
        {
            return await db.Assignments
                .AsNoTracking()
                .AnyAsync(a => a.FromId == estate && a.ToId == party && a.RoleId == RoleConstants.EstateAdministrator, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Result<AssignmentWithAssignmentPackageDto>> AddCreditor(Guid party, Guid estate, Guid creditor, Action<ConnectionOptions> configureConnections = null, CancellationToken cancellationToken = default)
        {
            var assignment = await connectionService.AddRightholder(estate, creditor, configureConnections, cancellationToken);
            if (assignment.IsProblem)
            {
                return assignment.Problem;
            }

            var assignmentId = assignment.Value.Id;

            var assignmentPackage = await db.AssignmentPackages
                    .AsNoTracking()
                    .Where(ap => ap.AssignmentId == assignmentId && ap.PackageId == PackageConstants.BankruptcyEstateReadAccess.Id)
                    .FirstOrDefaultAsync(cancellationToken);

            if (assignmentPackage is null)
            {
                assignmentPackage = new AssignmentPackage
                {
                    AssignmentId = assignmentId,
                    PackageId = PackageConstants.BankruptcyEstateReadAccess.Entity.Id
                };

                db.AssignmentPackages.Add(assignmentPackage);
                await db.SaveChangesAsync(cancellationToken);
            }

            return new AssignmentWithAssignmentPackageDto(assignment.Value, DtoMapper.Convert(assignmentPackage).SingleToList());
        }

        /// <inheritdoc />
        public async Task<Result<bool>> RevokeCreditor(Guid party, Guid estate, Guid creditor, Action<ConnectionOptions> configureConnections = null, CancellationToken cancellationToken = default)
        {
            var options = new ConnectionOptions(configureConnections);
            (Entity from, Entity to) = await GetFromAndToEntities(estate, creditor, cancellationToken);
            var problem = ValidateWriteOpInput(from, to, options);
            if (problem is { })
            {
                return problem;
            }

            var existingAssignment = await db.Assignments
                .AsNoTracking()
                .Where(e => e.FromId == from.Id)
                .Where(e => e.ToId == to.Id)
                .Where(e => e.RoleId == RoleConstants.Rightholder)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingAssignment is null)
            {
                return false;
            }

            var assignmentPackagesToRemove = await db.AssignmentPackages
                .AsNoTracking()
                .Where(ap => ap.AssignmentId == existingAssignment.Id && ap.PackageId == PackageConstants.BankruptcyEstateReadAccess.Id)
                .ToListAsync(cancellationToken);

            if (assignmentPackagesToRemove.Count == 0)
            {
                return false;
            }

            db.AssignmentPackages.RemoveRange(assignmentPackagesToRemove);
            int removedCount = await db.SaveChangesAsync(cancellationToken);

            var result = await assignmentService.DeleteAssignment(existingAssignment.Id, false, null, cancellationToken);

            return removedCount > 0;
        }

        /// <inheritdoc />
        public async Task<Result<List<CompactEntityDto>>> GetCreditors(Guid party, Guid estate, CancellationToken cancellationToken = default)
        {
            var estateAssignments = await db.Assignments
                .AsNoTracking()
                .Join(db.AssignmentPackages, a => a.Id, ap => ap.AssignmentId, (a, ap) => new { Assignment = a, AssignmentPackage = ap })
                .Where(a => a.Assignment.FromId == estate && a.Assignment.RoleId == RoleConstants.Rightholder && a.AssignmentPackage.PackageId == PackageConstants.BankruptcyEstateReadAccess.Id)
                .Select(a => a.Assignment.To)
                .ToListAsync(cancellationToken);

            List<CompactEntityDto> creditors = new List<CompactEntityDto>();

            foreach (var to in estateAssignments)
            {
                creditors.Add(DtoMapper.Convert(to));
            }

            return creditors;
        }

        /// <inheritdoc/>
        public async Task<Result<AssignmentDto>> AddAgent(Guid party, Guid user, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken = default)
        {
            var options = new ConnectionOptions(configureConnections);

            (Entity from, Entity to) = await GetFromAndToEntities(party, user, cancellationToken);
            var problem = ValidateWriteOpInput(from, to, options);
            if (problem is { })
            {
                return problem;
            }

            if (from.TypeId != EntityTypeConstants.Person)
            {
                return Problems.EntityTypeNotFound;
            }

            var existingAssignment = await db.Assignments.AsNoTracking().Where(p => p.FromId == party && p.ToId == user && p.RoleId == RoleConstants.Agent).FirstOrDefaultAsync(cancellationToken);
            if (existingAssignment is { })
            {
                return DtoMapper.Convert(existingAssignment);
            }

            var assignment = new Assignment
            {
                FromId = party,
                ToId = user,
                RoleId = RoleConstants.Agent,
            };

            db.Assignments.Add(assignment);
            await AgentAddedNotification.Upsert(
                db,
                party,
                user,
                appsettings?.Value?.Notifications?.AgentAddedNotifyInSeconds ?? AgentAddedNotification.DefaultNotifyInSeconds,
                cancellationToken
            );
            await db.SaveChangesAsync(cancellationToken);

            return DtoMapper.Convert(assignment);
        }

        /// <inheritdoc/>
        public async Task<ValidationProblemInstance?> RevokeAgent(Guid party, Guid user, bool cascade, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken = default)
        {
            var options = new ConnectionOptions(configureConnections);
            (Entity from, Entity to) = await GetFromAndToEntities(party, user, cancellationToken);
            var problem = ValidateWriteOpInput(from, to, options);
            if (problem is { })
            {
                return problem;
            }

            var existingAssignment = await db.Assignments
                .AsTracking()
                .Where(p => p.FromId == party && p.ToId == user && p.RoleId == RoleConstants.Agent)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingAssignment is null)
            {
                return null;
            }

            if (!cascade)
            {
                ValidationErrorBuilder errorBuilder = default;
                errorBuilder = await CascadingRevokeHelper.CheckCascadingDependenciesAgentAssignment(db, existingAssignment, cancellationToken);

                if (errorBuilder.TryBuild(out problem))
                {
                    return problem;
                }
            }

            await AgentRemovedNotification.Upsert(
                db,
                party,
                user,
                appsettings?.Value?.Notifications?.AgentRemovedNotifyInSeconds ?? AgentRemovedNotification.DefaultNotifyInSeconds,
                cancellationToken
            );

            db.Assignments.Remove(existingAssignment);
            await db.SaveChangesAsync(cancellationToken);
            return null;
        }

        /// <inheritdoc/>
        public async Task<Result<List<BankruptcyEntityDto>>> GetAgentAdminInformation(Guid party, CancellationToken cancellationToken)
        {
            var agentAssignments = await db.Assignments
                .AsNoTracking()
                .Include(a => a.To)
                .Where(a => a.FromId == party && a.RoleId == RoleConstants.Agent)
                .ToListAsync(cancellationToken);

            var adminAssignments = await db.Assignments
                .AsNoTracking()
                .Join(db.AssignmentPackages, a => a.Id, ap => ap.AssignmentId, (a, ap) => new { Assignment = a, AssignmentPackage = ap })
                .Where(a => a.Assignment.FromId == party && a.Assignment.RoleId == RoleConstants.Rightholder && a.AssignmentPackage.PackageId == PackageConstants.KonkursboAdministrator.Id)
                .Select(a => a.Assignment.To)
                .ToListAsync(cancellationToken);

            var agentUsers = agentAssignments.Select(a => DtoMapper.Convert(a.To)).ToList();
            var adminUsers = adminAssignments.Select(a => DtoMapper.Convert(a)).ToList();
            
            var combined = agentUsers
                .Select(x => (Entity: x, Permission: BankruptcyEstatePermissions.User))
                .Concat(adminUsers.Select(x => (Entity: x, Permission: BankruptcyEstatePermissions.Admin)))
                .GroupBy(x => x.Entity.Id)
                .Select(g =>
                {
                    var hasUser = g.Any(x => x.Permission == BankruptcyEstatePermissions.User);
                    var hasAdmin = g.Any(x => x.Permission == BankruptcyEstatePermissions.Admin);
                    var permission = hasUser && hasAdmin
                        ? BankruptcyEstatePermissions.UserAndAdmin
                        : hasUser
                            ? BankruptcyEstatePermissions.User
                            : BankruptcyEstatePermissions.Admin;
                    return new BankruptcyEntityDto(g.First().Entity, permission);
                })
                .ToList();

            return combined;
        }

        /// <inheritdoc/>
        public async Task<Result<AssignmentWithAssignmentPackageDto>> AddAdministrator(Guid party, Guid user, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken)
        {
            var assignment = await connectionService.AddRightholder(party, user, configureConnections, cancellationToken);
            if (assignment.IsProblem)
            {
                return assignment.Problem;
            }

            var assignmentId = assignment.Value.Id;

            var assignmentPackage = await db.AssignmentPackages
                    .AsNoTracking()
                    .Where(ap => ap.AssignmentId == assignmentId && ap.PackageId == PackageConstants.KonkursboAdministrator.Id)
                    .FirstOrDefaultAsync(cancellationToken);

            if (assignmentPackage is null)
            {
                assignmentPackage = new AssignmentPackage
                {
                    AssignmentId = assignmentId,
                    PackageId = PackageConstants.KonkursboAdministrator.Entity.Id
                };

                db.AssignmentPackages.Add(assignmentPackage);
                await db.SaveChangesAsync(cancellationToken);
            }

            return new AssignmentWithAssignmentPackageDto(assignment.Value, DtoMapper.Convert(assignmentPackage).SingleToList());
        }

        /// <inheritdoc/>
        public async Task<Result<bool>> RevokeAdministrator(Guid party, Guid user, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken)
        {
            var options = new ConnectionOptions(configureConnections);
            (Entity from, Entity to) = await GetFromAndToEntities(party, user, cancellationToken);
            var problem = ValidateWriteOpInput(from, to, options);
            if (problem is { })
            {
                return problem;
            }

            var existingAssignment = await db.Assignments
                .AsNoTracking()
                .Where(e => e.FromId == from.Id)
                .Where(e => e.ToId == to.Id)
                .Where(e => e.RoleId == RoleConstants.Rightholder)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingAssignment is null)
            {
                return false;
            }

            var assignmentPackagesToRemove = await db.AssignmentPackages
                .AsNoTracking()
                .Where(ap => ap.AssignmentId == existingAssignment.Id && ap.PackageId == PackageConstants.KonkursboAdministrator.Id)
                .ToListAsync(cancellationToken);

            if (assignmentPackagesToRemove.Count == 0)
            {
                return false;
            }

            db.AssignmentPackages.RemoveRange(assignmentPackagesToRemove);
            int removedCount = await db.SaveChangesAsync(cancellationToken);

            var result = await assignmentService.DeleteAssignment(existingAssignment.Id, false, null, cancellationToken);

            return removedCount > 0;
        }

        /// <inheritdoc />
        public async Task<Result<List<CompactEntityDto>>> GetBankruptcyEstatesForParty(Guid party, CancellationToken cancellationToken = default)
        {
            var assignments = await db.Assignments
                .AsNoTracking()
                .Where(a => a.ToId == party && a.RoleId == RoleConstants.EstateAdministrator)
                .Include(a => a.From)
                .ToListAsync(cancellationToken);

            return assignments.Select(a => DtoMapper.Convert(a.From)).ToList();
        }

        /// <inheritdoc />
        public async Task<Result<bool>> HasBankruptcyEstatesForParty(Guid party, CancellationToken cancellationToken = default)
        {
            return await db.Assignments
                .AsNoTracking()
                .AnyAsync(a => a.ToId == party && a.RoleId == RoleConstants.EstateAdministrator, cancellationToken);
        }

        public async Task<Result<List<CompactEntityDto>>> GetBankruptcyEstatesForUser(Guid party, Guid user, CancellationToken cancellationToken)
        {
            var query = await db.Delegations
            .AsNoTracking()
            .Include(d => d.To)
            .Include(d => d.From).ThenInclude(a => a.From)
            .Where(d => 
                d.FacilitatorId == party &&
                d.To.FromId == party &&
                d.To.ToId == user &&
                d.To.RoleId == RoleConstants.Agent &&
                d.From.ToId == party &&
                d.From.RoleId == RoleConstants.EstateAdministrator)
            .ToListAsync(cancellationToken);

            var result = query
                .Select(e =>
                    DtoMapper.Convert(e.From.From)
                ).ToList();

            return result;
        }

        /// <inheritdoc />
        public async Task<Result<CreateDelegationResponseDto>> AddBankruptcyEstateForUser(Guid party, Guid estate, Guid user, List<PackageReferenceDto> packages, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken)
        {
            var options = new ConnectionOptions(configureConnections);
            (Entity from, Entity to) = await GetFromAndToEntities(party, user, cancellationToken);
            var problem = ValidateWriteOpInput(from, to, options);
            if (problem is { })
            {
                return problem;
            }

            var packageList = GetPackageListFromUrnList(packages);

            if (packageList.IsProblem)
            {
                return packageList.Problem;
            }

            ValidationErrorBuilder errorBuilder = default;
            var agentAssignment = await db.Assignments
            .FirstOrDefaultAsync(a => a.FromId == party && a.ToId == user && a.RoleId == RoleConstants.Agent.Id, cancellationToken: cancellationToken);

            if (agentAssignment is null)
            {
                errorBuilder.Add(
                    ValidationErrors.MissingAssignment,
                    $"$QUERY/user",
                    [new(RoleConstants.Agent.Entity.Urn, $"Role is not assigned to '{user}' from '{party}'.")]
                );
            }

            var estateAssignment = await db.Assignments.AsNoTracking().FirstOrDefaultAsync(t => t.FromId == estate && t.ToId == party && t.RoleId == RoleConstants.EstateAdministrator.Id, cancellationToken);

            if (estateAssignment is null)
            {
                errorBuilder.Add(
                    ValidationErrors.MissingAssignment,
                    $"/role",
                    [new($"{RoleConstants.EstateAdministrator.Entity.Urn}", $"Role is not assigned to '{party}' from '{estate}'.")]
                );
            }

            if (errorBuilder.TryBuild(out problem))
            {
                return problem;
            }

            var delegation = await db.Delegations
                        .AsNoTracking()
                        .Include(d => d.From)
                        .Include(d => d.To)
                        .Where(d => d.FromId == estateAssignment.Id && d.ToId == agentAssignment.Id && d.FacilitatorId == party)
                        .FirstOrDefaultAsync(cancellationToken);

            bool isDelegationCreatedNow = false;
            if (delegation is null)
            {
                delegation = new Delegation
                {
                    FromId = estateAssignment.Id,
                    ToId = agentAssignment.Id,
                    FacilitatorId = party
                };
                db.Delegations.Add(delegation);
                isDelegationCreatedNow = true;
            }

            var availablePackages = await GetDelegablePackagesForBankruptcyEstateAdmin(cancellationToken);

            foreach (var packageId in packageList.Value)
            {
                if (!availablePackages.Any(p => p.PackageId == packageId))
                {
                    PackageConstants.TryGetById(packageId, out var package);
                    errorBuilder.Add(
                        ValidationErrors.PackageIsNotDelegable,
                        $"/role/{RoleConstants.EstateAdministrator.Entity.Urn}",
                        [new($"{package.Entity.Urn}", $"Package {package.Entity.Urn} is not delegable for role.")]
                    );
                }
                else
                {
                    var rolePackage = availablePackages
                        .Where(rp => rp.PackageId == packageId)
                        .OrderBy(rp => rp.RoleId != RoleConstants.MainAdministrator.Id && rp.EntityVariantId != null)
                        .FirstOrDefault();

                    var rolePackageId = rolePackage.Id;

                    DelegationPackage delegationPackage = null;

                    if (!isDelegationCreatedNow)
                    {
                        delegationPackage = await db.DelegationPackages
                        .AsNoTracking()
                        .Where(dp => dp.DelegationId == delegation.Id && 
                            dp.PackageId == packageId && 
                            dp.RolePackageId == rolePackageId)
                        .FirstOrDefaultAsync(cancellationToken);
                    }

                    if (delegationPackage is null)
                    {
                        delegationPackage = new DelegationPackage
                        {
                            DelegationId = delegation.Id,
                            PackageId = packageId,
                            RolePackageId = rolePackageId
                        };
                        db.DelegationPackages.Add(delegationPackage);
                    }                
                }
            }

            if (errorBuilder.TryBuild(out problem))
            {
                return problem;
            }

            await db.SaveChangesAsync(cancellationToken);

            // Ensure the From navigation is populated for the response mapping
            await db.Entry(delegation).Reference(d => d.From).LoadAsync(cancellationToken);

            return DtoMapper.Convert(delegation);
        }

        /// <inheritdoc />
        public async Task<Result<bool>> RevokeBankruptcyEstateForUser(Guid party, Guid estate, Guid user, List<PackageReferenceDto> packages, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken)
        {
            bool anyDataDeleted = false;
            var options = new ConnectionOptions(configureConnections);
            (Entity from, Entity to) = await GetFromAndToEntities(party, user, cancellationToken);
            var problem = ValidateWriteOpInput(from, to, options);
            if (problem is { })
            {
                return problem;
            }

            var packageList = GetPackageListFromUrnList(packages);

            if (packageList.IsProblem)
            {
                return packageList.Problem;
            }

            ValidationErrorBuilder errorBuilder = default;
            var agentAssignment = await db.Assignments
            .FirstOrDefaultAsync(a => a.FromId == party && a.ToId == user && a.RoleId == RoleConstants.Agent.Id, cancellationToken: cancellationToken);

            if (agentAssignment is null)
            {
                return false;
            }

            var clientAssignment = await db.Assignments.AsNoTracking().FirstOrDefaultAsync(t => t.FromId == estate && t.ToId == party && t.RoleId == RoleConstants.EstateAdministrator.Id, cancellationToken);

            if (clientAssignment is null)
            {
                return false;
            }

            var delegation = await db.Delegations
                        .AsNoTracking()
                        .Where(d => d.FromId == clientAssignment.Id && d.ToId == agentAssignment.Id && d.FacilitatorId == party)
                        .FirstOrDefaultAsync(cancellationToken);

            if (delegation is null)
            {
                return false;
            }

            var availablePackages = await GetDelegablePackagesForBankruptcyEstateAdmin(cancellationToken);

            foreach (var packageId in packageList.Value)
            {
                if (!availablePackages.Any(p => p.PackageId == packageId))
                {
                    PackageConstants.TryGetById(packageId, out var package);
                    errorBuilder.Add(
                        ValidationErrors.PackageIsNotDelegable,
                        $"/role/{RoleConstants.EstateAdministrator.Entity.Urn}",
                        [new($"{package.Entity.Urn}", $"Package {package.Entity.Urn} is not delegable for role.")]
                    );
                }
                else
                {
                    var rolePackage = availablePackages
                        .Where(rp => rp.PackageId == packageId)
                        .OrderBy(rp => rp.RoleId != RoleConstants.MainAdministrator.Id && rp.EntityVariantId != null)
                        .FirstOrDefault();

                    var rolePackageId = rolePackage.Id;
                    
                    var delegationPackage = await db.DelegationPackages
                        .AsNoTracking()
                        .Where(dp => dp.DelegationId == delegation.Id && dp.PackageId == packageId && dp.RolePackageId == rolePackageId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (delegationPackage is not null)
                    {
                        db.DelegationPackages.Remove(delegationPackage);
                        anyDataDeleted = true;
                    }
                }
            }

            if (errorBuilder.TryBuild(out problem))
            {
                return problem;
            }

            await db.SaveChangesAsync(cancellationToken);

            var currentDelegation = await db.Delegations
                .AsNoTracking()
                .Include(d => d.DelegationPackages)
                .Include(d => d.DelegationResources)
                .Where(d => d.FromId == clientAssignment.Id && d.ToId == agentAssignment.Id && d.FacilitatorId == party)
                .FirstOrDefaultAsync(cancellationToken);

            if (currentDelegation.DelegationPackages.Count == 0 && currentDelegation.DelegationResources.Count == 0)
            {
                db.Delegations.Remove(currentDelegation);                
                anyDataDeleted = true;
                await db.SaveChangesAsync(cancellationToken);
            }
            
            return anyDataDeleted;
        }

        public async Task<Result<List<PackageDto>>> GetBankruptcyEstatePackagesForUser(Guid party, Guid estate, Guid user, CancellationToken cancellationToken)
        {
            var query = await db.Delegations
            .AsNoTracking()
            .Include(d => d.DelegationPackages)
            .ThenInclude(dp => dp.Package)
            .Where(d =>
                d.FacilitatorId == party &&
                d.To.FromId == party &&
                d.To.ToId == user &&
                d.To.RoleId == RoleConstants.Agent &&
                d.From.ToId == party &&
                d.From.FromId == estate &&
                d.From.RoleId == RoleConstants.EstateAdministrator)
            .ToListAsync(cancellationToken);

            var result = query
                .SelectMany(e => e.DelegationPackages)
                .Select(dp => DtoMapper.Convert(dp.Package))
                .ToList();

            return result;
        }
    }

    /// <summary>
    /// Service for managing client delegations and delegation of access packageCodes.
    /// </summary>
    public interface IBankruptcyDelegationService
    {
        /// <summary>
        /// Check if a given estate is connected to the party
        /// </summary>
        /// <param name="party">The party identifier.</param>
        /// <param name="estate">The estate identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation. The task assignment contains true if the estate is connected to the party, or a problem detail if an error occurs.</returns>
        Task<bool> CheckBankruptcyEstateConnection(Guid party, Guid estate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a rettighetshaver assignment and adds the package BankruptcyEstateReadAccess to the assignment for a specific bankruptcy estate.
        /// 
        /// It is the callers responsibility to check if the party has access to the estate before calling this method.
        /// </summary>
        /// <param name="party">The party identifier.</param>
        /// <param name="estate">The bankruptcy estate identifier.</param>
        /// <param name="creditor">The user identifier.</param>
        /// <param name="configureConnections">Optional action to configure connection options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A problem details if some error occurs. true if read access is added and false if it alredy exists</returns>
        Task<Result<AssignmentWithAssignmentPackageDto>> AddCreditor(Guid party, Guid estate, Guid creditor, Action<ConnectionOptions> configureConnections = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes the package BankruptcyEstateReadAccess from the assignment between the creditor and the bankruptcy estate if the rettighetshaver assignment holds no more content the assignment is also removed.
        /// 
        /// It is the callers responsibility to check if the party has access to the estate before calling this method.
        /// </summary>
        /// <param name="party">The party identifier.</param>
        /// <param name="estate">The bankruptcy estate identifier.</param>
        /// <param name="creditor">The user identifier.</param>
        /// <param name="configureConnections">Optional action to configure connection options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A problem details if some error occurs. true if read access is revoked and false if it alredy was revoked</returns>
        Task<Result<bool>> RevokeCreditor(Guid party, Guid estate, Guid creditor, Action<ConnectionOptions> configureConnections = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of creditors for a specific bankruptcy estate. 
        /// 
        /// It is the callers responsibility to check if the party has access to the estate before calling this method.
        /// </summary>
        /// <param name="party">The party identifier.</param>
        /// <param name="estate">The bankruptcy estate identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A problem details if some error occurs. List of creditors if successful.</returns>
        Task<Result<List<CompactEntityDto>>> GetCreditors(Guid party, Guid estate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds an agent relationship between two entities.
        /// 
        /// It is the callers responsibility to check if the party has access to the estate before calling this method.
        /// </summary>
        /// <param name="party">The entity the Agent relationship is defined for</param>
        /// <param name="user">The entity the agent relationship is given to</param>
        /// <param name="configureConnections">Optional action to configure connection options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A problem details if some error occurs. The assignment details if successful.</returns>
        Task<Result<AssignmentDto>> AddAgent(Guid party, Guid user, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken = default);

        /// <summary>
        /// Revokes an agent relationship between two entities.
        /// </summary>
        /// <param name="party">The entity the Agent relationship is defined for</param>
        /// <param name="user">The entity the agent relationship is given to</param>
        /// <param name="cascade">If true the revoke is performed even when there are active dependencies else it will fail if there are active dependencies</param>
        /// <param name="configureConnections">Optional action to configure connection options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>error or nothing</returns>
        Task<ValidationProblemInstance?> RevokeAgent(Guid party, Guid user, bool cascade, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of agents/administrators for a specific party.
        /// 
        /// It is the callers responsibility to check if the party has access to the estate before calling this method.
        /// </summary>
        /// <param name="party">The party identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A problem details if some error occurs. List of agents/administrators if successful.</returns>
        Task<Result<List<BankruptcyEntityDto>>> GetAgentAdminInformation(Guid party, CancellationToken cancellationToken);

        /// <summary>
        /// Adds rightholder role to a user and assign the boadministrator package for a specific party.
        /// </summary>
        /// <param name="party">The entity the rightholder relationship is defined for</param>
        /// <param name="user">he entity the agent relationship is given to</param>
        /// <param name="configureConnections">Optional action to configure connection options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A problem details if some error occurs. The assignment details if successful.</returns>
        Task<Result<AssignmentWithAssignmentPackageDto>> AddAdministrator(Guid party, Guid user, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken);

        /// <summary>
        /// Revokes rightholder role from a user and removes the boadministrator package for a specific party.
        /// </summary>
        /// <param name="party">The entity the rightholder relationship is defined for</param>
        /// <param name="user">The entity the agent relationship is given to</param>
        /// <param name="configureConnections">Optional action to configure connection options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>error or nothing</returns>
        Task<Result<bool>> RevokeAdministrator(Guid party, Guid user, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the list of bankruptcy estates for a specific party.
        /// </summary>
        /// <param name="party">The party identifier to fetch bankruptcy estates for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A problem details if some error occurs. List of bankruptcy estates if successful.</returns>
        Task<Result<List<CompactEntityDto>>> GetBankruptcyEstatesForParty(Guid party, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether there are any bankruptcy estates for a specific party.
        /// </summary>
        /// <param name="party">The party identifier to check bankruptcy estates for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A problem details if some error occurs. True if the party has bankruptcy estates, otherwise false.</returns>
        Task<Result<bool>> HasBankruptcyEstatesForParty(Guid party, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the list of bankruptcy estates for a specific user.
        /// </summary>
        /// <param name="party">The entity the rightholder relationship is defined for</param>
        /// <param name="user">The user identifier to fetch</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A problem details if some error occurs. List of bankruptcy estates if successful.</returns>
        Task<Result<List<CompactEntityDto>>> GetBankruptcyEstatesForUser(Guid party, Guid user, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the list of pacgages for a given bankruptcy estate and a specific user.
        /// </summary>
        /// <param name="party">The entity the rightholder relationship is defined for</param>
        /// <param name="estate">The bankruptcyestate identifier to fetch</param>
        /// <param name="user">The user identifier to fetch</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A problem details if some error occurs. List of packages if successful.</returns>
        Task<Result<List<PackageDto>>> GetBankruptcyEstatePackagesForUser(Guid party, Guid estate, Guid user, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the list of bankruptcy estates for a specific user.
        /// </summary>
        /// <param name="party">The entity the rightholder relationship is defined for</param>
        /// <param name="estate">The bankruptcyestate identifier to fetch</param>
        /// <param name="user">The user identifier to fetch</param>
        /// <param name="packages">The packages to add to the delegation.</param>
        /// <param name="configureConnections">Optional action to configure connection options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A problem details if some error occurs. List of packages if successful.</returns>
        Task<Result<CreateDelegationResponseDto>> AddBankruptcyEstateForUser(Guid party, Guid estate, Guid user, List<PackageReferenceDto> packages, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the list of bankruptcy estates for a specific user.
        /// </summary>
        /// <param name="party">The entity the rightholder relationship is defined for</param>
        /// <param name="estate">The bankruptcyestate identifier to fetch</param>
        /// <param name="user">The user identifier to fetch</param>
        /// <param name="packages">The packages to revoke from the delegation.</param>
        /// <param name="configureConnections">Optional action to configure connection options.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A problem details if some error occurs. List of packages if successful.</returns>
        Task<Result<bool>> RevokeBankruptcyEstateForUser(Guid party, Guid estate, Guid user, List<PackageReferenceDto> packages, Action<ConnectionOptions> configureConnections, CancellationToken cancellationToken);
    }
}
