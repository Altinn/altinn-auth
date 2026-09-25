using Altinn.Authorization.Api.Contracts.AccessManagement.ActivityLog;
using Altinn.Authorization.Api.Contracts.AccessManagement.Request;

namespace Altinn.AccessMgmt.PersistenceEF.Queries;

/// <summary>
/// One activity type catalog key used as a filter conjunction: an entry matches when all set
/// parts match. <paramref name="Subtype"/> <see langword="null"/> is an exact match (entries
/// about the main record have null subtype); <paramref name="Status"/> <see langword="null"/>
/// is a wildcard (request child entries always carry a status, so the fallback row must match
/// them all). Multiple keys in a filter are OR'ed together.
/// </summary>
public sealed record ActivityTypeKey(ActivityLogType Type, ActivityLogSubtype? Subtype, ActivityLogTrigger Trigger, RequestStatus? Status);
