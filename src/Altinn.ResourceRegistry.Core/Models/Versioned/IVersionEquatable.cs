#nullable enable

namespace Altinn.ResourceRegistry.Core.Models.Versioned;

/// <summary>
/// An object for which the version and last modification date can be compared.
/// </summary>
/// <typeparam name="T">The version type.</typeparam>
public interface IVersionEquatable<in T>
    where T : notnull
{
    /// <summary>
    /// Indicates whether the current object's version is equal to the provided version value.
    /// </summary>
    /// <param name="other">The value to compare with this object's version.</param>
    /// <returns>
    /// <see langword="true"/> if the current object's version is equal to the <paramref name="other"/> parameter;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    bool VersionEquals(T other);

    /// <summary>
    /// Indicates whether the current object's time of modification is greater than the provided <see cref="DateTimeOffset"/> value.
    /// </summary>
    /// <param name="other">The value to compare with this object's modification time.</param>
    /// <returns>
    /// <see langword="true"/> if the current object's modification time is greater than the <paramref name="other"/> parameter;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>This comparison must use at most second precision.</remarks>
    bool ModifiedSince(HttpDateTimeHeaderValue other);
}
