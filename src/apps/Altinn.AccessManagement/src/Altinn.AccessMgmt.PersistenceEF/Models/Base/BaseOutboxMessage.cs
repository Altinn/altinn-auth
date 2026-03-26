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
public class BaseOutboxMessage
{
    private Guid _id;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseOutboxMessage"/> class.
    /// </summary>
    /// <remarks>
    /// A Version 7 UUID is generated automatically for improved database indexing
    /// and chronological ordering.
    /// </remarks>
    public BaseOutboxMessage()
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
    /// Gets or sets the reference identifier used to correlate related messages.
    /// </summary>
    public string RefId { get; set; }

    /// <summary>
    /// Gets or sets the current processing status of the outbox message.
    /// </summary>
    public OutboxStatus Status { get; set; }

    /// <summary>
    /// Gets or sets the earliest time at which the message may be processed.
    /// </summary>
    /// <remarks>
    /// If <c>null</c>, the message is eligible for immediate processing.
    /// This property can be used to implement delayed execution or retry backoff strategies.
    /// </remarks>
    public DateTime? Schedule { get; set; }

    /// <summary>
    /// Gets or sets the serialized payload of the message.
    /// </summary>
    /// <remarks>
    /// This typically contains JSON representing the event or command data
    /// to be handled by the registered handler.
    /// </remarks>
    public string Data { get; set; }

    /// <summary>
    /// Gets or sets the name or identifier of the handler responsible for processing the message.
    /// </summary>
    public string Handler { get; set; }

    /// <summary>
    /// Gets or sets the number of times the message processing has been retried.
    /// </summary>
    /// <remarks>
    /// This value can be used to implement retry policies or dead-letter handling.
    /// </remarks>
    public int Retries { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum allowed processing duration for the message.
    /// </summary>
    /// <remarks>
    /// If processing exceeds this duration, the message may be considered abandoned
    /// and eligible for retry by another worker.
    /// </remarks>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the timestamp when processing of the message started.
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// Custom message field.
    /// </summary>
    public string? HandlerMessage { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the message processing completed.
    /// </summary>
    /// <remarks>
    /// This value is set when the message transitions to a terminal state
    /// such as <see cref="OutboxStatus.Completed"/> or <see cref="OutboxStatus.Failed"/>.
    /// </remarks>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Gets or sets the OpenTelemetry correlation identifier associated with the message.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// Gets or sets the attempted number of times the message has been executed.
    /// </summary>
    /// <remarks>
    /// This value is used to correlate logs.
    /// </remarks>
    public int Attempt { get; set; } = 0;
}

/// <summary>
/// Represents the processing state of an outbox message.
/// </summary>
public enum OutboxStatus
{
    /// <summary>
    /// The message has been created but has not yet been processed.
    /// </summary>
    Pending,

    /// <summary>
    /// The message has been claimed by a worker and is currently being processed.
    /// </summary>
    Processing,

    /// <summary>
    /// The message was successfully processed by its handler.
    /// </summary>
    Completed,

    /// <summary>
    /// No handler was found for the message.
    /// </summary>
    NoHandler,

    /// <summary>
    /// The message timed out.
    /// </summary>
    TimedOut,

    /// <summary>
    /// The message timed out.
    /// </summary>
    Interrupted,

    /// <summary>
    /// The message processing failed.
    /// </summary>
    Failed,
}
