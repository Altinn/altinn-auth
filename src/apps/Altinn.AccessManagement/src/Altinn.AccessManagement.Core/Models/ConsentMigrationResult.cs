namespace Altinn.AccessManagement.Core.Models;

/// <summary>
/// Result of a single consent migration operation
/// </summary>
public record ConsentMigrationResult
{
    /// <summary>
    /// Indicates if the migration was successful
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if migration failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    public static ConsentMigrationResult Succeeded { get; } = new() { Success = true };

    /// <summary>
    /// Creates a failed result with error message
    /// </summary>
    public static ConsentMigrationResult Failed(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}
