#nullable enable

using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Altinn.ResourceRegistry.Models.ModelBinding;

/// <summary>
/// Extension methods for <see cref="IList{T}"/> of <see cref="IModelBinderProvider"/>.
/// </summary>
internal static class ModelBinderProviderListExtensions
{
    /// <summary>
    /// Insert a <see cref="ISingleton{TSelf}"/> binder provider.
    /// </summary>
    /// <typeparam name="T">The binder type.</typeparam>
    /// <param name="list">The list.</param>
    /// <param name="index">The index.</param>
    /// <returns><paramref name="list"/>, for chaining.</returns>
    public static IList<IModelBinderProvider> InsertSingleton<T>(this IList<IModelBinderProvider> list, int index)
        where T : IModelBinderProvider, ISingleton<T>
    {
        var instance = T.Instance;

        list.Insert(index, instance);
        return list;
    }
}
