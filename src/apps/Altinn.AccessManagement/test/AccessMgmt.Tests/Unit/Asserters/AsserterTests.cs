using Altinn.AccessManagement.Core.Asserters;

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
}
