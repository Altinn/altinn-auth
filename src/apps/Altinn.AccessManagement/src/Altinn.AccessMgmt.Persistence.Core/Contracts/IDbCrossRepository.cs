using Altinn.AccessMgmt.Persistence.Core.Helpers;
using Altinn.AccessMgmt.Persistence.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Core.Contracts;

/// <summary>
/// Defines a repository that supports retrieving related entities via a cross-reference table.
/// This repository extends <see cref="IDbExtendedRepository{T, TExtended}"/> by providing 
/// methods to fetch related entities of types <typeparamref name="TA"/> and <typeparamref name="TB"/>.
/// </summary>
/// <typeparam name="T">The primary entity type.</typeparam>
/// <typeparam name="TExtended">
/// The extended entity type that includes additional or computed properties related to the primary entity.
/// </typeparam>
/// <typeparam name="TA">The type of the first related entity set in the cross-reference.</typeparam>
/// <typeparam name="TB">The type of the second related entity set in the cross-reference.</typeparam>
public interface IDbCrossRepository<T, TExtended, TA, TB> : IDbExtendedRepository<T, TExtended>
{
    /// <summary>
    /// Retrieves a collection of related entities of type <typeparamref name="TA"/> that are associated 
    /// with the primary entity of type <typeparamref name="T"/> through the cross-reference table.
    /// </summary>
    /// <param name="id">The unique identifier of the primary entity.</param>
    /// <param name="options">The request options that specify query parameters such as pagination, sorting, or language preferences.</param>
    /// <param name="filters">A collection of generic filters used to refine the query results.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the collection of related entities of type <typeparamref name="TA"/>.
    /// </returns>
    Task<IEnumerable<TA>> GetA(Guid id, RequestOptions options = null, List<GenericFilter> filters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a collection of related entities of type <typeparamref name="TA"/> that are associated 
    /// with the primary entity of type <typeparamref name="T"/> through the cross-reference table.
    /// </summary>
    /// <param name="filters">A collection of generic filters used to refine the query results.</param>
    /// <param name="options">The request options that specify query parameters such as pagination, sorting, or language preferences.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the collection of related entities of type <typeparamref name="TA"/>.
    /// </returns>
    Task<IEnumerable<TA>> GetA(List<GenericFilter> filters, RequestOptions options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a collection of related entities of type <typeparamref name="TB"/> that are associated 
    /// with the primary entity of type <typeparamref name="T"/> through the cross-reference table.
    /// </summary>
    /// <param name="id">The unique identifier of the primary entity.</param>
    /// <param name="options">The request options that specify query parameters such as pagination, sorting, or language preferences.</param>
    /// <param name="filters">A collection of generic filters used to refine the query results.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the collection of related entities of type <typeparamref name="TB"/>.
    /// </returns>
    Task<IEnumerable<TB>> GetB(Guid id, RequestOptions options = null, List<GenericFilter> filters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a collection of related entities of type <typeparamref name="TB"/> that are associated 
    /// with the primary entity of type <typeparamref name="T"/> through the cross-reference table.
    /// </summary>
    /// <param name="filters">A collection of generic filters used to refine the query results.</param>
    /// <param name="options">The request options that specify query parameters such as pagination, sorting, or language preferences.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the collection of related entities of type <typeparamref name="TB"/>.
    /// </returns>
    Task<IEnumerable<TB>> GetB(List<GenericFilter> filters, RequestOptions options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a cross-reference between two entities.
    /// </summary>
    /// <param name="AIdentity">Identity for A</param>
    /// <param name="BIdentity">Identity for B</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<int> CreateCross(Guid AIdentity, Guid BIdentity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete cross reference.
    /// </summary>
    /// <param name="AIdentity">Identity for A</param>
    /// <param name="BIdentity">Identity for B</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<int> DeleteCross(Guid AIdentity, Guid BIdentity, CancellationToken cancellationToken = default);
}
