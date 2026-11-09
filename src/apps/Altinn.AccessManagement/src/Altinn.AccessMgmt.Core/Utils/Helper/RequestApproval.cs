using Altinn.AccessManagement.Core.Errors;
using Altinn.Authorization.Api.Contracts.AccessManagement;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Utils.Helper;

/// <summary>
/// Rules for approving a resource request with a selection of rights (right keys).
/// </summary>
public static class RequestApproval
{
    /// <summary>
    /// The status a resource request gets when it is approved.
    /// <list type="bullet">
    /// <item>No selection: what was requested is given, so the request is <see cref="RequestStatusV2.Approved"/>.</item>
    /// <item>A selection for a request for the whole resource (no requested rights): <see cref="RequestStatusV2.PartialApprove"/>.</item>
    /// <item>A selection with exactly the requested rights, in any order: <see cref="RequestStatusV2.Approved"/>.</item>
    /// <item>Any other selection, with fewer, other or more rights than requested: <see cref="RequestStatusV2.PartialApprove"/>.</item>
    /// </list>
    /// </summary>
    /// <param name="requestedRightKeys">The rights requested; null or empty when the whole resource was requested</param>
    /// <param name="selectedRightKeys">The rights the approver selected; null or empty when no selection was made</param>
    public static RequestStatusV2 DetermineStatus(IEnumerable<string>? requestedRightKeys, IEnumerable<string>? selectedRightKeys)
    {
        var selected = selectedRightKeys?.ToHashSet(StringComparer.Ordinal) ?? [];
        if (selected.Count == 0)
        {
            return RequestStatusV2.Approved;
        }

        var requested = requestedRightKeys?.ToHashSet(StringComparer.Ordinal) ?? [];
        if (requested.Count == 0)
        {
            return RequestStatusV2.PartialApprove;
        }

        return selected.SetEquals(requested) ? RequestStatusV2.Approved : RequestStatusV2.PartialApprove;
    }

    /// <summary>
    /// Validates that the authenticated user can delegate every right key, given the result of a delegation check
    /// for the resource on behalf of the party. Each key that cannot be delegated is reported at
    /// <paramref name="jsonPointer"/> followed by its index, e.g. <c>/rightKeys/0</c>.
    /// </summary>
    /// <param name="rightKeys">The right keys to give</param>
    /// <param name="rights">The rights from the delegation check</param>
    /// <param name="jsonPointer">Where the keys are in the request, e.g. <c>/rightKeys</c></param>
    /// <returns>Null when every key can be delegated, otherwise the problem to return.</returns>
    public static ValidationProblemInstance? ValidateUserCanDelegate(IReadOnlyList<string> rightKeys, IEnumerable<RightCheckDto> rights, string jsonPointer)
    {
        HashSet<string> delegableKeys = [.. rights.Where(right => right.Result).Select(right => right.Right.Key)];

        ValidationErrorBuilder errors = default;
        for (int i = 0; i < rightKeys.Count; i++)
        {
            var key = rightKeys[i];
            if (!delegableKeys.Contains(key))
            {
                errors.Add(ValidationErrors.RightKeyNotDelegableByUser, $"{jsonPointer}/{i}", [new("rightKey", $"Right key '{key}' cannot be delegated by the authenticated user.")]);
            }
        }

        errors.TryBuild(out var problem);
        return problem;
    }
}
