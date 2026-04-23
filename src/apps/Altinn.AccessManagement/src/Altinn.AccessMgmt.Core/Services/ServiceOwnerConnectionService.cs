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
        AppDbContext dbContext) : IServiceOwnerConnectionService
    {
        /// <summary>
        /// Allows service owners to 
        /// </summary>
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
    }
}
