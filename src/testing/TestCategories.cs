using System;
using System.Collections.Generic;
using Xunit.v3;

namespace Altinn.Authorization.Testing;

/// <summary>
/// Test category names used by the <c>Category</c> trait. Filter a run with
/// <c>dotnet test -- --filter-trait "Category=Unit"</c> (MTP) or
/// <c>--filter-query "/*/*/*/*[Category=Unit]"</c>.
/// </summary>
public static class TestCategories
{
    /// <summary>Fast, in-process tests with no external dependencies.</summary>
    public const string Unit = "Unit";

    /// <summary>Tests that exercise the real pipeline / a database / external services.</summary>
    public const string Integration = "Integration";

    /// <summary>The trait name both categories are published under.</summary>
    public const string Category = "Category";
}

/// <summary>
/// Marks a test class or method as a fast, in-process unit test
/// (publishes the trait <c>Category=Unit</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class UnitTestAttribute : Attribute, ITraitAttribute
{
    private static readonly IReadOnlyCollection<KeyValuePair<string, string>> Traits =
        new[] { new KeyValuePair<string, string>(TestCategories.Category, TestCategories.Unit) };

    /// <inheritdoc />
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() => Traits;
}

/// <summary>
/// Marks a test class or method as an integration test that needs the real
/// pipeline, a database, or external services (publishes the trait
/// <c>Category=Integration</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class IntegrationTestAttribute : Attribute, ITraitAttribute
{
    private static readonly IReadOnlyCollection<KeyValuePair<string, string>> Traits =
        new[] { new KeyValuePair<string, string>(TestCategories.Category, TestCategories.Integration) };

    /// <inheritdoc />
    public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() => Traits;
}
