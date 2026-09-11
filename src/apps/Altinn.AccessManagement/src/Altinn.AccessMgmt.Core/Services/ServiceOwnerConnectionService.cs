using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessManagement.Core.Models;
using Altinn.AccessManagement.Core.Services.Interfaces;
using Altinn.AccessMgmt.Core.Services.Contracts;
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
    public class ServiceOwnerConnectionService(
        AppDbContext dbContext,
        IConnectionService connectionService,
        IContextRetrievalService contextRetrievalService,
        ISingleRightsService singleRightsService) : IServiceOwnerConnectionService
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

            // Revoke assignment package
            dbContext.AssignmentPackages.Remove(assignmentPackage);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Remove assignment if we now deleted the last connection to the assignment.
            await RemoveAssignment(assignment, false, cancellationToken);

            return true;
        }

        /// <inheritdoc />
        public async Task<Result<AssignmentResourceDto>> AddResource(Guid fromId, Guid toId, Resource resource, IEnumerable<string> rightKeys, Guid authenticatedServiceOwnerId, Action<ConnectionOptions> configureConnection = null, CancellationToken cancellationToken = default)
        {
            if (resource is null)
            {
                return Problems.InvalidResource;
            }

            // MaskinportenSchema resources are delegated through the maskinporten delegation API, not as rightholder resources.
            if (string.Equals(resource.Type?.Name, "MaskinportenSchema", StringComparison.OrdinalIgnoreCase))
            {
                return Problems.ResourceNotDelegable;
            }

            List<string> keys = rightKeys?
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [];

            if (keys.Count == 0)
            {
                return Problems.MissingRightKey;
            }

            var options = new ConnectionOptions(configureConnection);

            // Validate From / To entity types against the configured options.
            var (fromEntity, toEntity) = await ConnectionWriteValidation.GetFromAndToEntitiesAsync(dbContext, fromId, toId, cancellationToken);
            var problem = ConnectionWriteValidation.ValidateWriteOpInput(fromEntity, toEntity, options);
            if (problem is not null)
            {
                return problem;
            }

            Entity serviceOwner = await dbContext.Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == authenticatedServiceOwnerId, cancellationToken);

            if (serviceOwner is null)
            {
                return Problems.PartyNotFound;
            }

            // Validate that all requested right keys exist in the resource policy
            List<RightDto> availableRights = await contextRetrievalService.GetResourcePolicyV2(resource.RefId, cancellationToken: cancellationToken);
            if (availableRights is null)
            {
                return Problems.MissingMetadata;
            }

            if (keys.Any(key => !availableRights.Any(right => string.Equals(right.Key, key, StringComparison.OrdinalIgnoreCase))))
            {
                return Problems.InvalidRightKey;
            }

            // Look for existing direct rightholder assignment
            Assignment assignment = await dbContext.Assignments
                .Where(a => a.FromId == fromId)
                .Where(a => a.ToId == toId)
                .Where(a => a.RoleId == RoleConstants.Rightholder)
                .FirstOrDefaultAsync(cancellationToken);

            bool assignmentCreated = false;
            if (assignment == null)
            {
                assignment = new Assignment()
                {
                    FromId = fromId,
                    ToId = toId,
                    RoleId = RoleConstants.Rightholder
                };

                await dbContext.Assignments.AddAsync(assignment, cancellationToken);
                await dbContext.SaveChangesAsync(cancellationToken);
                assignmentCreated = true;
            }

            // Write the delegation policy. The policy administration point stores the assignment resource for the rightholder assignment.
            List<Rule> rules = await singleRightsService.TryWriteDelegationPolicyRules(fromEntity, toEntity, resource, keys, serviceOwner, ignoreExistingPolicy: false, cancellationToken: cancellationToken);
            if (rules.Count == 0 || !rules.All(rule => rule.CreatedSuccessfully))
            {
                if (assignmentCreated)
                {
                    dbContext.Assignments.Remove(assignment);
                    await dbContext.SaveChangesAsync(cancellationToken);
                }

                return Problems.DelegationPolicyRuleWriteFailed;
            }

            AssignmentResource assignmentResource = await dbContext.AssignmentResources
                .AsNoTracking()
                .Where(a => a.AssignmentId == assignment.Id)
                .Where(a => a.ResourceId == resource.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (assignmentResource is null)
            {
                return Problems.DelegationPolicyRuleWriteFailed;
            }

            return DtoMapper.Convert(assignmentResource);
        }

        /// <inheritdoc />
        public async Task<Result<bool>> RevokeResource(Guid fromId, Guid toId, Guid resourceId, Guid authenticatedServiceOwnerId, CancellationToken cancellationToken = default)
        {
            // Look for existing direct rightholder assignment
            Assignment assignment = await dbContext.Assignments
                .Where(a => a.FromId == fromId)
                .Where(a => a.ToId == toId)
                .Where(a => a.RoleId == RoleConstants.Rightholder)
                .FirstOrDefaultAsync(cancellationToken);

            // Return if no assignment exists
            if (assignment == null)
            {
                return false;
            }

            // Fetch assigned resource
            AssignmentResource assignmentResource = await dbContext.AssignmentResources
                .Where(a => a.AssignmentId == assignment.Id)
                .Where(a => a.ResourceId == resourceId)
                .FirstOrDefaultAsync(cancellationToken);

            // Return if no assignment resource exists
            if (assignmentResource == null)
            {
                return false;
            }

            // Check if assignment resource is delegated by authorized entity
            if (assignmentResource.Audit_ChangedBy != authenticatedServiceOwnerId)
            {
                return Problems.ResourceNotRevocableFromAssignment;
            }

            // Clear the delegation policy before removing the assignment resource
            await singleRightsService.ClearPolicyRules(assignmentResource.PolicyPath, assignmentResource.PolicyVersion, cancellationToken);

            dbContext.AssignmentResources.Remove(assignmentResource);
            await dbContext.SaveChangesAsync(cancellationToken);

            // Remove the assignment if it was created by the service owner and we now deleted the last connection to it.
            if (assignment.Audit_ChangedBy == authenticatedServiceOwnerId)
            {
                await RemoveAssignment(assignment, false, cancellationToken);
            }

            return true;
        }

        /// <inheritdoc />
        public async Task<Result<List<RightDto>>> GetResourceRights(string resource, string languageCode = "nb", CancellationToken cancellationToken = default)
        {
            List<RightDto> rights = await contextRetrievalService.GetResourcePolicyV2(resource, languageCode, cancellationToken);
            if (rights is null)
            {
                return Problems.MissingMetadata;
            }

            return rights;
        }

        private async Task<ValidationProblemInstance> RemoveAssignment(Assignment assignment, bool cascade = false, CancellationToken cancellationToken = default)
        {
            if (!cascade)
            {
                var problem = await connectionService.CheckAssignmentForConnectedReferences(assignment, null, cancellationToken);

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
