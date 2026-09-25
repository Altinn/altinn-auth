#nullable enable

namespace Altinn.ResourceRegistry.Core.AccessLists;

/// <summary>
/// What to include when getting access lists.
/// </summary>
[Flags]
public enum AccessListIncludes : uint
{
    /// <summary>
    /// No additional includes.
    /// </summary>
    None = default,

    /// <summary>
    /// Include resource connections.
    /// </summary>
    ResourceConnections = 1 << 0,

    /// <summary>
    /// Include members.
    /// </summary>
    /// <remarks>
    /// Not implemented.
    /// </remarks>
    Members = 1 << 1,

    /// <summary>
    /// Include resource connections and their actions.
    /// </summary>
    ResourceConnectionsActions = ResourceConnections | 1 << 2,
}
