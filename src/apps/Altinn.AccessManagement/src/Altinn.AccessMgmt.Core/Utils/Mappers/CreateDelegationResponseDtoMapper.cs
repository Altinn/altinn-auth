using Altinn.AccessMgmt.PersistenceEF.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement;

namespace Altinn.AccessMgmt.Core.Utils;

/// <summary>
/// Utility for mapping of entity models to DTOs, related to creating a new client delegation from internal systemuser API.
/// </summary>
public partial class DtoMapper
{
    public static CreateDelegationResponseDto Convert(Delegation delegation)
    {
        return new CreateDelegationResponseDto
        {
            DelegationId = delegation.Id,
            FromEntityId = delegation.From.FromId
        };
    }

    public static IEnumerable<CreateDelegationResponseDto> Convert(IEnumerable<Delegation> delegations)
    {
        return delegations.Select(Convert).ToList();
    }
}
