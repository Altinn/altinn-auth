using Altinn.AccessManagement.Core.Asserters;
using Xunit;

namespace Altinn.AccessManagement.Tests.Unit.Asserters;

/// <summary>
/// summary
/// </summary>
[UnitTest]
public class AsserterTests
{
    /// <summary>
    /// summary
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    /// <returns></returns>
    public static IAssert<TModel> Asserter<TModel>() => new Asserter<TModel>();

    /// <summary>
    /// When every alternative in <see cref="IAssert{T}.Any"/> fails, the collected
    /// errors must be reported — the combinator must be able to fail.
    /// </summary>
    [Fact]
    public void Any_AllAssertionsFail_ReportsCollectedErrors()
    {
        var asserter = Asserter<string>();
        Assertion<string> failA = (errors, values) => errors.Add("A", ["a failed"]);
        Assertion<string> failB = (errors, values) => errors.Add("B", ["b failed"]);

        var result = asserter.Evaluate([], asserter.Any(failA, failB));

        Assert.NotNull(result);
        Assert.Contains("A", result.Errors.Keys);
        Assert.Contains("B", result.Errors.Keys);
    }

    /// <summary>
    /// When at least one alternative in <see cref="IAssert{T}.Any"/> passes, no
    /// errors are reported.
    /// </summary>
    [Fact]
    public void Any_OneAssertionPasses_ReportsNoErrors()
    {
        var asserter = Asserter<string>();
        Assertion<string> pass = (errors, values) => { };
        Assertion<string> fail = (errors, values) => errors.Add("B", ["b failed"]);

        var result = asserter.Evaluate([], asserter.Any(pass, fail));

        Assert.Null(result);
    }
}
