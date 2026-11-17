namespace Altinn.AccessMgmt.Core.Appsettings;

/// <summary>
/// Holds feature toggle values resolved once at application startup and intended to remain stable for the
/// lifetime of the host. Registered as a singleton so each host container has its own instance,
/// avoiding process-global mutable state and cross-host/test interference.
/// </summary>
public sealed class AppLifecycleFeatures
{
    /// <summary>
    /// Whether ADOS administrative units should be treated as subunits that inherit mainunit access
    /// (equal to BEDR/AAFY). Resolved from the <c>AccessManagement.Subunit.AdosInheritance</c> feature flag.
    /// </summary>
    public bool AdosSubunitInheritance { get; set; }

    /// <summary>
    /// Whether Altinn 2 roles should be excluded from all read and authorization paths.
    /// Resolved from the <c>AccessManagement.Roles.DisableAltinn2</c> feature flag.
    /// </summary>
    public bool DisableAltinn2Roles { get; set; }
}
