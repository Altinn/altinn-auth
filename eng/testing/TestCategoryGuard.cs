using System.Reflection;

namespace Altinn.Authorization.Testing;

/// <summary>
/// Guards the unit/integration lane split. The CI test lanes select tests with
/// <c>--filter-trait "Category=Unit"</c> / <c>--filter-trait "Category=Integration"</c>, so a
/// test method that carries neither category (via <see cref="UnitTestAttribute"/>
/// or <see cref="IntegrationTestAttribute"/> on the method or its class) matches
/// no lane and is silently skipped. This test fails the build instead, naming the
/// offenders. It is compiled into every test assembly (see
/// <c>src/Directory.Build.targets</c>) and inspects its own assembly.
/// </summary>
[UnitTest]
public class TestCategoryGuard
{
    [Fact]
    public void EveryTestMethodHasACategoryTrait()
    {
        var uncategorised = new List<string>();

        foreach (Type type in TestClasses())
        {
            bool classCategorised = HasCategory(type.GetCustomAttributes(inherit: true).Cast<Attribute>());

            // Include inherited methods: a concrete class may inherit its tests
            // (and its class-level category) from an abstract base.
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                bool isTest = method.GetCustomAttributes(inherit: true)
                    .Any(a => a is FactAttribute || a is TheoryAttribute);
                if (!isTest)
                {
                    continue;
                }

                bool methodCategorised = HasCategory(method.GetCustomAttributes(inherit: true).Cast<Attribute>());

                if (!classCategorised && !methodCategorised)
                {
                    uncategorised.Add($"{type.FullName}.{method.Name}");
                }
            }
        }

        uncategorised.Should().BeEmpty(
            "every test must be marked [UnitTest] or [IntegrationTest] (on the class or the method) so the CI unit/integration lane filters run it; an uncategorised test matches neither lane and is silently skipped");
    }

    /// <summary>
    /// Guards the <c>HasIntegrationTests</c> project property (declared in the csproj,
    /// embedded as assembly metadata via <c>src/Directory.Build.targets</c>). Projects
    /// that set it to <c>false</c> are skipped by the CI integration lane, so a stale
    /// declaration either hides integration tests from CI (declared false, tests exist)
    /// or brings back the misleading zero-test "Failed!" summary the property exists to
    /// avoid (declared true, no tests). Permanently skipped tests
    /// (<c>[Fact(Skip = ...)]</c>) don't count as runnable: an all-skipped run is also
    /// reported as "Failed!", and skipping the assembly changes nothing for them.
    /// </summary>
    [Fact]
    public void HasIntegrationTestsDeclarationMatchesAssembly()
    {
        Assembly assembly = typeof(TestCategoryGuard).Assembly;

        bool declared = !string.Equals(
            assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "HasIntegrationTests")?.Value,
            "false",
            StringComparison.OrdinalIgnoreCase);

        var runnableIntegrationTests = new List<string>();

        foreach (Type type in TestClasses())
        {
            bool classIsIntegration = type.GetCustomAttributes(inherit: true).OfType<IntegrationTestAttribute>().Any();

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                FactAttribute? fact = method.GetCustomAttributes(inherit: true).OfType<FactAttribute>().FirstOrDefault();
                if (fact is null || fact.Skip is not null)
                {
                    continue;
                }

                bool methodIsIntegration = method.GetCustomAttributes(inherit: true).OfType<IntegrationTestAttribute>().Any();

                if (classIsIntegration || methodIsIntegration)
                {
                    runnableIntegrationTests.Add($"{type.FullName}.{method.Name}");
                }
            }
        }

        if (declared)
        {
            runnableIntegrationTests.Should().NotBeEmpty(
                "this assembly has no runnable integration tests, so the CI integration lane would run it with zero matching tests and report the run as \"Failed!\"; declare <HasIntegrationTests>false</HasIntegrationTests> in the csproj so the lane skips it");
        }
        else
        {
            runnableIntegrationTests.Should().BeEmpty(
                "this assembly declares <HasIntegrationTests>false</HasIntegrationTests>, so the CI integration lane skips it and these integration tests would never run in CI; remove the property from the csproj");
        }
    }

    /// <summary>
    /// Concrete classes of this assembly — the only ones xUnit discovers as test
    /// classes; abstract bases and open generics are exercised through their concrete
    /// subclasses.
    /// </summary>
    private static IEnumerable<Type> TestClasses()
    {
        Assembly assembly = typeof(TestCategoryGuard).Assembly;

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types.OfType<Type>().ToArray();
        }

        return types.Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters);
    }

    private static bool HasCategory(IEnumerable<Attribute> attributes) =>
        attributes.Any(a => a is UnitTestAttribute or IntegrationTestAttribute);
}
