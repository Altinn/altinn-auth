using Altinn.AccessManagement.Core.Extensions;

namespace Altinn.AccessMgmt.Core.Models;

/// <summary>
/// Persistent status
/// </summary>
public class StatusRecord
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusRecord"/> class.
    /// </summary>
    public StatusRecord()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>
    /// Identity
    /// </summary>
    public Guid Id
    {
        get => _id;
        set
        {
            if (!value.IsVersion7Uuid())
            {
                throw new ArgumentException("Id must be a version 7 UUID", nameof(value));
            }

            _id = value;
        }
    }

    /// <summary>
    /// Name (e.g. AccessMgmt-Ingest-PartyFeed)
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Current state
    /// </summary>
    public string State { get; set; }

    /// <summary>
    /// Latest message
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Payload data (e.g. JsonSerialized item)
    /// </summary>
    public string Payload { get; set; }

    /// <summary>
    /// A defineable limit (e.g. retryLimit)
    /// </summary>
    public int RetryLimit { get; set; }

    /// <summary>
    /// A counter (e.g. retryAttempts)
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
