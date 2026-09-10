namespace Altinn.AccessMgmt.Core.Appsettings;

/// <summary>
/// Holds feature toggle values resolved once at application startup and kept immutable for the
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
}
