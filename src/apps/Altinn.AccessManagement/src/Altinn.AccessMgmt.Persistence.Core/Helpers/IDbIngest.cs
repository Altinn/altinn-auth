namespace Altinn.AccessMgmt.Persistence.Core.Helpers;

/// <summary>
/// Interface for ingesting data into the database
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IDbIngest<T> 
    where T : class, new()
{
    /// <summary>
    /// Ingest data into the database
    /// </summary>
    /// <param name="data">Data to ingest</param>
    /// <param name="cancellationToken">CancellationToken</param>
    /// <returns></returns>
    Task<int> Ingest(List<T> data, CancellationToken cancellationToken = default);
}
