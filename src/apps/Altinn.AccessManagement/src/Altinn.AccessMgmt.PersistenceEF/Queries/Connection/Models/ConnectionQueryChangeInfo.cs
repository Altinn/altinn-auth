namespace Altinn.AccessMgmt.PersistenceEF.Queries.Connection.Models;

/// <summary>
/// Who last changed an assignment row (package, resource or instance), and when.
/// Read from <c>audit_changedby</c> and <c>audit_validfrom</c> on the current version of the row.
/// </summary>
/// <param name="ValidFrom">When the current version of the row became valid, i.e. the last change.</param>
/// <param name="ChangedBy">The entity that made the last change, or <c>null</c> if not recorded.</param>
public sealed record ConnectionQueryChangeInfo(DateTimeOffset ValidFrom, Guid? ChangedBy);
