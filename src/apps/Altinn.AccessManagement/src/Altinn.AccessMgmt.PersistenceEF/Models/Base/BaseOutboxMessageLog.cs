using System.ComponentModel.DataAnnotations.Schema;
using Altinn.AccessMgmt.PersistenceEF.Models.Audit.Base;
using Altinn.AccessMgmt.PersistenceEF.Models.Extensions;

namespace Altinn.AccessMgmt.PersistenceEF.Models.Base;

/// <summary>
/// Base representation of a message stored in the outbox.
/// </summary>
/// <remarks>
/// The outbox pattern is used to ensure reliable message publishing in distributed systems.
/// Instead of sending messages directly, they are first persisted to the database as
/// outbox records and later processed by a background worker.
///
/// This base class contains the common metadata required to manage the lifecycle of an
/// outbox message, including scheduling, retry handling, processing state, and tracing.
///
/// Implementations typically extend this class to add table mappings or additional fields.
/// </remarks>
[NotMapped]
public class BaseOutboxMessageLog
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseOutboxMessage"/> class.
    /// </summary>
    /// <remarks>
    /// A Version 7 UUID is generated automatically for improved database indexing
    /// and chronological ordering.
    /// </remarks>
    public BaseOutboxMessageLog()
    {
        Id = Guid.CreateVersion7();
    }

    /// <summary>
    /// Gets or sets the unique identifier for the outbox message.
    /// </summary>
    /// <remarks>
    /// The identifier must be a Version 7 UUID to ensure monotonic ordering and
    /// efficient indexing in the database.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// Thrown when the assigned value is not a Version 7 UUID.
    /// </exception>
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
    /// Gets or sets the Outbox Message Id
    /// </summary>
    public Guid OutboxMessageId { get; set; }

    /// <summary>
    /// Gets or sets the Log entry.
    /// </summary>
    public string Log { get; set; }

    /// <summary>
    /// Gets or sets the attempt.
    /// </summary>
    /// <remarks>
    /// Just an indicator that allows you to correlate message with the attempt.
    /// </remarks>
    public int Attempt { get; set; }
}
