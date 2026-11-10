#nullable enable

namespace Altinn.ResourceRegistry.Models.ModelBinding;

/// <summary>
/// A class which provides a singleton.
/// </summary>
internal interface ISingleton<TSelf>
    where TSelf : ISingleton<TSelf>
{
    /// <summary>
    /// Gets a singleton instance of model binder.
    /// </summary>
    public static abstract TSelf Instance { get; }
}
