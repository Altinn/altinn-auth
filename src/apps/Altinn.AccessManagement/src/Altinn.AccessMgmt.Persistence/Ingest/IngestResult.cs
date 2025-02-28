namespace Altinn.AccessMgmt.Repo.Ingest;

/// <summary>
/// Result generated from an ingest action
/// </summary>
public class IngestResult
{
    /// <summary>
    /// IngestResult constructor
    /// </summary>
    /// <param name="type">Type of data ingested</param>
    public IngestResult(Type type)
    {
        Type = type;
    }

    /// <summary>
    /// Type of data ingested
    /// </summary>
    public Type Type { get; set; }

    /// <summary>
    /// Indicate if ingestion was successfull
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Objects created
    /// </summary>
    public int Created { get; set; }

    /// <summary>
    /// Objects updated
    /// </summary>
    public int Updated { get; set; }

    /// <summary>
    /// Objects delted
    /// </summary>
    public int Deleted { get; set; }

    /// <summary>
    /// Objects ignored
    /// </summary>
    public int Ignored { get; set; }
}
