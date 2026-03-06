using System.ComponentModel.DataAnnotations;

namespace Altinn.AccessManagement.Core.Configuration;

/// <summary>
/// Configuration settings for consent migration from old application
/// </summary>
public class ConsentMigrationSettings
{
    /// <summary>
    /// Number of consent GUIDs to fetch per batch
    /// </summary>
    [Range(1, 1000)]
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// Status of consents to migrate (e.g., "expired", "active")
    /// </summary>
    [Range(1, 3)]
    public int ConsentStatus { get; set; } = 1;

    /// <summary>
    /// Delay in milliseconds between processing batches (when GUIDs are available)
    /// </summary>
    [Range(100, 60000)]
    public int NormalDelayMs { get; set; } = 2000;

    /// <summary>
    /// Delay in milliseconds when feed returns zero GUIDs (longer pause before retrying)
    /// </summary>
    [Range(1000, 300000)]
    public int EmptyFeedDelayMs { get; set; } = 60000;

    /// <summary>
    /// Delay in milliseconds when feature flag is disabled (before rechecking)
    /// </summary>
    [Range(60000, 3600000)]
    public int FeatureDisabledDelayMs { get; set; } = 600000;

    /// <summary>
    /// Indicates whether only expired consents should be migrated
    /// </summary>
    public bool OnlyExpiredConsents { get; set; } = true;
}
