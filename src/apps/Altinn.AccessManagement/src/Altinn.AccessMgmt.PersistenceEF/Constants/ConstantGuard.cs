using System.Reflection;

namespace Altinn.AccessMgmt.PersistenceEF.Constants;

public static class ConstantGuard
{
    /// <summary>
    /// Validates that all <see cref="ConstantDefinition{T}"/> instances across the given assembly
    /// have unique <see cref="ConstantDefinition{T}.Id"/> values.
    /// Throws <see cref="InvalidOperationException"/> if duplicates are found.
    /// </summary>
    public static void ConstantIdsAreUnique()
    {
        var assembly = typeof(ConstantDefinition<>).Assembly;
        var allConstants = assembly
            .GetTypes()
            .Where(t => t.IsClass && t.IsAbstract && t.IsSealed)
            .SelectMany(t => t.GetProperties(BindingFlags.Public | BindingFlags.Static))
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition() == typeof(ConstantDefinition<>))
            .Select(p => p.GetValue(null))
            .Cast<dynamic>()
            .ToList();

        var duplicates = allConstants
            .GroupBy(c => (Guid)c.Id)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicates.Count > 0)
        {
            var details = duplicates.Select(d => $"Duplicate Id '{d.Key}' for ConstantDefinition found in: {string.Join(", ", d.Select(c => c.Entity?.Name))}");
            throw new InvalidOperationException(string.Join(Environment.NewLine, details));
        }
    }
}
