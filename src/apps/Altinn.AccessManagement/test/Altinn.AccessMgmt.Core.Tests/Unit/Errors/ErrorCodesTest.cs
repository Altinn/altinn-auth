using System.Reflection;
using Altinn.AccessManagement.Core.Errors;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessMgmt.Core.Tests.Unit.Errors;

/// <summary>
/// Error codes are part of the API. Consumers can't tell two errors apart if they share a code.
/// </summary>
[UnitTest]
public class ErrorCodesTest
{
    [Fact]
    public void ValidationErrors_HaveUniqueErrorCodes()
    {
        AssertUniqueErrorCodes<ValidationErrorDescriptor>(typeof(ValidationErrors), d => d.ErrorCode.ToString());
    }

    [Fact]
    public void Problems_HaveUniqueErrorCodes()
    {
        AssertUniqueErrorCodes<ProblemDescriptor>(typeof(Problems), d => d.ErrorCode.ToString());
    }

    private static void AssertUniqueErrorCodes<T>(Type catalog, Func<T, string?> errorCode)
    {
        var descriptors = catalog
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(T))
            .ToList();

        descriptors.Should().NotBeEmpty();

        var duplicates = descriptors
            .GroupBy(p => errorCode((T)p.GetValue(null)!))
            .Where(g => g.Count() > 1)
            .Select(g => $"{g.Key}: {string.Join(", ", g.Select(p => p.Name))}");

        duplicates.Should().BeEmpty();
    }
}
