using Altinn.AccessManagement.Core.Helpers.Extensions;
using Altinn.AccessManagement.Core.Models;
using Altinn.Authorization.Api.Contracts.AccessManagement.Apps;
using Altinn.Authorization.Api.Contracts.AccessManagement.Apps.Enums;
using Altinn.Urn.Json;

namespace Altinn.AccessManagement.Api.Apps.Utils;

/// <summary>
/// Maps between the Apps API contracts and the core models.
/// </summary>
public static class ModelMapper
{
    /// <summary>
    /// Maps a delegation or revoke request to the core model.
    /// </summary>
    public static AppsInstanceDelegationRequest ToCore(AppsInstanceDelegationRequestDto dto)
    {
        return new AppsInstanceDelegationRequest
        {
            From = dto.From.Value,
            To = dto.To.Value,
            Rights = dto.Rights?.Select(ToCore).ToList(),
        };
    }

    /// <summary>
    /// Maps a delegation response to the contract.
    /// </summary>
    public static AppsInstanceDelegationResponseDto ToDto(AppsInstanceDelegationResponse response)
    {
        return new AppsInstanceDelegationResponseDto
        {
            From = response.From,
            To = response.To,
            ResourceId = response.ResourceId,
            InstanceId = response.InstanceId,
            Rights = response.Rights?.Select(ToDto).ToList(),
        };
    }

    /// <summary>
    /// Maps a revoke response to the contract.
    /// </summary>
    public static AppsInstanceRevokeResponseDto ToDto(AppsInstanceRevokeResponse response)
    {
        return new AppsInstanceRevokeResponseDto
        {
            From = response.From,
            To = response.To,
            ResourceId = response.ResourceId,
            InstanceId = response.InstanceId,
            Rights = response.Rights?.Select(ToDto).ToList(),
        };
    }

    /// <summary>
    /// Maps a delegation check result to the contract.
    /// </summary>
    public static ResourceRightDelegationCheckResultDto ToDto(ResourceRightDelegationCheckResult result)
    {
        return new ResourceRightDelegationCheckResultDto
        {
            RightKey = result.RightKey,
            Resource = result.Resource?.Select(urn => (UrnJsonTypeValue)urn).ToList(),
            Action = result.Action,
            Status = result.Status.MapByNameOrValue<DelegableStatusExternal>(),
            Details = result.Details?.Select(ToDto).ToList(),
        };
    }

    private static RightInternal ToCore(AppsInstanceRightDto dto)
    {
        return new RightInternal
        {
            Resource = dto.Resource?.ToList(),
            Action = dto.Action.Value,
        };
    }

    private static RightDelegationResultDto ToDto(InstanceRightDelegationResult right)
    {
        return new RightDelegationResultDto
        {
            Resource = right.Resource,
            Action = right.Action,
            Status = right.Status.MapByNameOrValue<DelegationStatusExternal>(),
        };
    }

    private static RightRevokeResultDto ToDto(InstanceRightRevokeResult right)
    {
        return new RightRevokeResultDto
        {
            Resource = right.Resource,
            Action = right.Action,
            Status = right.Status.MapByNameOrValue<RevokeStatusExternal>(),
        };
    }

    private static DetailExternal ToDto(Detail detail)
    {
        return new DetailExternal
        {
            Code = detail.Code.MapByNameOrValue<DetailCodeExternal>(),
            Description = detail.Description,
            Parameters = detail.Parameters?.ToDictionary(p => p.Key, p => p.Value?.Select(ToDto).ToList()),
        };
    }

    private static AttributeMatchExternal ToDto(AttributeMatch match)
    {
        return new AttributeMatchExternal
        {
            Id = match.Id,
            Value = match.Value,
        };
    }
}
