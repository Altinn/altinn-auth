using Altinn.AccessManagement.Core.Errors;
using Altinn.AccessMgmt.Core.Validation;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Api.Enduser.Validation;

/// <summary>
/// Cross-field (semantic) validation rules that relate multiple connection parameters.
/// Assumes atomic parameter validation (ConnectionParameterRules) has already run.
/// Only adds errors for relational constraints; skips work if any involved value is syntactically invalid.
/// </summary>
internal static class ConnectionCombinationRules
{
    /// <summary>
    /// party must equal from (used for add connection / add assignment scenarios where from must be actor party).
    /// </summary>
    internal static RuleExpression PartyEqualsFrom(string party, string from) => () =>
    {
        if (!Guid.TryParse(party, out var partyId) || partyId == Guid.Empty)
        {
            return null;
        }

        if (!Guid.TryParse(from, out var fromId) || fromId == Guid.Empty)
        {
            return null;
        }

        if (partyId != fromId)
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, "QUERY/from", [new("from", ValidationMessageTexts.PartyMustMatchFrom)]);
        }

        return null;
    };

    /// <summary>
    /// party must match either from or to (used for read scenarios or remove connection where actor can be either side).
    /// </summary>
    internal static RuleExpression PartyMatchesFromOrTo(string party, string from, string to) => () =>
    {
        if (!Guid.TryParse(party, out var partyId) || partyId == Guid.Empty)
        {
            return null;
        }

        bool fromValid = Guid.TryParse(from, out var fromId) && fromId != Guid.Empty;
        bool toValid = Guid.TryParse(to, out var toId) && toId != Guid.Empty;
        if (!fromValid && !toValid)
        {
            return null;
        }

        if ((fromValid && fromId == partyId) || (toValid && toId == partyId))
        {
            return null;
        }

        return (ref ValidationErrorBuilder errors) =>
        {
            if (fromValid)
            {
                errors.Add(ValidationErrors.InvalidQueryParameter, "QUERY/from", [new("from", ValidationMessageTexts.FromOrToMustMatchParty)]);
            }

            if (toValid)
            {
                errors.Add(ValidationErrors.InvalidQueryParameter, "QUERY/to", [new("to", ValidationMessageTexts.FromOrToMustMatchParty)]);
            }
        };
    };

    /// <summary>
    /// For remove scenarios: party must match from or to; both from and to required.
    /// </summary>      
    internal static RuleExpression RemovePartyMatchesFromOrTo(string party, string from, string to) => () =>
    {
        if (!Guid.TryParse(party, out var partyId) || partyId == Guid.Empty)
        {
            return null;
        }

        if (!Guid.TryParse(from, out var fromId) || fromId == Guid.Empty)
        {
            return null;
        }

        if (!Guid.TryParse(to, out var toId) || toId == Guid.Empty)
        {
            return null;
        }

        if (partyId == fromId || partyId == toId)
        {
            return null;
        }

        return (ref ValidationErrorBuilder errors) =>
        {
            errors.Add(ValidationErrors.InvalidQueryParameter, "QUERY/from", [new("from", ValidationMessageTexts.FromOrToMustMatchParty)]);
            errors.Add(ValidationErrors.InvalidQueryParameter, "QUERY/to", [new("to", ValidationMessageTexts.FromOrToMustMatchParty)]);
        };
    };

    /// <summary>
    /// Package reference must be exactly one of (packageId, packageUrn). Reject empty Guid.
    /// </summary>
    internal static RuleExpression ExclusivePackageReference(Guid? packageId, string packageUrn, string idName = "packageId", string urnName = "package") => () =>
    {
        var urnProvided = !string.IsNullOrWhiteSpace(packageUrn);
        var idProvided = packageId.HasValue && packageId.Value != Guid.Empty;
        if (idProvided ^ urnProvided)
        {
            return null; // exactly one OK
        }

        if (idProvided && urnProvided)
        {
            return (ref ValidationErrorBuilder errors) =>
            {
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{idName}", [new(idName, ValidationMessageTexts.ProvideEitherPackageRef)]);
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{urnName}", [new(urnName, ValidationMessageTexts.ProvideEitherPackageRef)]);
            };
        }

        if (packageId.HasValue && packageId.Value == Guid.Empty)
        {
            return (ref ValidationErrorBuilder errors) =>
                errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{idName}", [new(idName, ValidationMessageTexts.PackageIdMustNotBeEmpty)]);
        }

        return (ref ValidationErrorBuilder errors) =>
        {
            errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{idName}", [new(idName, ValidationMessageTexts.RequireOnePackageRef)]);
            errors.Add(ValidationErrors.InvalidQueryParameter, $"QUERY/{urnName}", [new(urnName, ValidationMessageTexts.RequireOnePackageRef)]);
        };
    };
}
