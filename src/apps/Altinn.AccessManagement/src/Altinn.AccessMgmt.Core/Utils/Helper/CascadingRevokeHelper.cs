using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Contexts;
using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.ProblemDetails;
using Microsoft.EntityFrameworkCore;

namespace Altinn.AccessMgmt.Core.Utils.Helper
{
    public class CascadingRevokeHelper
    {
        public static async Task<ValidationErrorBuilder> CheckCascadingDependenciesAgentAssignment(AppDbContext db, Assignment existingAssignment, CancellationToken cancellationToken = default)
        {
            ValidationErrorBuilder errorBuilder = default;

            if (existingAssignment is null)
            {
                return errorBuilder;
            }

            var existingDelegations = await db.Delegations
                .AsNoTracking()
                .Where(p => p.ToId == existingAssignment.Id)
                .Include(p => p.DelegationPackages)
                .Include(p => p.DelegationResources)
                .Where(p => p.DelegationPackages.Any() || p.DelegationResources.Any())
                .AsSplitQuery()
                .ToListAsync(cancellationToken);

            foreach (var existingDelegation in existingDelegations)
            {
                if (existingDelegation.DelegationPackages.Count > 0)
                {
                    var pkgs = string.Join(", ", existingDelegation.DelegationPackages.Select(p => p.PackageId));
                    errorBuilder.Add(
                        ValidationErrors.DelegationHasActiveConnections,
                        "$QUERY/cascade",
                        [
                            new($"{existingDelegation.Id}", $"Cannot remove delegation '{existingDelegation.Id}' because party '{existingAssignment.ToId}' still has active delegated packages <{pkgs}> from party '{existingDelegation.FromId}'.")
                        ]
                    );
                }

                if (existingDelegation.DelegationResources.Count > 0)
                {
                    var resources = string.Join(", ", existingDelegation.DelegationResources.Select(r => r.ResourceId));
                    errorBuilder.Add(
                        ValidationErrors.DelegationHasActiveConnections,
                        "$QUERY/cascade",
                        [
                            new($"{existingDelegation.Id}", $"Cannot remove delegation '{existingDelegation.Id}' because party '{existingAssignment.ToId}' still has active delegated resources <{resources}> from party '{existingDelegation.FromId}'.")
                        ]
                    );
                }
            }

            return errorBuilder;
        }        
    }
}
