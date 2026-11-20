using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Altinn.AccessManagement.TestUtils.Concurrency;

/// <summary>
/// Makes every save of an attached context fail with a unique violation, as if another request kept committing
/// the same row first. Used to drive a retry loop to its attempt limit.
/// </summary>
/// <param name="constraintName">The unique index the injected violation names.</param>
public sealed class FailEverySave(string constraintName)
{
    private int _attempts;

    private volatile bool _enabled = true;

    /// <summary>
    /// Gets how many saves were attempted and failed while enabled.
    /// </summary>
    public int Attempts => Volatile.Read(ref _attempts);

    /// <summary>
    /// Gets or sets whether saves fail. Turn it off around seeding or asserting when the
    /// injection is attached to every context of a host.
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    /// <summary>
    /// Attaches the injection to a context.
    /// </summary>
    public void Attach(DbContext db) => db.SavingChanges += (_, _) =>
    {
        if (!_enabled)
        {
            return;
        }

        Interlocked.Increment(ref _attempts);
        throw new DbUpdateException(
            "An error occurred while saving the entity changes. See the inner exception for details.",
            new PostgresException(
                messageText: $"duplicate key value violates unique constraint \"{constraintName}\"",
                severity: "ERROR",
                invariantSeverity: "ERROR",
                sqlState: PostgresErrorCodes.UniqueViolation,
                constraintName: constraintName));
    };
}
