using System.Reflection;

using Altinn.AccessManagement.Core.Errors;
using Altinn.Authorization.ProblemDetails;

namespace Altinn.AccessManagement.Api.Tests.Other;

[UnitTest]
public class ProblemsValidation
{
    [Fact]
    public void Problems_AllDescriptors_HaveDistinctErrorCodes()
    {
        var codes = ErrorCodesOf<ProblemDescriptor>(typeof(Problems), d => d.ErrorCode);

        Assert.NotEmpty(codes);
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    [Fact]
    public void ValidationErrors_AllDescriptors_HaveDistinctErrorCodes()
    {
        var codes = ErrorCodesOf<ValidationErrorDescriptor>(typeof(ValidationErrors), d => d.ErrorCode);

        Assert.NotEmpty(codes);
        Assert.Equal(codes.Count, codes.Distinct().Count());
    }

    // Reflects every public static descriptor property on the catalog so the check
    // covers the whole set and cannot drift as descriptors are added (the old
    // hand-maintained list had already missed one). Reading each value also forces
    // its initializer to run, so a descriptor that throws on construction fails here.
    private static List<ErrorCode> ErrorCodesOf<T>(Type catalog, Func<T, ErrorCode> errorCode) =>
        catalog.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(T))
            .Select(p => errorCode((T)p.GetValue(null)!))
            .ToList();
}
