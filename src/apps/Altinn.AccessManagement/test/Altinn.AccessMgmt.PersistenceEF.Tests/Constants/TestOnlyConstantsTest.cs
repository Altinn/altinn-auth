using System.Reflection;
using Altinn.AccessMgmt.PersistenceEF.Constants;
using Altinn.AccessMgmt.PersistenceEF.Models;

namespace Altinn.AccessMgmt.PersistenceEF.Tests.Constants;

[UnitTest]
public class TestOnlyConstantsTest
{
    /// <summary>
    /// The static data ingest only leaves out rows that point to a test only package. Rows that point to a test only
    /// role or area, for example, would still be inserted in production and fail on the foreign key.
    /// </summary>
    [Fact]
    public void TestOnly_IsOnlySetOnPackages()
    {
        var constants = typeof(ConstantDefinition<>).Assembly.GetTypes()
            .Where(t => t.IsClass && t.IsAbstract && t.IsSealed)
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Static))
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(ConstantDefinition<>))
            .ToList();

        Assert.Contains(constants, p => p.PropertyType == typeof(ConstantDefinition<Role>));

        var offenders = constants
            .Where(p => p.PropertyType != typeof(ConstantDefinition<Package>))
            .Where(p => (bool)p.PropertyType.GetProperty(nameof(ConstantDefinition<Package>.TestOnly))!.GetValue(p.GetValue(null))!)
            .Select(p => $"{p.DeclaringType!.Name}.{p.Name}");

        Assert.Empty(offenders);
    }
}
