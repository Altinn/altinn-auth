#nullable enable

using Altinn.ResourceRegistry.Core.Models.Versioned;
using FluentAssertions.Formatting;
using System.Runtime.CompilerServices;

namespace Altinn.ResourceRegistry.Tests.Utils;

internal class VersionedEntityConditionResultFormatter 
    : IValueFormatter
{
    [ModuleInitializer]
    internal static void Register()
    {
        Formatter.AddFormatter(Instance);
    }

    private static IValueFormatter Instance { get; } = new VersionedEntityConditionResultFormatter();

    private VersionedEntityConditionResultFormatter()
    {
    }

    public bool CanHandle(object value)
        => value is VersionedEntityConditionResult;

    public void Format(object value, FormattedObjectGraph formattedGraph, FormattingContext context, FormatChild formatChild)
    {
        formattedGraph.AddFragment(((VersionedEntityConditionResult)value).ToString());
    }
}
