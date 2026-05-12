using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement;

namespace Altinn.AccessMgmt.Core.Services
{
    public class ServiceOwnerConnectionService(
        AppDbContext dbContext,
        IConnectionService connectionService,
        IFeatureManager featureManager) : IServiceOwnerConnectionService
    {
        /// <inheritdoc />
        public async Task<Result<AssignmentPackageDto>> AddPackage(Guid fromId, Guid toId, Guid packageId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
        {
            var options = new ConnectionOptions(configureConnection);

            // Validate From / To entity types against the configured options.
            var (fromEntity, toEntity) = await ConnectionWriteValidation.GetFromAndToEntitiesAsync(dbContext, fromId, toId, cancellationToken);
            var problem = ConnectionWriteValidation.ValidateWriteOpInput(fromEntity, toEntity, options);
            if (problem is not null)
            {
                return problem;
            }

            // Look for existing direct rightholder assignment
            Assignment assignment = await dbContext.Assignments
                .Where(a => a.FromId == fromId)
                .Where(a => a.ToId == toId)
                .Where(a => a.RoleId == RoleConstants.Rightholder)
                .FirstOrDefaultAsync(cancellationToken);

            if (assignment == null)
            {
                assignment = new Assignment()
                {
                    FromId = fromId,
                    ToId = toId,
                    RoleId = RoleConstants.Rightholder
                };

                await dbContext.Assignments.AddAsync(assignment, cancellationToken);
            }

            // Check if package already assigned
            AssignmentPackage existingAssignmentPackage = await dbContext.AssignmentPackages
                .AsNoTracking()
                .Where(a => a.AssignmentId == assignment.Id)
                .Where(a => a.PackageId == packageId)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingAssignmentPackage is { })
            {
                return DtoMapper.Convert(existingAssignmentPackage);
            }

            var newAssignmentPackage = new AssignmentPackage()
            {
                AssignmentId = assignment.Id,
                PackageId = packageId,
            };

            await dbContext.AssignmentPackages.AddAsync(newAssignmentPackage, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            return DtoMapper.Convert(newAssignmentPackage);
        }

        /// <inheritdoc />>
        public async Task<Result<bool>> RevokePackage(Guid fromId, Guid toId, Guid packageId, Guid autenticatedServiceOwnerId, CancellationToken cancellationToken = default)
        {
            // Look for existing direct rightholder assignment
            Assignment assignment = await dbContext.Assignments
                .Where(a => a.FromId == fromId)
                .Where(a => a.ToId == toId)
                .Where(a => a.RoleId == RoleConstants.Rightholder)
                .FirstOrDefaultAsync(cancellationToken);

            // Return if no assignment exsists
            if (assignment == null)
            {
                return false;
            }

            // Fetch assigned package
            AssignmentPackage assignmentPackage = await dbContext.AssignmentPackages
                .AsNoTracking()
                .Where(a => a.AssignmentId == assignment.Id)
                .Where(a => a.PackageId == packageId)
                .FirstOrDefaultAsync(cancellationToken);

            // Return if no assignment package exsists
            if (assignmentPackage == null)
            {
                return false;
            }

            // Check if assignment package is delegated by authorized entity
            if (assignmentPackage.Audit_ChangedBy != autenticatedServiceOwnerId)
            {
                return Problems.PackageNotRevocableFromAssignment;
            }

            // If the revoked package is InnbyggerSkatteforholdPrivatpersoner, we need to check if there is an existing
            // PrivateTaxAffairs assignment delegated by the serviceowner that also needs to be revoked.
            if (await featureManager.IsEnabledAsync(AccessMgmtFeatureFlags.Altinn2RoleRevoke))
            {
                if (packageId == PackageConstants.InnbyggerSkatteforholdPrivatpersoner.Id)
                {
                    // Look for existing direct PrivateTaxAffairs assignment delegated by the serviceowner
                    Assignment skatteforholdRole = await dbContext.Assignments
                        .Where(a => a.FromId == fromId)
                        .Where(a => a.ToId == toId)
                        .Where(a => a.RoleId == RoleConstants.PrivateTaxAffairs)
                        .Where(a => a.Audit_ChangedBy == autenticatedServiceOwnerId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (skatteforholdRole is not null)
                    {
                        // Revoke PrivateTaxAffairs assignment
                        dbContext.Assignments.Remove(skatteforholdRole);
                    }
                }
            }

            // Revoke assignment package
            dbContext.AssignmentPackages.Remove(assignmentPackage);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Remove assignment if we now deleted the last connection to the assignment.
            await RemoveAssignment(assignment, false, cancellationToken);

            return true;
        }

        private async Task<ValidationProblemInstance> RemoveAssignment(Assignment assignment, bool cascade = false, CancellationToken cancellationToken = default)
        {
            if (!cascade)
            {
                var problem = await connectionService.CheckAssignmentForConnectedRefernces(assignment.Id, cancellationToken);

                if (problem is { })
                {
                    return problem;
                }
            }

            dbContext.Remove(assignment);
            await dbContext.SaveChangesAsync(cancellationToken);

            return null;
        }
    }
}
