using Altinn.AccessMgmt.Core.Services;
using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;

namespace Altinn.AccessManagement.Api.Enduser.Controllers.Base;

/// <summary>
/// One area of the activity log: the slice of events it serves, the filter fields it offers
/// and the catalog entries it accepts as typeId input. The area is the authorization
/// boundary — the concrete controller declares route, feature flag and policies, and the
/// base constrains every query to the area.
/// </summary>
/// <param name="Types">The main record types the area serves (the slice's types, used for
/// catalog gating and the 400-level validation; the service clamps to the same slice).</param>
/// <param name="Subtypes">The subtypes the area accepts in typeId input and lists under
/// types, or null for all. Query rows are already scoped by <paramref name="Types"/> and the
/// service slice.</param>
/// <param name="Slice">The service surface serving this area.</param>
/// <param name="Fields">The filter fields the area offers; filter lookups on other fields
/// are rejected.</param>
public sealed record ActivityLogArea(
    IReadOnlyList<ActivityLogType> Types,
    IReadOnlyList<ActivityLogSubtype?> Subtypes,
    ActivityLogSlice Slice,
    IReadOnlyList<ActivityLogFilterField> Fields)
{
    /// <summary>
    /// Whether a catalog entry belongs to this area.
    /// </summary>
    public bool AcceptsCatalogEntry(ActivityLogType type, ActivityLogSubtype? subtype)
        => Types.Contains(type) && (Subtypes is null || Subtypes.Contains(subtype));
}

/// <summary>
/// The per-area service surface an area is served by. The slices are disjoint and clamped in
/// the service itself, so they can never mix.
/// </summary>
public enum ActivityLogSlice
{
    /// <summary>Assignment and delegation events, Maskinporten schema events excluded.</summary>
    Connections,

    /// <summary>Access request events.</summary>
    Request,

    /// <summary>Only the Maskinporten schema (Supplier-role) events.</summary>
    Maskinporten,
}

/// <summary>
/// The activity log areas: connections (assignments and delegations), request (access
/// requests) and maskinporten (Maskinporten schema delegations — the Supplier-role slice).
/// </summary>
public static class ActivityLogAreas
{
    /// <summary>
    /// Assignment and delegation events, with Maskinporten schema events excluded as
    /// everywhere else.
    /// </summary>
    public static readonly ActivityLogArea Connections = new(
        Types: ActivityLogSlices.Connections,
        Subtypes: null,
        Slice: ActivityLogSlice.Connections,
        Fields:
        [
            ActivityLogFilterField.From,
            ActivityLogFilterField.To,
            ActivityLogFilterField.Via,
            ActivityLogFilterField.By,
            ActivityLogFilterField.Role,
            ActivityLogFilterField.Package,
            ActivityLogFilterField.Resource,
            ActivityLogFilterField.Source,
            ActivityLogFilterField.ActivityType,
        ]);

    /// <summary>
    /// Access request events, including their package/resource children and status changes.
    /// </summary>
    public static readonly ActivityLogArea Request = new(
        Types: ActivityLogSlices.Request,
        Subtypes: null,
        Slice: ActivityLogSlice.Request,
        Fields:
        [
            ActivityLogFilterField.From,
            ActivityLogFilterField.To,
            ActivityLogFilterField.By,
            ActivityLogFilterField.Package,
            ActivityLogFilterField.Resource,
            ActivityLogFilterField.Source,
            ActivityLogFilterField.ActivityType,
        ]);

    /// <summary>
    /// Maskinporten schema delegation events: assignment main-row and resource events carrying
    /// the Supplier role, which every other area hides.
    /// </summary>
    public static readonly ActivityLogArea Maskinporten = new(
        Types: ActivityLogSlices.Maskinporten,
        Subtypes: [null, ActivityLogSubtype.Resource],
        Slice: ActivityLogSlice.Maskinporten,
        Fields:
        [
            ActivityLogFilterField.From,
            ActivityLogFilterField.To,
            ActivityLogFilterField.By,
            ActivityLogFilterField.Resource,
            ActivityLogFilterField.Source,
            ActivityLogFilterField.ActivityType,
        ]);
}
