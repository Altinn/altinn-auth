using System;
using System.Linq;

namespace Altinn.Platform.Authorization.Helpers.Extensions
{
    /// <summary>
    /// Extensions to facilitate sanitization of string values
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Sanitize the input as a file name. 
        /// </summary>
        /// <param name="input">The input variable to be sanitized</param>
        /// <param name="throwExceptionOnInvalidCharacters">Throw exception instead of replacing invalid characters with '-'</param>
        /// <returns></returns>
        public static string AsFileName(this string input, bool throwExceptionOnInvalidCharacters = true)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            // Check for alpha, numeric and hyphen to avoid the third and most expensive check
            if (input.All(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '-'))
            {
                return input;
            }

            char[] illegalFileNameCharacters = System.IO.Path.GetInvalidFileNameChars();
            if (throwExceptionOnInvalidCharacters)
            {
                if (illegalFileNameCharacters.Any(ic => input.Any(i => ic == i)))
                {                    
                    throw new ArgumentOutOfRangeException(nameof(input));
                }

                return input;
            }

            return illegalFileNameCharacters.Aggregate(input, (current, c) => current.Replace(c, '-'));
        }
    }
}
