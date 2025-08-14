using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Altinn.AccessMgmt.PersistenceEF.Extensions;

public static class PropertyBuilderExtensions
{
    public static PropertyBuilder Translate(this PropertyBuilder builder)
    {
        return builder.HasAnnotation("Translate", true);
    }

    public static PropertyBuilder Translate<TProperty>(this PropertyBuilder<TProperty> builder)
    {
        return builder.HasAnnotation("Translate", true);
    }
}
