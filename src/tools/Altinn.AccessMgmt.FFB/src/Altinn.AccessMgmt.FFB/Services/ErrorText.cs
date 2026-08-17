namespace Altinn.AccessMgmt.FFB.Services;

/// <summary>
/// Formats exceptions for display in the UI.
/// </summary>
public static class ErrorText
{
    /// <summary>
    /// Flattens an exception chain into a display string, skipping consecutive
    /// duplicate messages, joined by blank lines.
    /// </summary>
    public static string Flatten(Exception ex)
    {
        var parts = new List<string> { ex.Message };
        var inner = ex.InnerException;
        while (inner is not null)
        {
            if (inner.Message != parts[^1])
            {
                parts.Add(inner.Message);
            }

            inner = inner.InnerException;
        }

        return string.Join("\n\n", parts);
    }
}
