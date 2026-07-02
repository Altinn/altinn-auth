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

        var uncategorised = new List<string>();

        foreach (Type type in types)
        {
            // Only concrete classes are discovered as test classes; abstract bases
            // and open generics are exercised through their concrete subclasses.
            if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters)
            {
                continue;
            }

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

    private static bool HasCategory(IEnumerable<Attribute> attributes) =>
        attributes.Any(a => a is UnitTestAttribute or IntegrationTestAttribute);
}
