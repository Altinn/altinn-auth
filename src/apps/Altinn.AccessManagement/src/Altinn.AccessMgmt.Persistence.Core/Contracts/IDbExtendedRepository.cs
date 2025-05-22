using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Core.Contracts;

/// <summary>
/// Represents an extended repository that provides additional data access operations
/// for a primary entity of type <typeparamref name="T"/> along with its extended representation of type <typeparamref name="TExtended"/>.
/// </summary>
/// <typeparam name="T">The primary entity type.</typeparam>
/// <typeparam name="TExtended">
/// The extended entity type which includes additional information or computed properties related to the primary entity.
/// </typeparam>
public interface IDbExtendedRepository<T, TExtended> : IDbBasicRepository<T>
{
    /// <summary>
    /// Retrieves the extended entity corresponding to the specified unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the primary entity.</param>
    /// <param name="options">
    /// Optional request options, such as language, paging, or as-of date. If null, default options are applied.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <param name="callerName">
    /// The name of the calling method, automatically provided by the compiler. 
    /// Used to set the span name for OpenTelemetry tracing to aid in observability and diagnostics.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the extended entity if found; otherwise, null.
    /// </returns>
    Task<TExtended> GetExtended(Guid id, RequestOptions options = null, CancellationToken cancellationToken = default, [CallerMemberName] string callerName = "");

    /// <summary>
    /// Retrieves a collection of extended entities.
    /// </summary>
    /// <param name="options">
    /// Optional request options, such as language, paging, or as-of date. If null, default options are applied.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <param name="callerName">
    /// The name of the calling method, automatically provided by the compiler. 
    /// Used to set the span name for OpenTelemetry tracing to aid in observability and diagnostics.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a collection of extended entities.
    /// </returns>
    Task<QueryResponse<TExtended>> GetExtended(RequestOptions options = null, CancellationToken cancellationToken = default, [CallerMemberName] string callerName = "");

    /// <summary>
    /// Retrieves a collection of extended entities based on property and value
    /// </summary>
    /// <param name="property">Filter property</param>
    /// <param name="value">Filter value</param>
    /// <param name="options">RequestOptions</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <param name="callerName">
    /// The name of the calling method, automatically provided by the compiler. 
    /// Used to set the span name for OpenTelemetry tracing to aid in observability and diagnostics.
    /// </param>
    /// <returns>T<see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
    Task<QueryResponse<TExtended>> GetExtended<TProperty>(Expression<Func<TExtended, TProperty>> property, TProperty value, RequestOptions options = null, CancellationToken cancellationToken = default, [CallerMemberName] string callerName = "");

    /// <summary>
    /// Retrieves a collection of extended entities that match the criteria specified by a <see cref="GenericFilterBuilder{TExtended}"/>.
    /// </summary>
    /// <param name="filter">
    /// A filter builder specifying the criteria for retrieving extended entities.
    /// </param>
    /// <param name="options">
    /// Optional request options, such as language, paging, or as-of date. If null, default options are applied.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <param name="callerName">
    /// The name of the calling method, automatically provided by the compiler. 
    /// Used to set the span name for OpenTelemetry tracing to aid in observability and diagnostics.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a collection of extended entities that match the specified criteria.
    /// </returns>
    Task<QueryResponse<TExtended>> GetExtended(GenericFilterBuilder<TExtended> filter, RequestOptions options = null, CancellationToken cancellationToken = default, [CallerMemberName] string callerName = "");

    /// <summary>
    /// Retrieves a collection of extended entities that match the provided filter criteria.
    /// </summary>
    /// <param name="filters">A collection of filter conditions to apply.</param>
    /// <param name="options">
    /// Optional request options, such as language, paging, or as-of date. If null, default options are applied.
    /// </param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <param name="callerName">
    /// The name of the calling method, automatically provided by the compiler. 
    /// Used to set the span name for OpenTelemetry tracing to aid in observability and diagnostics.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a collection of extended entities that satisfy the provided filters.
    /// </returns>
    Task<QueryResponse<TExtended>> GetExtended(IEnumerable<GenericFilter> filters, RequestOptions options = null, CancellationToken cancellationToken = default, [CallerMemberName] string callerName = "");
}
