using Altinn.AccessMgmt.Persistence.Core.Models;

namespace Altinn.AccessMgmt.Persistence.Core.Contracts;

/// <summary>
/// Ingest data service
/// </summary>
public interface IIngestService
{
    /// <summary>
    /// Ingest data
    /// </summary>
    Task<int> IngestData<T>(List<T> data, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingest data to temp table, using original table as template
    /// </summary>
    Task<int> IngestTempData<T>(List<T> data, Guid ingestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Merge data from temp table to original
    /// </summary>
    Task<int> MergeTempData<T>(Guid ingestId, IEnumerable<GenericParameter> matchColumns = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Ingest data to temp table, using original table as template
    /// </summary>
    Task<int> IngestAndMergeData<T>(List<T> data, IEnumerable<GenericParameter> matchColumns = null, CancellationToken cancellationToken = default);
}
