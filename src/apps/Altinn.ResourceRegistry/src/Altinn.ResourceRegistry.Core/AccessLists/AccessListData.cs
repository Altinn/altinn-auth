#nullable enable

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// Utils for <see cref="AccessListData{T}"/>.
/// </summary>
public static class AccessListData
{
    /// <summary>
    /// Creates a new instance of <see cref="AccessListData{T}"/>.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    /// <param name="id">The access list id.</param>
    /// <param name="updatedAt">When the access list was last updated.</param>
    /// <param name="version">The access list version.</param>
    /// <param name="value">The data.</param>
    /// <returns>A new <see cref="AccessListData{T}"/>.</returns>
    public static AccessListData<T> Create<T>(
        Guid id,
        DateTimeOffset updatedAt,
        ulong version,
        T value)
        => new(id, updatedAt, version, value);

    /// <summary>
    /// Creates a new instance of <see cref="AccessListData{T}"/> from a <see cref="AccessListMetadata"/> and the data.
    /// </summary>
    /// <typeparam name="T">The data type.</typeparam>
    /// <param name="metadata">The access list metadata.</param>
    /// <param name="value">The data.</param>
    /// <returns>A new <see cref="AccessListData{T}"/>.</returns>
    public static AccessListData<T> Create<T>(
        AccessListMetadata metadata,
        T value)
        => new(metadata, value);
}

/// <summary>
/// Data from an access list along with metadata.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
/// <param name="Id"><inheritdoc cref="AccessListMetadata(Guid, DateTimeOffset, ulong)" path="/param[@name='Id']/node()"/></param>
/// <param name="UpdatedAt"><inheritdoc cref="AccessListMetadata(Guid, DateTimeOffset, ulong)" path="/param[@name='UpdatedAt']/node()"/></param>
/// <param name="Version"><inheritdoc cref="AccessListMetadata(Guid, DateTimeOffset, ulong)" path="/param[@name='Version']/node()"/></param>
/// <param name="Value">The data value.</param>
public sealed record AccessListData<T>(
    Guid Id,
    DateTimeOffset UpdatedAt,
    ulong Version,
    T Value)
    : AccessListMetadata(Id, UpdatedAt, Version)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccessListData{T}"/> class
    /// from a <see cref="AccessListMetadata"/> and the data.
    /// </summary>
    /// <param name="metadata">The metadata.</param>
    /// <param name="value">The value.</param>
    public AccessListData(AccessListMetadata metadata, T value)
        : this(metadata.Id, metadata.UpdatedAt, metadata.Version, value)
    {
    }
}
