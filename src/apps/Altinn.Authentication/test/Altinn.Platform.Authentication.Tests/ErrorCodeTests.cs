using System;
using System.Linq;
using System.Reflection;
using Altinn.Authentication.Core.Problems;
using Altinn.Authorization.ProblemDetails;
using Altinn.Platform.Authentication.Core.Errors;
using Xunit;

namespace Altinn.Platform.Authentication.Tests
{
    /// <summary>
    /// Error codes are part of the API. Consumers can't tell two errors apart if they share a code.
    /// </summary>
    public class ErrorCodeTests
    {
        [Fact]
        public void ValidationErrors_HaveUniqueErrorCodes()
        {
            AssertUniqueErrorCodes<ValidationErrorDescriptor>(typeof(ValidationErrors), d => d.ErrorCode.ToString());
        }

        [Fact]
        public void Problems_HaveUniqueErrorCodes()
        {
            AssertUniqueErrorCodes<ProblemDescriptor>(typeof(Problem), d => d.ErrorCode.ToString());
        }

        private static void AssertUniqueErrorCodes<T>(Type catalog, Func<T, string?> errorCode)
        {
            PropertyInfo[] descriptors = catalog
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Where(p => p.PropertyType == typeof(T))
                .ToArray();

            Assert.NotEmpty(descriptors);

            var duplicates = descriptors
                .GroupBy(p => errorCode((T)p.GetValue(null)!))
                .Where(g => g.Count() > 1)
                .Select(g => $"{g.Key}: {string.Join(", ", g.Select(p => p.Name))}");

            Assert.Empty(duplicates);
        }
    }
}
