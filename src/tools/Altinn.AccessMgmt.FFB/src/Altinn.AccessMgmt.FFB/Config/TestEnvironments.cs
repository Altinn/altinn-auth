namespace Altinn.AccessMgmt.FFB.Config;

/// <summary>
/// Environments known to be test environments. Anything else may be production, so code that must never
/// touch production allows only these.
/// </summary>
public static class TestEnvironments
{
    public static readonly IReadOnlyList<string> Names = ["Local", "AT22", "AT23", "AT24", "YT01", "TT02"];

    public static bool Contains(string environment) => Names.Contains(environment, StringComparer.OrdinalIgnoreCase);
}
