#nullable enable

using System.Collections.Immutable;
using System.Text;
using Altinn.ResourceRegistry.Core.AccessLists;
using Altinn.ResourceRegistry.Extensions;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Altinn.ResourceRegistry.Models.ModelBinding;

/// <summary>
/// Binds the <see cref="AccessListIncludes"/> model.
/// </summary>
internal class AccessListIncludesModelBinder
    : ModelBinder<AccessListIncludes>
    , ISingleton<AccessListIncludesModelBinder>
{
    private static ImmutableDictionary<AccessListIncludes, string> _stringified
        = ImmutableDictionary<AccessListIncludes, string>.Empty;

    private readonly static string ResourceConnections = "resources";
    private readonly static string ResourceConnectionsActions = "resource-actions";
    private readonly static string Members = "members";

    /// <summary>
    /// The allowed values.
    /// </summary>
    public static readonly ImmutableArray<string> AllowedValues = [ResourceConnections, ResourceConnectionsActions, Members];

    /// <summary>
    /// Stringify a <see cref="AccessListIncludes"/> to a query-valid value.
    /// </summary>
    /// <param name="value">The value to stringify.</param>
    /// <returns>The stringified value, or <see langword="null"/> if it is <see cref="AccessListIncludes.None"/>.</returns>
    public static string? Stringify(AccessListIncludes value)
    {
        if (value == AccessListIncludes.None)
        {
            return null;
        }

        return ImmutableInterlocked.GetOrAdd(ref _stringified, value, CreateString);

        static string CreateString(AccessListIncludes value)
        {
            var builder = new StringBuilder();
            if (value.HasFlag(AccessListIncludes.ResourceConnections))
            {
                builder.Append(ResourceConnections).Append(',');
            }

            if (value.HasFlag(AccessListIncludes.ResourceConnectionsActions))
            {
                builder.Append(ResourceConnectionsActions).Append(',');
            }

            if (value.HasFlag(AccessListIncludes.Members))
            {
                builder.Append(Members).Append(',');
            }

            builder.Length -= 1;
            return builder.ToString();
        }
    }

    /// <inheritdoc/>
    public static AccessListIncludesModelBinder Instance { get; } = new();

    private AccessListIncludesModelBinder()
    {
    }

    /// <inheritdoc/>
    public override Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var values = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        var result = AccessListIncludes.None;

        bool hasErrors = false;
        foreach (var value in values)
        {
            foreach (var name in value.AsSpan().Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                result |= Parse(name, bindingContext, ref hasErrors);
            }
        }

        if (hasErrors)
        {
            bindingContext.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        bindingContext.Result = ModelBindingResult.Success(result);
        return Task.CompletedTask;
    }

    private static AccessListIncludes Parse(ReadOnlySpan<char> name, ModelBindingContext bindingContext, ref bool hasErrors)
    {
        if (name.Equals(ResourceConnections, StringComparison.Ordinal))
        {
            return AccessListIncludes.ResourceConnections;
        }

        if (name.Equals(ResourceConnectionsActions, StringComparison.Ordinal))
        {
            return AccessListIncludes.ResourceConnectionsActions;
        }

        if (name.Equals(Members, StringComparison.Ordinal))
        {
            return AccessListIncludes.Members;
        }

        hasErrors = true;
        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName, $"Invalid value: '{name}'");
        return AccessListIncludes.None;
    }
}
