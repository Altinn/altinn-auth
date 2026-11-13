namespace Altinn.Authorization.Xacml;

/// <summary>
/// Extensions to facilitate sanitization of string values
/// </summary>
internal static class StringExtensions
{
    /// <summary>
    /// Sanitize the input as a file name.
    /// </summary>
    /// <param name="input">The input variable to be sanitized</param>
    /// <returns>The input, unchanged</returns>
    /// <exception cref="ArgumentOutOfRangeException">The input contains characters that are not valid in a file name</exception>
    internal static string AsFileName(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        // Check for alpha, numeric and hyphen to avoid the more expensive check
        if (input.All(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '-'))
        {
            return input;
        }

        char[] illegalFileNameCharacters = Path.GetInvalidFileNameChars();
        if (illegalFileNameCharacters.Any(ic => input.Any(i => ic == i)))
        {
            throw new ArgumentOutOfRangeException(nameof(input));
        }

        return input;
    }
}
