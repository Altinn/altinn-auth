namespace Altinn.ResourceRegistry.Core.Utils;

/// <summary>
/// Interface for converting from a type to another type.
/// </summary>
/// <typeparam name="TSelf">The type to convert to.</typeparam>
/// <typeparam name="T">The type to convert from.</typeparam>
public interface IConvertibleFrom<TSelf, in T>
    where TSelf : IConvertibleFrom<TSelf, T>
{
    /// <summary>
    /// Convert from <typeparamref name="T"/> to <typeparamref name="TSelf"/>.
    /// </summary>
    /// <param name="value">The value to convert from.</param>
    /// <returns>The converted value.</returns>
    public static abstract TSelf From(T value);
}
