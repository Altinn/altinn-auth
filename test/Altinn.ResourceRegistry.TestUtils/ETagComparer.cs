using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace Altinn.ResourceRegistry.TestUtils;

public class ETagComparer
    : IEqualityComparer<EntityTagHeaderValue>
{
    private const StringComparison COMPARISON = StringComparison.Ordinal;

    public static ETagComparer Instance { get; } = new();

    private ETagComparer()
    {
    }

    public bool Equals(EntityTagHeaderValue? x, EntityTagHeaderValue? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
        {
            return false;
        }

        return x.IsWeak == y.IsWeak 
            && string.Equals(x.Tag, y.Tag, COMPARISON);
    }

    public int GetHashCode([DisallowNull] EntityTagHeaderValue obj)
    {
        return HashCode.Combine(obj.IsWeak, string.GetHashCode(obj.Tag, COMPARISON));
    }
}
