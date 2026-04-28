using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Services.Contracts;
using Altinn.AccessMgmt.Core.Utils;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Services
{
    public class ServiceOwnerConnectionService(
        AppDbContext dbContext,
        IConnectionService connectionService) : IServiceOwnerConnectionService
    {
        /// <inheritdoc />
        public async Task<Result<AssignmentPackageDto>> AddPackage(Guid fromId, Guid toId, Guid packageId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
        {
            ConnectionOptions options = new(configureConnection);

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
